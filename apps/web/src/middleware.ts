import { NextRequest, NextResponse } from "next/server";

const AUTH_COOKIE           = "auth_token";
const REFRESH_COOKIE        = "refresh_token";
const ORG_COOKIE            = "org_id";
const USER_ID_COOKIE        = "user_id";
const ROLE_COOKIE           = "member_role";
const NAME_COOKIE           = "display_name";
const EMAIL_VERIFIED_COOKIE = "email_verified";

const PUBLIC_ROUTES    = ["/login", "/register", "/verify-email", "/forgot-password", "/reset-password", "/invite"];
const PROTECTED_PREFIX = "/dashboard";

const COOKIE_OPTS = {
  httpOnly: true,
  path: "/",
  sameSite: "lax" as const,
  secure: process.env.NODE_ENV === "production",
};

// Reads the JWT expiry time only for proactive refresh scheduling.
// This does NOT verify the signature — authorization is enforced server-side on every API request.
function jwtExpiry(token: string): number {
  try {
    const part    = token.split(".")[1];
    const padded  = part.replace(/-/g, "+").replace(/_/g, "/");
    const payload = JSON.parse(atob(padded));
    return typeof payload.exp === "number" ? payload.exp : 0;
  } catch {
    return 0;
  }
}

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const token        = request.cookies.get(AUTH_COOKIE)?.value;

  const isPublic    = PUBLIC_ROUTES.some((r) => pathname.startsWith(r));
  const isProtected = pathname.startsWith(PROTECTED_PREFIX);

  // "false" explicitly = unverified. Absent cookie = treat as verified (backwards compat for existing sessions)
  const emailVerified = request.cookies.get(EMAIL_VERIFIED_COOKIE)?.value !== "false";

  if (token) {
    if (!emailVerified) {
      // Unverified users can only land on /verify-email
      if (!pathname.startsWith("/verify-email")) {
        return NextResponse.redirect(new URL("/verify-email?sent=true", request.url));
      }
      return NextResponse.next();
    }

    // Verified users hitting public auth pages → go to dashboard
    // Exception: /invite must be accessible to accept/decline while logged in
    if (isPublic && !pathname.startsWith("/invite")) {
      return NextResponse.redirect(new URL("/dashboard", request.url));
    }
  }

  // Not logged in hitting protected route → redirect to login
  if (!token && isProtected) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("next", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Proactive token refresh: if JWT expires in < 60 seconds, refresh now
  if (token && isProtected) {
    const exp     = jwtExpiry(token);
    const nowSecs = Math.floor(Date.now() / 1000);
    const refresh = request.cookies.get(REFRESH_COOKIE)?.value;

    if (exp > 0 && exp - nowSecs < 60 && refresh) {
      try {
        const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5116";
        const refreshRes = await fetch(`${apiBase}/api/auth/refresh`, {
          method:  "POST",
          headers: { "Content-Type": "application/json" },
          body:    JSON.stringify({ refreshToken: refresh }),
          cache:   "no-store",
        });

        if (refreshRes.ok) {
          const data = await refreshRes.json();

          // Rewrite request cookie so the current page sees the new token
          const reqHeaders = new Headers(request.headers);
          const existing   = request.headers.get("cookie") ?? "";
          reqHeaders.set("cookie", replaceCookie(existing, AUTH_COOKIE, data.accessToken));

          const response = NextResponse.next({ request: { headers: reqHeaders } });

          response.cookies.set(AUTH_COOKIE,           data.accessToken,                                   { ...COOKIE_OPTS, maxAge: data.expiresIn });
          response.cookies.set(REFRESH_COOKIE,        data.refreshToken,                                  { ...COOKIE_OPTS, maxAge: data.refreshExpiresIn });
          response.cookies.set(ORG_COOKIE,            data.organization.id,                               { ...COOKIE_OPTS, maxAge: data.expiresIn });
          response.cookies.set(USER_ID_COOKIE,        data.user.id,                                       { ...COOKIE_OPTS, maxAge: data.expiresIn });
          response.cookies.set(ROLE_COOKIE,           data.organization.role,                             { ...COOKIE_OPTS, maxAge: data.expiresIn });
          response.cookies.set(NAME_COOKIE,           `${data.user.firstName} ${data.user.lastName}`,     { ...COOKIE_OPTS, maxAge: data.expiresIn });
          response.cookies.set(EMAIL_VERIFIED_COOKIE, data.user.emailVerified ? "true" : "false",         { ...COOKIE_OPTS, maxAge: data.expiresIn });

          return response;
        } else {
          // Refresh token invalid/expired → force re-login
          const loginUrl = new URL("/login", request.url);
          const res = NextResponse.redirect(loginUrl);
          res.cookies.delete(AUTH_COOKIE);
          res.cookies.delete(REFRESH_COOKIE);
          return res;
        }
      } catch {
        // Network error — let the request through with the old token
      }
    }
  }

  return NextResponse.next();
}

function replaceCookie(cookieHeader: string, name: string, value: string): string {
  const parts   = cookieHeader.split(";").map((c) => c.trim()).filter(Boolean);
  const without = parts.filter((c) => !c.toLowerCase().startsWith(`${name.toLowerCase()}=`));
  without.push(`${name}=${value}`);
  return without.join("; ");
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};

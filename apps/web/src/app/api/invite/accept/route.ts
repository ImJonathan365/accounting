import { NextRequest, NextResponse } from "next/server";
import { apiClient } from "@/lib/api-client";

const COOKIE_OPTS = {
  httpOnly: true,
  path: "/",
  sameSite: "lax" as const,
  secure: process.env.NODE_ENV === "production",
};

export async function GET(request: NextRequest) {
  const token     = request.nextUrl.searchParams.get("token") ?? "";
  const authToken = request.cookies.get("auth_token")?.value;

  if (!token) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  if (!authToken) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("next", `/api/invite/accept?token=${token}`);
    return NextResponse.redirect(loginUrl);
  }

  try {
    const info = await apiClient.invitations.getInfo(token);

    if (!info.isValid) {
      return NextResponse.redirect(new URL(`/invite?token=${token}`, request.url));
    }

    await apiClient.invitations.accept(token, authToken);
    const res = await apiClient.auth.switchOrg({ orgId: info.orgId }, authToken);

    const response = NextResponse.redirect(new URL("/dashboard", request.url));
    response.cookies.set("auth_token",     res.accessToken,                              { ...COOKIE_OPTS, maxAge: res.expiresIn });
    response.cookies.set("refresh_token",  res.refreshToken,                             { ...COOKIE_OPTS, maxAge: res.refreshExpiresIn });
    response.cookies.set("org_id",         res.organization.id,                          { ...COOKIE_OPTS, maxAge: res.expiresIn });
    response.cookies.set("display_name",   `${res.user.firstName} ${res.user.lastName}`, { ...COOKIE_OPTS, maxAge: res.expiresIn });
    response.cookies.set("user_id",        res.user.id,                                  { ...COOKIE_OPTS, maxAge: res.expiresIn });
    response.cookies.set("member_role",    res.organization.role,                        { ...COOKIE_OPTS, maxAge: res.expiresIn });
    response.cookies.set("email_verified", res.user.emailVerified ? "true" : "false",    { ...COOKIE_OPTS, maxAge: res.expiresIn });

    return response;
  } catch {
    return NextResponse.redirect(new URL(`/invite?token=${token}`, request.url));
  }
}

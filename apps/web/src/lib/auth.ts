import { cookies } from "next/headers";

const TOKEN_COOKIE          = "auth_token";
const ORG_COOKIE            = "org_id";
const DISPLAY_NAME_COOKIE   = "display_name";
export const USER_ID_COOKIE   = "user_id";
export const ROLE_COOKIE      = "member_role";
export const REFRESH_COOKIE   = "refresh_token";

export async function getServerToken(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(TOKEN_COOKIE)?.value;
}

export async function getCurrentOrgId(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(ORG_COOKIE)?.value;
}

export async function getDisplayName(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(DISPLAY_NAME_COOKIE)?.value;
}

export async function getCurrentUserId(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(USER_ID_COOKIE)?.value;
}

export async function getCurrentUserRole(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(ROLE_COOKIE)?.value;
}

export async function getRefreshToken(): Promise<string | undefined> {
  const cookieStore = await cookies();
  return cookieStore.get(REFRESH_COOKIE)?.value;
}

import { cookies } from "next/headers";

const TOKEN_COOKIE = "auth_token";
const ORG_COOKIE = "org_id";
const DISPLAY_NAME_COOKIE = "display_name";

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

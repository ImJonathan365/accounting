"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { apiClient } from "./api-client";
import type { CreateAccountRequest, LoginRequest, RegisterRequest, UpdateProfileRequest, CreateJournalEntryRequest, VoidJournalEntryRequest, UpdateOrgSettingsRequest } from "@accounting/types";
import { getServerToken, getCurrentOrgId } from "./auth";

const TOKEN_COOKIE = "auth_token";
const ORG_COOKIE = "org_id";
const DISPLAY_NAME_COOKIE = "display_name";

function cookieOpts(maxAge: number) {
  return {
    httpOnly: false,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    maxAge,
  };
}

export async function loginAction(data: LoginRequest) {
  const res = await apiClient.auth.login(data);
  const store = await cookies();

  store.set(TOKEN_COOKIE, res.accessToken, { ...cookieOpts(res.expiresIn), httpOnly: true });
  store.set(ORG_COOKIE, res.organization.id, { ...cookieOpts(res.expiresIn), httpOnly: true });
  store.set(DISPLAY_NAME_COOKIE, `${res.user.firstName} ${res.user.lastName}`, cookieOpts(res.expiresIn));

  redirect("/dashboard");
}

export async function registerAction(data: RegisterRequest) {
  const res = await apiClient.auth.register(data);
  const store = await cookies();

  store.set(TOKEN_COOKIE, res.accessToken, { ...cookieOpts(res.expiresIn), httpOnly: true });
  store.set(ORG_COOKIE, res.organization.id, { ...cookieOpts(res.expiresIn), httpOnly: true });
  store.set(DISPLAY_NAME_COOKIE, `${res.user.firstName} ${res.user.lastName}`, cookieOpts(res.expiresIn));

  redirect("/dashboard");
}

export async function logoutAction() {
  const store = await cookies();
  store.delete(TOKEN_COOKIE);
  store.delete(ORG_COOKIE);
  store.delete(DISPLAY_NAME_COOKIE);
  redirect("/login");
}

export async function createAccountAction(data: CreateAccountRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  await apiClient.accounts.create(orgId, data, token);
  revalidatePath("/dashboard/accounts");
}

export async function createJournalEntryAction(data: CreateJournalEntryRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const entry = await apiClient.journal.create(orgId, data, token);
  revalidatePath("/dashboard/journal");
  return entry;
}

export async function voidJournalEntryAction(entryId: string, data: VoidJournalEntryRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const result = await apiClient.journal.void(orgId, entryId, data, token);
  revalidatePath("/dashboard/journal");
  revalidatePath(`/dashboard/journal/${entryId}`);
  return result;
}

export async function updateOrgSettingsAction(data: UpdateOrgSettingsRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const result = await apiClient.settings.update(orgId, data, token);
  revalidatePath("/dashboard/settings");
  return result;
}

export async function updateProfileAction(data: UpdateProfileRequest) {
  const token = await getServerToken();
  if (!token) throw new Error("No autenticado");

  const profile = await apiClient.users.updateProfile(data, token);

  const store = await cookies();
  const existing = store.get(DISPLAY_NAME_COOKIE);
  if (existing) {
    store.set(DISPLAY_NAME_COOKIE, `${profile.firstName} ${profile.lastName}`, cookieOpts(3600));
  }

  revalidatePath("/dashboard/profile");
  return profile;
}

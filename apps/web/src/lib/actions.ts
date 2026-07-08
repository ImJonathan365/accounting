"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { apiClient } from "./api-client";
import type { CreateAccountRequest, UpdateAccountRequest, LoginRequest, RegisterRequest, UpdateProfileRequest, CreateJournalEntryRequest, UpdateJournalEntryRequest, VoidJournalEntryRequest, UpdateOrgSettingsRequest, InviteMemberRequest, UpdateMemberRoleRequest } from "@accounting/types";
import { getServerToken, getCurrentOrgId, getCurrentUserId, getRefreshToken, USER_ID_COOKIE, ROLE_COOKIE, REFRESH_COOKIE } from "./auth";

const TOKEN_COOKIE           = "auth_token";
const ORG_COOKIE             = "org_id";
const DISPLAY_NAME_COOKIE    = "display_name";
const EMAIL_VERIFIED_COOKIE  = "email_verified";

function cookieOpts(maxAge: number, httpOnly = true) {
  return {
    httpOnly,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    maxAge,
  };
}

import type { AuthResponse } from "@accounting/types";

async function setAuthCookies(res: AuthResponse) {
  const store = await cookies();
  store.set(TOKEN_COOKIE,          res.accessToken,                                cookieOpts(res.expiresIn));
  store.set(REFRESH_COOKIE,        res.refreshToken,                               cookieOpts(res.refreshExpiresIn));
  store.set(ORG_COOKIE,            res.organization.id,                            cookieOpts(res.expiresIn));
  store.set(DISPLAY_NAME_COOKIE,   `${res.user.firstName} ${res.user.lastName}`,   cookieOpts(res.expiresIn));
  store.set(USER_ID_COOKIE,        res.user.id,                                    cookieOpts(res.expiresIn));
  store.set(ROLE_COOKIE,           res.organization.role,                          cookieOpts(res.expiresIn));
  store.set(EMAIL_VERIFIED_COOKIE, res.user.emailVerified ? 'true' : 'false',      cookieOpts(res.expiresIn));
}

export async function loginAction(data: LoginRequest, next?: string) {
  const res = await apiClient.auth.login(data);
  await setAuthCookies(res);
  // next must start with "/" to prevent open redirect
  redirect(next && next.startsWith("/") ? next : "/dashboard");
}

export async function registerAction(data: RegisterRequest) {
  const res = await apiClient.auth.register(data);
  await setAuthCookies(res);
  redirect("/verify-email?sent=true");
}

export async function changePasswordAction(currentPassword: string, newPassword: string, confirmNewPassword: string) {
  const token = await getServerToken();
  if (!token) throw new Error("No autenticado");
  await apiClient.users.changePassword(currentPassword, newPassword, confirmNewPassword, token);
}

export async function deleteAccountAction(password: string) {
  const token = await getServerToken();
  if (!token) throw new Error("No autenticado");
  await apiClient.users.deleteAccount(password, token);
  const store = await cookies();
  store.delete(TOKEN_COOKIE);
  store.delete(REFRESH_COOKIE);
  store.delete(ORG_COOKIE);
  store.delete(DISPLAY_NAME_COOKIE);
  store.delete(USER_ID_COOKIE);
  store.delete(ROLE_COOKIE);
  store.delete(EMAIL_VERIFIED_COOKIE);
  redirect("/login");
}

export async function forgotPasswordAction(email: string) {
  await apiClient.auth.forgotPassword(email);
}

export async function resetPasswordAction(token: string, newPassword: string, confirmNewPassword: string) {
  await apiClient.auth.resetPassword(token, newPassword, confirmNewPassword);
}

export async function verifyEmailAction(token: string) {
  await apiClient.auth.verifyEmail(token);
  const store = await cookies();
  // Mark email as verified so middleware unlocks the dashboard
  store.set(EMAIL_VERIFIED_COOKIE, 'true', cookieOpts(60 * 60 * 24 * 30));
}

export async function resendVerificationAction(email: string) {
  await apiClient.auth.resendVerification(email);
}

export async function switchOrgAction(orgId: string) {
  const token = await getServerToken();
  if (!token) redirect("/login");

  const res = await apiClient.auth.switchOrg({ orgId }, token);
  await setAuthCookies(res);
  redirect("/dashboard");
}

export async function logoutAction() {
  const refreshToken = await getRefreshToken();
  if (refreshToken) {
    await apiClient.auth.logout(refreshToken).catch(() => { /* best-effort */ });
  }
  const store = await cookies();
  store.delete(TOKEN_COOKIE);
  store.delete(REFRESH_COOKIE);
  store.delete(ORG_COOKIE);
  store.delete(DISPLAY_NAME_COOKIE);
  store.delete(USER_ID_COOKIE);
  store.delete(ROLE_COOKIE);
  store.delete(EMAIL_VERIFIED_COOKIE);
  redirect("/login");
}

export async function createAccountAction(data: CreateAccountRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  await apiClient.accounts.create(orgId, data, token);
  revalidatePath("/dashboard/accounts");
}

export async function updateAccountAction(id: string, data: UpdateAccountRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  await apiClient.accounts.update(orgId, id, data, token);
  revalidatePath("/dashboard/accounts");
}

export async function toggleAccountActiveAction(id: string) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  await apiClient.accounts.toggle(orgId, id, token);
  revalidatePath("/dashboard/accounts");
}

export async function createJournalEntryAction(data: CreateJournalEntryRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const entry = await apiClient.journal.create(orgId, data, token);
  revalidatePath("/dashboard/journal");
  return entry;
}

export async function updateJournalEntryAction(entryId: string, data: UpdateJournalEntryRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const result = await apiClient.journal.update(orgId, entryId, data, token);
  revalidatePath("/dashboard/journal");
  revalidatePath(`/dashboard/journal/${entryId}`);
  return result;
}

export async function deleteJournalEntryAction(entryId: string) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  await apiClient.journal.delete(orgId, entryId, token);
  revalidatePath("/dashboard/journal");
}

export async function postJournalEntryAction(entryId: string) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const result = await apiClient.journal.post(orgId, entryId, token);
  revalidatePath("/dashboard/journal");
  revalidatePath(`/dashboard/journal/${entryId}`);
  return result;
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

export async function acceptInvitationAction(token: string, orgId: string) {
  const authToken = await getServerToken();
  if (!authToken) redirect("/login?next=" + encodeURIComponent(`/invite?token=${token}`));
  await apiClient.invitations.accept(token, authToken);
  // Switch active org to the one just joined, then go to dashboard
  const res = await apiClient.auth.switchOrg({ orgId }, authToken);
  await setAuthCookies(res);
  redirect("/dashboard");
}

export async function declineInvitationAction(token: string) {
  await apiClient.invitations.decline(token);
}

export async function inviteMemberAction(data: InviteMemberRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const result = await apiClient.members.invite(orgId, data, token);
  revalidatePath("/dashboard/team");
  return result;
}

export async function updateMemberRoleAction(userId: string, data: UpdateMemberRoleRequest) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  const result = await apiClient.members.updateRole(orgId, userId, data, token);
  revalidatePath("/dashboard/team");
  return result;
}

export async function removeMemberAction(userId: string) {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) throw new Error("No autenticado");

  await apiClient.members.remove(orgId, userId, token);
  revalidatePath("/dashboard/team");
}

export async function updateProfileAction(data: UpdateProfileRequest) {
  const token = await getServerToken();
  if (!token) throw new Error("No autenticado");

  const profile = await apiClient.users.updateProfile(data, token);

  const store    = await cookies();
  const existing = store.get(DISPLAY_NAME_COOKIE);
  if (existing) {
    store.set(DISPLAY_NAME_COOKIE, `${profile.firstName} ${profile.lastName}`,
      { ...cookieOpts(jwtRemainingSeconds(token)), httpOnly: true });
  }

  revalidatePath("/dashboard/profile");
  return profile;
}

function jwtRemainingSeconds(token: string): number {
  try {
    const payload = JSON.parse(Buffer.from(token.split(".")[1], "base64url").toString());
    return Math.max(60, (payload.exp as number) - Math.floor(Date.now() / 1000));
  } catch {
    return 3600;
  }
}

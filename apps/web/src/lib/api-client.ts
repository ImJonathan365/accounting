import { apiBaseUrl } from "./env";
import type { AuthResponse, LoginRequest, RegisterRequest, UserProfile, UpdateProfileRequest } from "@accounting/types";
import type { Account, CreateAccountRequest, UpdateAccountRequest } from "@accounting/types";
import type { JournalEntry, JournalEntrySummary, CreateJournalEntryRequest, VoidJournalEntryRequest, PagedResult } from "@accounting/types";
import type { TrialBalance, IncomeStatement, BalanceSheet, DashboardSummary } from "@accounting/types";
import type { OrgSettings, UpdateOrgSettingsRequest } from "@accounting/types";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fieldErrors?: Record<string, string[]>
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const res = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers: { ...headers, ...(options.headers as Record<string, string>) },
    cache: "no-store",
  });

  if (!res.ok) {
    const body = await res.json().catch(() => null);
    if (body?.errors) {
      throw new ApiError(res.status, body.title ?? "Error de validación.", body.errors);
    }
    throw new ApiError(res.status, body?.title ?? `Error ${res.status} en ${path}`);
  }

  return res.json() as Promise<T>;
}

export const apiClient = {
  auth: {
    login: (data: LoginRequest) =>
      request<AuthResponse>("/api/auth/login", { method: "POST", body: JSON.stringify(data) }),

    register: (data: RegisterRequest) =>
      request<AuthResponse>("/api/auth/register", { method: "POST", body: JSON.stringify(data) }),
  },

  users: {
    getProfile: (token: string) =>
      request<UserProfile>("/api/users/me", {}, token),

    updateProfile: (data: UpdateProfileRequest, token: string) =>
      request<UserProfile>("/api/users/me", { method: "PUT", body: JSON.stringify(data) }, token),
  },

  accounts: {
    list: (orgId: string, token: string) =>
      request<Account[]>(`/api/organizations/${orgId}/accounts`, {}, token),

    create: (orgId: string, data: CreateAccountRequest, token: string) =>
      request<Account>(`/api/organizations/${orgId}/accounts`, { method: "POST", body: JSON.stringify(data) }, token),

    update: (orgId: string, id: string, data: UpdateAccountRequest, token: string) =>
      request<Account>(`/api/organizations/${orgId}/accounts/${id}`, { method: "PUT", body: JSON.stringify(data) }, token),

    toggle: (orgId: string, id: string, token: string) =>
      request<Account>(`/api/organizations/${orgId}/accounts/${id}/toggle`, { method: "PATCH" }, token),
  },

  journal: {
    list: (orgId: string, token: string, page = 1, pageSize = 25) =>
      request<PagedResult<JournalEntrySummary>>(
        `/api/organizations/${orgId}/journal-entries?page=${page}&pageSize=${pageSize}`, {}, token),

    get: (orgId: string, id: string, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries/${id}`, {}, token),

    create: (orgId: string, data: CreateJournalEntryRequest, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries`, { method: "POST", body: JSON.stringify(data) }, token),

    void: (orgId: string, id: string, data: VoidJournalEntryRequest, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries/${id}/void`, { method: "POST", body: JSON.stringify(data) }, token),
  },

  dashboard: {
    getSummary: (orgId: string, token: string) =>
      request<DashboardSummary>(`/api/organizations/${orgId}/dashboard`, {}, token),
  },

  reports: {
    trialBalance: (orgId: string, from: string, to: string, token: string) =>
      request<TrialBalance>(
        `/api/organizations/${orgId}/reports/trial-balance?from=${from}&to=${to}`, {}, token),

    incomeStatement: (orgId: string, from: string, to: string, token: string) =>
      request<IncomeStatement>(
        `/api/organizations/${orgId}/reports/income-statement?from=${from}&to=${to}`, {}, token),

    balanceSheet: (orgId: string, asOf: string, token: string) =>
      request<BalanceSheet>(
        `/api/organizations/${orgId}/reports/balance-sheet?asOf=${asOf}`, {}, token),
  },

  settings: {
    get: (orgId: string, token: string) =>
      request<OrgSettings>(`/api/organizations/${orgId}/settings`, {}, token),

    update: (orgId: string, data: UpdateOrgSettingsRequest, token: string) =>
      request<OrgSettings>(
        `/api/organizations/${orgId}/settings`,
        { method: "PUT", body: JSON.stringify(data) },
        token),
  },

  export: {
    download: async (path: string, token: string): Promise<Blob> => {
      const res = await fetch(`${apiBaseUrl}${path}`, {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
      });
      if (!res.ok) throw new ApiError(res.status, `Error ${res.status} al exportar`);
      return res.blob();
    },
  },

  get: <T>(path: string, token: string) => request<T>(path, {}, token),
  post: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, token),
  put: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }, token),
};

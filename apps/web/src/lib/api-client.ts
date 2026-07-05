import { apiBaseUrl } from "./env";
import type { AuthResponse, LoginRequest, RegisterRequest, UserProfile, UpdateProfileRequest, UserOrg, SwitchOrgRequest } from "@accounting/types";
import type { AuditLog } from "@accounting/types";
import type { Account, CreateAccountRequest, UpdateAccountRequest } from "@accounting/types";
import type { JournalEntry, JournalEntrySummary, CreateJournalEntryRequest, UpdateJournalEntryRequest, VoidJournalEntryRequest, PagedResult } from "@accounting/types";
import type { Member, InviteMemberRequest, UpdateMemberRoleRequest } from "@accounting/types";
import type { TrialBalance, IncomeStatement, BalanceSheet, DashboardSummary, Ledger } from "@accounting/types";
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

function statusMessage(status: number): string {
  switch (status) {
    case 400: return "La solicitud contiene datos inválidos.";
    case 401: return "Tu sesión ha expirado. Por favor inicia sesión de nuevo.";
    case 403: return "No tienes permisos para realizar esta acción.";
    case 404: return "El recurso solicitado no existe.";
    case 409: return "Hay un conflicto con el estado actual del recurso.";
    case 422: return "No se pudo procesar la solicitud. Verifica los datos.";
    case 429: return "Demasiadas solicitudes. Espera un momento antes de intentar de nuevo.";
    case 500: return "Error interno del servidor. Intenta de nuevo más tarde.";
    default:  return "Ocurrió un error inesperado. Intenta de nuevo.";
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
    const fallback = statusMessage(res.status);
    throw new ApiError(res.status, body?.title ?? fallback);
  }

  if (res.status === 204 || res.headers.get("content-length") === "0") {
    return undefined as T;
  }

  return res.json() as Promise<T>;
}

export const apiClient = {
  auth: {
    login: (data: LoginRequest) =>
      request<AuthResponse>("/api/auth/login", { method: "POST", body: JSON.stringify(data) }),

    register: (data: RegisterRequest) =>
      request<AuthResponse>("/api/auth/register", { method: "POST", body: JSON.stringify(data) }),

    switchOrg: (data: SwitchOrgRequest, token: string) =>
      request<AuthResponse>("/api/auth/switch-org", { method: "POST", body: JSON.stringify(data) }, token),

    refresh: (refreshToken: string) =>
      request<AuthResponse>("/api/auth/refresh", { method: "POST", body: JSON.stringify({ refreshToken }) }),

    logout: (refreshToken: string) =>
      request<void>("/api/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken }) }),
  },

  users: {
    getProfile: (token: string) =>
      request<UserProfile>("/api/users/me", {}, token),

    updateProfile: (data: UpdateProfileRequest, token: string) =>
      request<UserProfile>("/api/users/me", { method: "PUT", body: JSON.stringify(data) }, token),

    listOrgs: (token: string) =>
      request<UserOrg[]>("/api/users/me/organizations", {}, token),
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
    list: (orgId: string, token: string, page = 1, pageSize = 25, filters?: {
      from?: string; to?: string; status?: string; search?: string;
    }) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (filters?.from)   params.set("from",   filters.from);
      if (filters?.to)     params.set("to",     filters.to);
      if (filters?.status) params.set("status", filters.status);
      if (filters?.search) params.set("search", filters.search);
      return request<PagedResult<JournalEntrySummary>>(
        `/api/organizations/${orgId}/journal-entries?${params}`, {}, token);
    },

    get: (orgId: string, id: string, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries/${id}`, {}, token),

    create: (orgId: string, data: CreateJournalEntryRequest, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries`, { method: "POST", body: JSON.stringify(data) }, token),

    update: (orgId: string, id: string, data: UpdateJournalEntryRequest, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries/${id}`, { method: "PUT", body: JSON.stringify(data) }, token),

    delete: (orgId: string, id: string, token: string) =>
      request<void>(`/api/organizations/${orgId}/journal-entries/${id}`, { method: "DELETE" }, token),

    post: (orgId: string, id: string, token: string) =>
      request<JournalEntry>(`/api/organizations/${orgId}/journal-entries/${id}/post`, { method: "POST" }, token),

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

    ledger: (orgId: string, accountId: string, from: string, to: string, token: string) =>
      request<Ledger>(
        `/api/organizations/${orgId}/reports/ledger?accountId=${accountId}&from=${from}&to=${to}`, {}, token),
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

  members: {
    list: (orgId: string, token: string) =>
      request<Member[]>(`/api/organizations/${orgId}/members`, {}, token),
    invite: (orgId: string, data: InviteMemberRequest, token: string) =>
      request<Member>(`/api/organizations/${orgId}/members`, { method: "POST", body: JSON.stringify(data) }, token),
    updateRole: (orgId: string, userId: string, data: UpdateMemberRoleRequest, token: string) =>
      request<Member>(`/api/organizations/${orgId}/members/${userId}/role`, { method: "PUT", body: JSON.stringify(data) }, token),
    remove: (orgId: string, userId: string, token: string) =>
      request<void>(`/api/organizations/${orgId}/members/${userId}`, { method: "DELETE" }, token),
  },

  audit: {
    list: (orgId: string, token: string, page = 1, pageSize = 50) =>
      request<PagedResult<AuditLog>>(
        `/api/organizations/${orgId}/audit?page=${page}&pageSize=${pageSize}`, {}, token),
  },

  get: <T>(path: string, token: string) => request<T>(path, {}, token),
  post: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, token),
  put: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }, token),
};

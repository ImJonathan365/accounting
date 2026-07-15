import { apiBaseUrl } from "./env";
import type { AuthResponse, LoginRequest, RegisterRequest, UserProfile, UpdateProfileRequest, UserOrg, SwitchOrgRequest } from "@accounting/types";
import type { AuditLog } from "@accounting/types";
import type { Account, CreateAccountRequest, UpdateAccountRequest } from "@accounting/types";
import type { JournalEntry, JournalEntrySummary, CreateJournalEntryRequest, UpdateJournalEntryRequest, VoidJournalEntryRequest, PagedResult } from "@accounting/types";
import type { Member, InviteMemberRequest, UpdateMemberRoleRequest, InvitationInfo } from "@accounting/types";
import type { TrialBalance, IncomeStatement, BalanceSheet, DashboardSummary, Ledger, CashFlow } from "@accounting/types";
import type { Period, ClosePeriodRequest } from "@accounting/types";
import type { OrgSettings, UpdateOrgSettingsRequest } from "@accounting/types";
import type { YearEndStatus, YearEndCloseRequest } from "@accounting/types";
import type { RecurringEntry, CreateRecurringEntryRequest, UpdateRecurringEntryRequest, GeneratePendingResult } from "@accounting/types";
import type { BankAccount, BankReconciliation, BankTransaction, CreateBankAccountRequest, ImportBankTransactionRow } from "@accounting/types";
import type { Contact, Invoice, CreateContactRequest, UpdateContactRequest, CreateInvoiceRequest, CreatePaymentRequest } from "@accounting/types";
import type { Budget, BudgetVsActual, CreateBudgetRequest, UpsertBudgetLineRequest } from "@accounting/types";
import type { TaxRate, CreateTaxRateRequest, Product, CreateProductRequest } from "@accounting/types";
import type { OverdueInvoice, SetOpeningBalancesRequest, OpeningBalanceResult } from "@accounting/types";

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

    forgotPassword: (email: string) =>
      request<{ message: string }>("/api/auth/forgot-password", { method: "POST", body: JSON.stringify({ email }) }),

    resetPassword: (token: string, newPassword: string, confirmNewPassword: string) =>
      request<{ message: string }>("/api/auth/reset-password", { method: "POST", body: JSON.stringify({ token, newPassword, confirmNewPassword }) }),

    verifyEmail: (token: string) =>
      request<{ message: string }>("/api/auth/verify-email", { method: "POST", body: JSON.stringify({ token }) }),

    resendVerification: (email: string) =>
      request<{ message: string }>("/api/auth/resend-verification", { method: "POST", body: JSON.stringify({ email }) }),
  },

  users: {
    getProfile: (token: string) =>
      request<UserProfile>("/api/users/me", {}, token),

    updateProfile: (data: UpdateProfileRequest, token: string) =>
      request<UserProfile>("/api/users/me", { method: "PUT", body: JSON.stringify(data) }, token),

    listOrgs: (token: string) =>
      request<UserOrg[]>("/api/users/me/organizations", {}, token),

    changePassword: (currentPassword: string, newPassword: string, confirmNewPassword: string, token: string) =>
      request<void>("/api/users/me/change-password", { method: "POST", body: JSON.stringify({ currentPassword, newPassword, confirmNewPassword }) }, token),

    deleteAccount: (password: string, token: string) =>
      request<void>("/api/users/me", { method: "DELETE", body: JSON.stringify({ password }) }, token),
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

    ledger: (orgId: string, accountId: string, from: string, to: string, token: string, page = 1, pageSize = 50) =>
      request<Ledger>(
        `/api/organizations/${orgId}/reports/ledger?accountId=${accountId}&from=${from}&to=${to}&page=${page}&pageSize=${pageSize}`, {}, token),

    cashFlow: (orgId: string, from: string, to: string, token: string) =>
      request<CashFlow>(
        `/api/organizations/${orgId}/reports/cash-flow?from=${from}&to=${to}`, {}, token),
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
      request<void>(`/api/organizations/${orgId}/members`, { method: "POST", body: JSON.stringify(data) }, token),
    updateRole: (orgId: string, userId: string, data: UpdateMemberRoleRequest, token: string) =>
      request<Member>(`/api/organizations/${orgId}/members/${userId}/role`, { method: "PUT", body: JSON.stringify(data) }, token),
    remove: (orgId: string, userId: string, token: string) =>
      request<void>(`/api/organizations/${orgId}/members/${userId}`, { method: "DELETE" }, token),
  },

  invitations: {
    getInfo: (token: string) =>
      request<InvitationInfo>(`/api/invitations/${encodeURIComponent(token)}`),
    accept: (token: string, authToken: string) =>
      request<Member>(`/api/invitations/${encodeURIComponent(token)}/accept`, { method: "POST" }, authToken),
    decline: (token: string) =>
      request<void>(`/api/invitations/${encodeURIComponent(token)}/decline`, { method: "POST" }),
  },

  audit: {
    list: (orgId: string, token: string, page = 1, pageSize = 50) =>
      request<PagedResult<AuditLog>>(
        `/api/organizations/${orgId}/audit?page=${page}&pageSize=${pageSize}`, {}, token),
  },

  periods: {
    list: (orgId: string, token: string, year?: number) =>
      request<Period[]>(
        `/api/organizations/${orgId}/periods${year ? `?year=${year}` : ""}`, {}, token),
    close: (orgId: string, data: ClosePeriodRequest, token: string) =>
      request<Period>(
        `/api/organizations/${orgId}/periods/close`,
        { method: "POST", body: JSON.stringify(data) }, token),
    reopen: (orgId: string, year: number, month: number, token: string) =>
      request<void>(
        `/api/organizations/${orgId}/periods/${year}/${month}`,
        { method: "DELETE" }, token),
  },

  yearEnd: {
    getStatus: (orgId: string, year: number, token: string) =>
      request<YearEndStatus>(`/api/organizations/${orgId}/year-end/${year}`, {}, token),
    close: (orgId: string, data: YearEndCloseRequest, token: string) =>
      request<YearEndStatus>(
        `/api/organizations/${orgId}/year-end/close`,
        { method: "POST", body: JSON.stringify(data) }, token),
  },

  recurring: {
    list: (orgId: string, token: string) =>
      request<RecurringEntry[]>(`/api/organizations/${orgId}/recurring-entries`, {}, token),
    getById: (orgId: string, id: string, token: string) =>
      request<RecurringEntry>(`/api/organizations/${orgId}/recurring-entries/${id}`, {}, token),
    create: (orgId: string, data: CreateRecurringEntryRequest, token: string) =>
      request<RecurringEntry>(
        `/api/organizations/${orgId}/recurring-entries`,
        { method: "POST", body: JSON.stringify(data) }, token),
    update: (orgId: string, id: string, data: UpdateRecurringEntryRequest, token: string) =>
      request<RecurringEntry>(
        `/api/organizations/${orgId}/recurring-entries/${id}`,
        { method: "PATCH", body: JSON.stringify(data) }, token),
    delete: (orgId: string, id: string, token: string) =>
      request<void>(
        `/api/organizations/${orgId}/recurring-entries/${id}`,
        { method: "DELETE" }, token),
    generatePending: (orgId: string, token: string) =>
      request<GeneratePendingResult>(
        `/api/organizations/${orgId}/recurring-entries/generate-pending`,
        { method: "POST" }, token),
  },

  bank: {
    list: (orgId: string, token: string) =>
      request<BankAccount[]>(`/api/organizations/${orgId}/bank-accounts`, {}, token),
    create: (orgId: string, data: CreateBankAccountRequest, token: string) =>
      request<BankAccount>(`/api/organizations/${orgId}/bank-accounts`, { method: "POST", body: JSON.stringify(data) }, token),
    getReconciliation: (orgId: string, bankAccountId: string, token: string, page = 1, pageSize = 50) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      return request<BankReconciliation>(`/api/organizations/${orgId}/bank-accounts/${bankAccountId}/reconciliation?${params}`, {}, token);
    },
    import: (orgId: string, bankAccountId: string, rows: ImportBankTransactionRow[], token: string) =>
      request<BankTransaction[]>(`/api/organizations/${orgId}/bank-accounts/${bankAccountId}/transactions/import`, { method: "POST", body: JSON.stringify(rows) }, token),
    match: (orgId: string, bankAccountId: string, txId: string, journalEntryId: string, token: string) =>
      request<BankTransaction>(`/api/organizations/${orgId}/bank-accounts/${bankAccountId}/transactions/${txId}/match`, { method: "PATCH", body: JSON.stringify({ journalEntryId }) }, token),
    exclude: (orgId: string, bankAccountId: string, txId: string, token: string) =>
      request<BankTransaction>(`/api/organizations/${orgId}/bank-accounts/${bankAccountId}/transactions/${txId}/exclude`, { method: "PATCH" }, token),
    unmatch: (orgId: string, bankAccountId: string, txId: string, token: string) =>
      request<BankTransaction>(`/api/organizations/${orgId}/bank-accounts/${bankAccountId}/transactions/${txId}/unmatch`, { method: "PATCH" }, token),
  },

  contacts: {
    list: (orgId: string, token: string, opts?: { type?: string; search?: string; page?: number; pageSize?: number }) => {
      const params = new URLSearchParams();
      if (opts?.type)     params.set("type",     opts.type);
      if (opts?.search)   params.set("search",   opts.search);
      if (opts?.page)     params.set("page",     String(opts.page));
      if (opts?.pageSize) params.set("pageSize", String(opts.pageSize));
      const qs = params.toString();
      return request<PagedResult<Contact>>(`/api/organizations/${orgId}/contacts${qs ? `?${qs}` : ""}`, {}, token);
    },
    getById: (orgId: string, id: string, token: string) =>
      request<Contact>(`/api/organizations/${orgId}/contacts/${id}`, {}, token),
    create: (orgId: string, data: CreateContactRequest, token: string) =>
      request<Contact>(`/api/organizations/${orgId}/contacts`, { method: "POST", body: JSON.stringify(data) }, token),
    update: (orgId: string, id: string, data: UpdateContactRequest, token: string) =>
      request<Contact>(`/api/organizations/${orgId}/contacts/${id}`, { method: "PATCH", body: JSON.stringify(data) }, token),
  },

  invoices: {
    list: (orgId: string, token: string, type?: string, status?: string, search?: string, from?: string, to?: string, page = 1, pageSize = 25) => {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (type)   params.set("type",   type);
      if (status) params.set("status", status);
      if (search) params.set("search", search);
      if (from)   params.set("from",   from);
      if (to)     params.set("to",     to);
      return request<PagedResult<Invoice>>(`/api/organizations/${orgId}/invoices?${params}`, {}, token);
    },
    getById: (orgId: string, id: string, token: string) =>
      request<Invoice>(`/api/organizations/${orgId}/invoices/${id}`, {}, token),
    create: (orgId: string, data: CreateInvoiceRequest, token: string) =>
      request<Invoice>(`/api/organizations/${orgId}/invoices`, { method: "POST", body: JSON.stringify(data) }, token),
    issue: (orgId: string, id: string, token: string) =>
      request<Invoice>(`/api/organizations/${orgId}/invoices/${id}/issue`, { method: "POST" }, token),
    recordPayment: (orgId: string, id: string, data: CreatePaymentRequest, token: string) =>
      request<Invoice>(`/api/organizations/${orgId}/invoices/${id}/payments`, { method: "POST", body: JSON.stringify(data) }, token),
    void: (orgId: string, id: string, token: string) =>
      request<Invoice>(`/api/organizations/${orgId}/invoices/${id}/void`, { method: "POST" }, token),
    pdfUrl: (orgId: string, id: string) =>
      `/api/organizations/${orgId}/invoices/${id}/pdf`,
    overdue: (orgId: string, token: string) =>
      request<OverdueInvoice[]>(`/api/organizations/${orgId}/invoices/overdue`, {}, token),
  },

  openingBalances: {
    set: (orgId: string, data: SetOpeningBalancesRequest, token: string) =>
      request<OpeningBalanceResult>(`/api/organizations/${orgId}/opening-balances`, { method: "POST", body: JSON.stringify(data) }, token),
  },

  taxRates: {
    list: (orgId: string, token: string) =>
      request<TaxRate[]>(`/api/organizations/${orgId}/tax-rates`, {}, token),
    create: (orgId: string, data: CreateTaxRateRequest, token: string) =>
      request<TaxRate>(`/api/organizations/${orgId}/tax-rates`, { method: "POST", body: JSON.stringify(data) }, token),
  },

  products: {
    list: (orgId: string, token: string) =>
      request<Product[]>(`/api/organizations/${orgId}/products`, {}, token),
    create: (orgId: string, data: CreateProductRequest, token: string) =>
      request<Product>(`/api/organizations/${orgId}/products`, { method: "POST", body: JSON.stringify(data) }, token),
    update: (orgId: string, id: string, data: Partial<CreateProductRequest> & { isActive?: boolean }, token: string) =>
      request<Product>(`/api/organizations/${orgId}/products/${id}`, { method: "PATCH", body: JSON.stringify(data) }, token),
  },

  budgets: {
    list: (orgId: string, token: string) =>
      request<Budget[]>(`/api/organizations/${orgId}/budgets`, {}, token),
    getById: (orgId: string, id: string, token: string) =>
      request<Budget>(`/api/organizations/${orgId}/budgets/${id}`, {}, token),
    create: (orgId: string, data: CreateBudgetRequest, token: string) =>
      request<Budget>(`/api/organizations/${orgId}/budgets`, { method: "POST", body: JSON.stringify(data) }, token),
    upsertLine: (orgId: string, id: string, data: UpsertBudgetLineRequest, token: string) =>
      request<Budget>(`/api/organizations/${orgId}/budgets/${id}/lines`, { method: "PUT", body: JSON.stringify(data) }, token),
    getVsActual: (orgId: string, id: string, token: string) =>
      request<BudgetVsActual>(`/api/organizations/${orgId}/budgets/${id}/vs-actual`, {}, token),
  },

  get: <T>(path: string, token: string) => request<T>(path, {}, token),
  post: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, token),
  put: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }, token),
};

import { apiBaseUrl } from "./env";
import type { AuthResponse, LoginRequest, RegisterRequest, UserProfile, UpdateProfileRequest } from "@accounting/types";
import type { Account, CreateAccountRequest } from "@accounting/types";

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
  },

  get: <T>(path: string, token: string) => request<T>(path, {}, token),
  post: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, token),
  put: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }, token),
};

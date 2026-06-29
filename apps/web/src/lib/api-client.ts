import { apiBaseUrl } from "./env";
import type { AuthResponse, LoginRequest, RegisterRequest } from "@accounting/types";

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string
): Promise<T> {
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
    const body = await res.text();
    throw new Error(`API ${res.status} ${path}: ${body}`);
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

  get: <T>(path: string, token: string) =>
    request<T>(path, {}, token),

  post: <T>(path: string, body: unknown, token: string) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }, token),
};

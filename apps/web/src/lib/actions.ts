"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { apiClient } from "./api-client";
import type { LoginRequest, RegisterRequest } from "@accounting/types";

const TOKEN_COOKIE = "auth_token";
const ORG_COOKIE = "org_id";

export async function loginAction(data: LoginRequest) {
  const res = await apiClient.auth.login(data);

  const cookieStore = await cookies();
  cookieStore.set(TOKEN_COOKIE, res.accessToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: res.expiresIn,
  });
  cookieStore.set(ORG_COOKIE, res.organization.id, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: res.expiresIn,
  });

  redirect("/dashboard");
}

export async function registerAction(data: RegisterRequest) {
  const res = await apiClient.auth.register(data);

  const cookieStore = await cookies();
  cookieStore.set(TOKEN_COOKIE, res.accessToken, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: res.expiresIn,
  });
  cookieStore.set(ORG_COOKIE, res.organization.id, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    maxAge: res.expiresIn,
  });

  redirect("/dashboard");
}

export async function logoutAction() {
  const cookieStore = await cookies();
  cookieStore.delete(TOKEN_COOKIE);
  cookieStore.delete(ORG_COOKIE);
  redirect("/login");
}

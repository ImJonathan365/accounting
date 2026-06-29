"use client";

import { useState } from "react";
import Link from "next/link";
import { loginAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

function validate(email: string, password: string) {
  const errors: Record<string, string> = {};
  if (!email) errors.email = "El email es requerido.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) errors.email = "Ingresa un email válido.";
  if (!password) errors.password = "La contraseña es requerida.";
  return errors;
}

export default function LoginPage() {
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const email = (form.get("email") as string).trim();
    const password = form.get("password") as string;

    const errors = validate(email, password);
    if (Object.keys(errors).length) { setFieldErrors(errors); return; }
    setFieldErrors({});
    setGlobalError(null);
    setLoading(true);

    try {
      await loginAction({ email, password });
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors) {
        const mapped: Record<string, string> = {};
        for (const [k, v] of Object.entries(err.fieldErrors)) mapped[k] = v[0];
        setFieldErrors(mapped);
      } else {
        setGlobalError(err instanceof Error ? err.message : "Error al iniciar sesión.");
      }
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Bienvenido</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Inicia sesión en tu cuenta</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
          {globalError && (
            <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
              {globalError}
            </div>
          )}

          <Field label="Email" error={fieldErrors.email}>
            <input
              name="email" type="email" autoComplete="email" placeholder="tu@email.com"
              className={input(!!fieldErrors.email)}
              onChange={() => setFieldErrors((p) => ({ ...p, email: "" }))}
            />
          </Field>

          <Field label="Contraseña" error={fieldErrors.password}>
            <input
              name="password" type="password" autoComplete="current-password"
              className={input(!!fieldErrors.password)}
              onChange={() => setFieldErrors((p) => ({ ...p, password: "" }))}
            />
          </Field>

          <button type="submit" disabled={loading} className={btn}>
            {loading ? "Ingresando…" : "Iniciar sesión"}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500 dark:text-slate-400">
          ¿No tienes cuenta?{" "}
          <Link href="/register" className="font-medium text-indigo-600 hover:underline dark:text-indigo-400">
            Regístrate
          </Link>
        </p>
      </div>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{error}</p>}
    </div>
  );
}

const input = (hasError: boolean) =>
  `block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 transition focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 ${
    hasError
      ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
      : "border-slate-300 focus:border-indigo-500 focus:ring-indigo-200 dark:border-slate-600 dark:focus:ring-indigo-900"
  }`;

const btn =
  "w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 transition dark:focus:ring-offset-slate-800";

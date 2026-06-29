"use client";

import { useState } from "react";
import Link from "next/link";
import { registerAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

function validate(data: Record<string, string>) {
  const e: Record<string, string> = {};
  if (!data.firstName) e.firstName = "El nombre es requerido.";
  if (!data.lastName) e.lastName = "El apellido es requerido.";
  if (!data.email) e.email = "El email es requerido.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) e.email = "Ingresa un email válido.";
  if (!data.password) e.password = "La contraseña es requerida.";
  else if (data.password.length < 8) e.password = "Mínimo 8 caracteres.";
  else if (!/[A-Z]/.test(data.password)) e.password = "Debe incluir al menos una mayúscula.";
  else if (!/[0-9]/.test(data.password)) e.password = "Debe incluir al menos un número.";
  if (!data.organizationName) e.organizationName = "El nombre de la organización es requerido.";
  return e;
}

export default function RegisterPage() {
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  function clearField(name: string) {
    setFieldErrors((p) => ({ ...p, [name]: "" }));
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = new FormData(e.currentTarget);
    const data = {
      firstName: (form.get("firstName") as string).trim(),
      lastName: (form.get("lastName") as string).trim(),
      email: (form.get("email") as string).trim(),
      password: form.get("password") as string,
      organizationName: (form.get("organizationName") as string).trim(),
    };

    const errors = validate(data);
    if (Object.keys(errors).length) { setFieldErrors(errors); return; }
    setFieldErrors({});
    setGlobalError(null);
    setLoading(true);

    try {
      await registerAction(data);
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors) {
        const mapped: Record<string, string> = {};
        for (const [k, v] of Object.entries(err.fieldErrors)) mapped[k] = v[0];
        setFieldErrors(mapped);
      } else {
        setGlobalError(err instanceof Error ? err.message : "Error al registrarse.");
      }
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Crear cuenta</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Comienza a gestionar tus finanzas</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
          {globalError && (
            <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
              {globalError}
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <Field label="Nombre" error={fieldErrors.firstName}>
              <input name="firstName" type="text" autoComplete="given-name" placeholder="Juan"
                className={input(!!fieldErrors.firstName)} onChange={() => clearField("firstName")} />
            </Field>
            <Field label="Apellido" error={fieldErrors.lastName}>
              <input name="lastName" type="text" autoComplete="family-name" placeholder="Pérez"
                className={input(!!fieldErrors.lastName)} onChange={() => clearField("lastName")} />
            </Field>
          </div>

          <Field label="Email" error={fieldErrors.email}>
            <input name="email" type="email" autoComplete="email" placeholder="tu@email.com"
              className={input(!!fieldErrors.email)} onChange={() => clearField("email")} />
          </Field>

          <Field label="Contraseña" error={fieldErrors.password}>
            <input name="password" type="password" autoComplete="new-password"
              className={input(!!fieldErrors.password)} onChange={() => clearField("password")} />
            <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">Mínimo 8 caracteres, una mayúscula y un número.</p>
          </Field>

          <Field label="Nombre de la organización" error={fieldErrors.organizationName}>
            <input name="organizationName" type="text" placeholder="Mis finanzas personales"
              className={input(!!fieldErrors.organizationName)} onChange={() => clearField("organizationName")} />
          </Field>

          <button type="submit" disabled={loading} className={btn}>
            {loading ? "Creando cuenta…" : "Crear cuenta"}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-500 dark:text-slate-400">
          ¿Ya tienes cuenta?{" "}
          <Link href="/login" className="font-medium text-indigo-600 hover:underline dark:text-indigo-400">
            Inicia sesión
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

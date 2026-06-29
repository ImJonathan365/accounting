"use client";

import { useState, useTransition } from "react";
import { updateProfileAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";
import type { UserProfile } from "@accounting/types";

function validate(first: string, last: string) {
  const e: Record<string, string> = {};
  if (!first.trim()) e.firstName = "El nombre es requerido.";
  if (!last.trim()) e.lastName = "El apellido es requerido.";
  return e;
}

export function ProfileForm({ profile }: { profile: UserProfile }) {
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [success, setSuccess] = useState(false);
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const els = e.currentTarget.elements;
    const firstName = (els.namedItem("firstName") as HTMLInputElement).value;
    const lastName = (els.namedItem("lastName") as HTMLInputElement).value;

    const errors = validate(firstName, lastName);
    if (Object.keys(errors).length) { setFieldErrors(errors); return; }
    setFieldErrors({});
    setGlobalError(null);
    setSuccess(false);

    startTransition(async () => {
      try {
        await updateProfileAction({ firstName: firstName.trim(), lastName: lastName.trim() });
        setSuccess(true);
      } catch (err) {
        if (err instanceof ApiError && err.fieldErrors) {
          const mapped: Record<string, string> = {};
          for (const [k, v] of Object.entries(err.fieldErrors)) mapped[k] = v[0];
          setFieldErrors(mapped);
        } else {
          setGlobalError(err instanceof Error ? err.message : "Error al actualizar el perfil.");
        }
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {globalError && (
        <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {globalError}
        </div>
      )}
      {success && (
        <div className="rounded-lg bg-green-50 px-4 py-3 text-sm text-green-700 dark:bg-green-900/30 dark:text-green-400">
          Perfil actualizado correctamente.
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label="Nombre" error={fieldErrors.firstName}>
          <input
            name="firstName" type="text" defaultValue={profile.firstName}
            className={input(!!fieldErrors.firstName)}
            onChange={() => setFieldErrors((p) => ({ ...p, firstName: "" }))}
          />
        </Field>
        <Field label="Apellido" error={fieldErrors.lastName}>
          <input
            name="lastName" type="text" defaultValue={profile.lastName}
            className={input(!!fieldErrors.lastName)}
            onChange={() => setFieldErrors((p) => ({ ...p, lastName: "" }))}
          />
        </Field>
      </div>

      <Field label="Email">
        <input
          type="email" value={profile.email} readOnly
          className="block w-full cursor-not-allowed rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-400 dark:border-slate-600 dark:bg-slate-700/50 dark:text-slate-500"
        />
        <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">El email no se puede modificar por ahora.</p>
      </Field>

      <div className="pt-2">
        <button type="submit" disabled={isPending} className={btn}>
          {isPending ? "Guardando…" : "Guardar cambios"}
        </button>
      </div>
    </form>
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
  `block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 transition focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 ${
    hasError
      ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
      : "border-slate-300 focus:border-indigo-500 focus:ring-indigo-200 dark:border-slate-600 dark:focus:ring-indigo-900"
  }`;

const btn =
  "rounded-lg bg-indigo-600 px-5 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 disabled:opacity-50 transition focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 dark:focus:ring-offset-slate-800";

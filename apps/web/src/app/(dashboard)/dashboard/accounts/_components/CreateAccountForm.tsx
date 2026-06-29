"use client";

import { useState, useTransition } from "react";
import { createAccountAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";
import type { Account, AccountType, CreateAccountRequest } from "@accounting/types";

const ACCOUNT_TYPES: { value: AccountType; label: string }[] = [
  { value: "Asset", label: "Activo" },
  { value: "Liability", label: "Pasivo" },
  { value: "Equity", label: "Capital" },
  { value: "Income", label: "Ingreso" },
  { value: "Expense", label: "Gasto" },
];

function validate(code: string, name: string, type: string) {
  const e: Record<string, string> = {};
  if (!code.trim()) e.code = "El código es requerido.";
  else if (!/^[\w.\-]+$/.test(code.trim())) e.code = "Solo letras, números, puntos y guiones.";
  else if (code.trim().length > 20) e.code = "Máximo 20 caracteres.";
  if (!name.trim()) e.name = "El nombre es requerido.";
  if (!type) e.type = "El tipo es requerido.";
  return e;
}

export function CreateAccountForm({ accounts }: { accounts: Account[] }) {
  const [open, setOpen] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function clearField(name: string) {
    setFieldErrors((p) => ({ ...p, [name]: "" }));
  }

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const els = e.currentTarget.elements;
    const code = (els.namedItem("code") as HTMLInputElement).value;
    const name = (els.namedItem("name") as HTMLInputElement).value;
    const type = (els.namedItem("type") as HTMLSelectElement).value;
    const parentId = (els.namedItem("parentId") as HTMLSelectElement).value;
    const isPostable = (els.namedItem("isPostable") as HTMLInputElement).checked;

    const errors = validate(code, name, type);
    if (Object.keys(errors).length) { setFieldErrors(errors); return; }
    setFieldErrors({});
    setGlobalError(null);

    const data: CreateAccountRequest = {
      code: code.trim(),
      name: name.trim(),
      type: type as AccountType,
      isPostable,
      ...(parentId ? { parentId } : {}),
    };

    startTransition(async () => {
      try {
        await createAccountAction(data);
        e.currentTarget?.reset();
        setOpen(false);
      } catch (err) {
        if (err instanceof ApiError && err.fieldErrors) {
          const mapped: Record<string, string> = {};
          for (const [k, v] of Object.entries(err.fieldErrors)) mapped[k] = v[0];
          setFieldErrors(mapped);
        } else {
          setGlobalError(err instanceof Error ? err.message : "Error al crear la cuenta.");
        }
      }
    });
  }

  if (!open) {
    return (
      <button
        onClick={() => setOpen(true)}
        className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 transition-colors"
      >
        + Nueva cuenta
      </button>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <h3 className="mb-4 text-base font-semibold text-slate-900 dark:text-slate-100">Nueva cuenta</h3>

      {globalError && (
        <div className="mb-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {globalError}
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label="Código" error={fieldErrors.code}>
          <input name="code" placeholder="1.1.01" className={inp(!!fieldErrors.code)}
            onChange={() => clearField("code")} />
        </Field>

        <Field label="Nombre" error={fieldErrors.name}>
          <input name="name" placeholder="Caja general" className={inp(!!fieldErrors.name)}
            onChange={() => clearField("name")} />
        </Field>

        <Field label="Tipo" error={fieldErrors.type}>
          <select name="type" defaultValue="" className={inp(!!fieldErrors.type)}
            onChange={() => clearField("type")}>
            <option value="" disabled>Selecciona un tipo</option>
            {ACCOUNT_TYPES.map((t) => (
              <option key={t.value} value={t.value}>{t.label}</option>
            ))}
          </select>
        </Field>

        <Field label={<>Cuenta padre <span className="font-normal text-slate-400">(opcional)</span></>}>
          <select name="parentId" className={inp(false)}>
            <option value="">Sin cuenta padre</option>
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
            ))}
          </select>
        </Field>
      </div>

      <div className="mt-4 flex items-center gap-2">
        <input id="isPostable" name="isPostable" type="checkbox" defaultChecked
          className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 dark:border-slate-600" />
        <label htmlFor="isPostable" className="text-sm text-slate-700 dark:text-slate-300">
          Permite movimientos (cuenta postable)
        </label>
      </div>

      <div className="mt-6 flex gap-3">
        <button type="submit" disabled={isPending}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors">
          {isPending ? "Guardando…" : "Guardar"}
        </button>
        <button type="button" onClick={() => { setOpen(false); setFieldErrors({}); setGlobalError(null); }}
          className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700 transition-colors">
          Cancelar
        </button>
      </div>
    </form>
  );
}

function Field({ label, error, children }: { label: React.ReactNode; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{error}</p>}
    </div>
  );
}

const inp = (hasError: boolean) =>
  `block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 transition focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 ${
    hasError
      ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
      : "border-slate-300 focus:border-indigo-500 focus:ring-indigo-200 dark:border-slate-600 dark:focus:ring-indigo-900"
  }`;

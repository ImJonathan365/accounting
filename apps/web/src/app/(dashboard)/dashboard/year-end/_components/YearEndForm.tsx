"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Account } from "@accounting/types";

interface Props {
  year:           number;
  orgId:          string;
  token:          string;
  equityAccounts: Account[];
}

export function YearEndForm({ year, orgId, token, equityAccounts }: Props) {
  const router                        = useRouter();
  const [selectedAccount, setAccount] = useState("");
  const [confirmed, setConfirmed]     = useState(false);
  const [isPending, startTransition]  = useTransition();

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedAccount) { toast.error("Selecciona la cuenta de resultados acumulados."); return; }
    if (!confirmed)        { toast.error("Debes confirmar antes de continuar."); return; }

    startTransition(async () => {
      try {
        await apiClient.yearEnd.close(orgId, { year, retainedEarningsAccountId: selectedAccount }, token);
        toast.success(`Año ${year} cerrado correctamente.`);
        router.refresh();
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : "Error al cerrar el año.";
        toast.error(msg);
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5 rounded-xl border border-amber-200 bg-amber-50 p-6 dark:border-amber-800 dark:bg-amber-950/20">
      <div className="flex items-start gap-3">
        <svg className="mt-0.5 h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
        </svg>
        <div>
          <p className="font-semibold text-amber-900 dark:text-amber-200">
            Cerrar el ejercicio {year}
          </p>
          <p className="mt-0.5 text-sm text-amber-700 dark:text-amber-300">
            Esta acción es <strong>irreversible</strong>. Se creará un asiento de cierre con fecha 31/12/{year}.
          </p>
        </div>
      </div>

      <div>
        <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
          Cuenta de Resultados Acumulados <span className="text-red-500">*</span>
        </label>
        {equityAccounts.length === 0 ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-950/30 dark:text-red-300">
            No hay cuentas de Capital (Equity) postables. Crea una antes de continuar.
          </p>
        ) : (
          <select
            value={selectedAccount}
            onChange={(e) => setAccount(e.target.value)}
            className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
          >
            <option value="">Selecciona una cuenta…</option>
            {equityAccounts.map((a) => (
              <option key={a.id} value={a.id}>
                {a.code} — {a.name}
              </option>
            ))}
          </select>
        )}
      </div>

      <label className="flex cursor-pointer items-start gap-3">
        <input
          type="checkbox"
          checked={confirmed}
          onChange={(e) => setConfirmed(e.target.checked)}
          className="mt-0.5 h-4 w-4 rounded border-slate-300 text-indigo-600"
        />
        <span className="text-sm text-slate-700 dark:text-slate-300">
          Entiendo que esta acción cerrará el año <strong>{year}</strong>, zerará todas las cuentas
          de ingreso y gasto, y que no podrá deshacerse.
        </span>
      </label>

      <button
        type="submit"
        disabled={isPending || !confirmed || !selectedAccount || equityAccounts.length === 0}
        className="rounded-lg bg-amber-600 px-6 py-2 text-sm font-semibold text-white transition-colors hover:bg-amber-700 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {isPending ? "Cerrando ejercicio…" : `Cerrar ejercicio ${year}`}
      </button>
    </form>
  );
}

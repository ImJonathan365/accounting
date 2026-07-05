"use client";

import { useState, useTransition } from "react";
import { toggleAccountActiveAction } from "@/lib/actions";
import { EditAccountModal } from "./EditAccountModal";
import type { Account, AccountType } from "@accounting/types";

const TYPE_LABELS: Record<AccountType, string> = {
  Asset: "Activo",
  Liability: "Pasivo",
  Equity: "Capital",
  Income: "Ingreso",
  Expense: "Gasto",
};

const TYPE_COLORS: Record<AccountType, string> = {
  Asset:     "bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
  Liability: "bg-orange-50 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
  Equity:    "bg-purple-50 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400",
  Income:    "bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400",
  Expense:   "bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400",
};

interface Props {
  accounts: Account[];
}

export function AccountsTable({ accounts }: Props) {
  const [editing, setEditing]       = useState<Account | null>(null);
  const [pending, startTransition]  = useTransition();
  const [toggling, setToggling]     = useState<string | null>(null);
  const [toggleError, setToggleError] = useState<string | null>(null);

  function handleToggle(account: Account) {
    setToggling(account.id);
    setToggleError(null);
    startTransition(async () => {
      try {
        await toggleAccountActiveAction(account.id);
      } catch {
        setToggleError("No se pudo cambiar el estado de la cuenta. Intenta de nuevo.");
      } finally {
        setToggling(null);
      }
    });
  }

  return (
    <>
      {editing && (
        <EditAccountModal account={editing} onClose={() => setEditing(null)} />
      )}

      {toggleError && (
        <p className="mb-3 rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600 dark:bg-red-950/30 dark:text-red-400">
          {toggleError}
        </p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
          <thead>
            <tr className="bg-slate-50 dark:bg-slate-700/50">
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Código</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Nombre</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Tipo</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Postable</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Estado</th>
              <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
            {accounts.map((account: Account) => (
              <tr
                key={account.id}
                className={`transition-colors hover:bg-slate-50 dark:hover:bg-slate-700/30 ${!account.isActive ? "opacity-60" : ""}`}
              >
                <td className="px-4 py-3 font-mono text-sm font-medium text-slate-900 dark:text-slate-100">{account.code}</td>
                <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">{account.name}</td>
                <td className="px-4 py-3">
                  <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${TYPE_COLORS[account.type]}`}>
                    {TYPE_LABELS[account.type]}
                  </span>
                </td>
                <td className="px-4 py-3 hidden sm:table-cell">
                  {account.isPostable ? (
                    <span className="inline-flex rounded-full bg-green-50 px-2 py-0.5 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400">Sí</span>
                  ) : (
                    <span className="inline-flex rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-500 dark:bg-slate-700 dark:text-slate-400">No</span>
                  )}
                </td>
                <td className="px-4 py-3 hidden sm:table-cell">
                  {account.isActive ? (
                    <span className="inline-flex rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">Activa</span>
                  ) : (
                    <span className="inline-flex rounded-full bg-red-50 px-2 py-0.5 text-xs font-medium text-red-600 dark:bg-red-900/30 dark:text-red-400">Inactiva</span>
                  )}
                </td>
                <td className="px-4 py-3 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => setEditing(account)}
                      title="Editar cuenta"
                      className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-700 dark:hover:text-slate-300 transition-colors"
                    >
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125" />
                      </svg>
                    </button>
                    <button
                      onClick={() => handleToggle(account)}
                      disabled={toggling === account.id || pending}
                      title={account.isActive ? "Desactivar cuenta" : "Activar cuenta"}
                      className={`rounded-md p-1.5 transition-colors disabled:opacity-40 ${
                        account.isActive
                          ? "text-green-600 hover:bg-red-50 hover:text-red-600 dark:text-green-400 dark:hover:bg-red-900/20 dark:hover:text-red-400"
                          : "text-slate-400 hover:bg-green-50 hover:text-green-600 dark:hover:bg-green-900/20 dark:hover:text-green-400"
                      }`}
                    >
                      {toggling === account.id ? (
                        <span className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
                      ) : account.isActive ? (
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                        </svg>
                      ) : (
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                      )}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}

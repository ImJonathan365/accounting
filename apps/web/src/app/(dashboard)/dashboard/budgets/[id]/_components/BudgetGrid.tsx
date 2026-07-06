"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Budget, Account } from "@accounting/types";

interface Props {
  budget:   Budget;
  accounts: Account[];
  orgId:    string;
  token:    string;
  canEdit:  boolean;
  months:   string[];
}

export function BudgetGrid({ budget, accounts, orgId, token, canEdit, months }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [adding, setAdding]           = useState(false);
  const [newAccountId, setNewAccountId] = useState("");

  const budgetMap = new Map<string, Map<number, number>>();
  for (const l of budget.lines) {
    if (!budgetMap.has(l.accountId)) budgetMap.set(l.accountId, new Map());
    budgetMap.get(l.accountId)!.set(l.month, l.amount);
  }

  const accountIds = Array.from(new Set(budget.lines.map(l => l.accountId)));
  const accountMap = new Map(accounts.map(a => [a.id, a]));
  const unusedAccounts = accounts.filter(a => !accountIds.includes(a.id));

  function save(accountId: string, month: number, value: string) {
    const amount = parseFloat(value) || 0;
    startTransition(async () => {
      try {
        await apiClient.budgets.upsertLine(orgId, budget.id, { accountId, month, amount }, token);
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  function addAccount() {
    if (!newAccountId) return;
    startTransition(async () => {
      try {
        await apiClient.budgets.upsertLine(orgId, budget.id, { accountId: newAccountId, month: 1, amount: 0 }, token);
        setAdding(false); setNewAccountId("");
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  const cellClass = "w-20 rounded border border-slate-200 bg-white px-1.5 py-1 text-right text-xs tabular-nums focus:border-indigo-400 focus:outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100 disabled:opacity-50";

  return (
    <div className="overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-700">
      <table className="text-sm">
        <thead className="bg-slate-50 dark:bg-slate-800/60">
          <tr>
            <th className="whitespace-nowrap px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-400 min-w-48">Cuenta</th>
            {months.map(m => (
              <th key={m} className="px-2 py-2 text-center text-xs font-semibold uppercase tracking-wide text-slate-400">{m}</th>
            ))}
            <th className="px-3 py-2 text-right text-xs font-semibold uppercase tracking-wide text-slate-400">Total</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50 bg-white dark:bg-slate-800">
          {accountIds.map(accId => {
            const acc     = accountMap.get(accId);
            const monthMap = budgetMap.get(accId) ?? new Map();
            const total    = Array.from(monthMap.values()).reduce((s, v) => s + v, 0);
            return (
              <tr key={accId}>
                <td className="whitespace-nowrap px-4 py-2 text-xs text-slate-700 dark:text-slate-300">
                  <span className="font-mono text-slate-400 mr-1">{acc?.code}</span>{acc?.name}
                </td>
                {months.map((_, i) => {
                  const month = i + 1;
                  const val   = monthMap.get(month) ?? 0;
                  return (
                    <td key={month} className="px-2 py-1.5">
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        defaultValue={val === 0 ? "" : val}
                        disabled={!canEdit || isPending}
                        onBlur={e => { if (parseFloat(e.target.value) !== val) save(accId, month, e.target.value); }}
                        className={cellClass}
                      />
                    </td>
                  );
                })}
                <td className="px-3 py-2 text-right font-semibold tabular-nums text-slate-900 dark:text-slate-100">
                  {total.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>

      {canEdit && (
        <div className="border-t border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-800">
          {!adding ? (
            <button onClick={() => setAdding(true)} className="text-sm text-indigo-600 hover:underline dark:text-indigo-400">
              + Agregar cuenta
            </button>
          ) : (
            <div className="flex items-center gap-2">
              <select value={newAccountId} onChange={e => setNewAccountId(e.target.value)}
                className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100">
                <option value="">Selecciona cuenta…</option>
                {unusedAccounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
              </select>
              <button onClick={addAccount} disabled={!newAccountId || isPending} className="rounded-lg bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50">
                Agregar
              </button>
              <button onClick={() => { setAdding(false); setNewAccountId(""); }} className="text-sm text-slate-500 hover:text-slate-700">
                Cancelar
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

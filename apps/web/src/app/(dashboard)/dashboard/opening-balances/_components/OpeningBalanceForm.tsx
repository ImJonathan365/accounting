"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Account } from "@accounting/types";

interface LineState { accountId: string; accountCode: string; accountName: string; accountType: string; debit: string; credit: string; }

interface Props { orgId: string; token: string; accounts: Account[]; }

function today() { return new Date().toISOString().slice(0, 10); }

export function OpeningBalanceForm({ orgId, token, accounts }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [date, setDate]         = useState(today());
  const [description, setDesc]  = useState("Saldos iniciales");
  const [error, setError]       = useState<string | null>(null);

  const postable = accounts.filter(a => a.isPostable);

  const [lines, setLines] = useState<LineState[]>(
    postable.map(a => ({
      accountId:   a.id,
      accountCode: a.code,
      accountName: a.name,
      accountType: a.type,
      debit:  "",
      credit: "",
    }))
  );

  const totalDebit  = lines.reduce((s, l) => s + (parseFloat(l.debit)  || 0), 0);
  const totalCredit = lines.reduce((s, l) => s + (parseFloat(l.credit) || 0), 0);
  const diff        = totalDebit - totalCredit;
  const isBalanced  = Math.abs(diff) < 0.005;

  function updateLine(accountId: string, field: "debit" | "credit", value: string) {
    setLines(ls => ls.map(l => l.accountId === accountId ? { ...l, [field]: value } : l));
  }

  const inputClass = "w-full rounded border border-slate-200 bg-transparent px-2 py-1 text-right text-sm tabular-nums focus:border-indigo-400 focus:outline-none dark:border-slate-600";

  const typeOrder: Record<string, number> = { Asset: 1, Liability: 2, Equity: 3, Income: 4, Expense: 5 };
  const grouped = Object.entries(
    lines.reduce((acc, l) => {
      const key = l.accountType;
      if (!acc[key]) acc[key] = [];
      acc[key].push(l);
      return acc;
    }, {} as Record<string, LineState[]>)
  ).sort((a, b) => (typeOrder[a[0]] ?? 9) - (typeOrder[b[0]] ?? 9));

  const typeLabel: Record<string, string> = {
    Asset: "Activos", Liability: "Pasivos", Equity: "Capital", Income: "Ingresos", Expense: "Gastos",
  };

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!isBalanced) {
      setError(`El asiento no balancea: diferencia de ${Math.abs(diff).toFixed(2)}`);
      return;
    }
    const nonZero = lines.filter(l => parseFloat(l.debit) > 0 || parseFloat(l.credit) > 0);
    if (nonZero.length === 0) { setError("Ingresa al menos un valor."); return; }

    startTransition(async () => {
      try {
        await apiClient.openingBalances.set(orgId, {
          date,
          description: description.trim() || undefined,
          lines: nonZero.map(l => ({
            accountId: l.accountId,
            debit:     parseFloat(l.debit)  || 0,
            credit:    parseFloat(l.credit) || 0,
          })),
        }, token);
        toast.success("Saldos iniciales registrados. Puedes revisarlos en el Diario.");
        router.push("/dashboard/journal");
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error al guardar.");
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && (
        <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">{error}</div>
      )}

      <div className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800 space-y-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Encabezado</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Fecha</label>
            <input type="date" value={date} onChange={e => setDate(e.target.value)}
              className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100" />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Descripción</label>
            <input value={description} onChange={e => setDesc(e.target.value)}
              className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100" />
          </div>
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
        <div className="border-b border-slate-100 px-6 py-3 dark:border-slate-700">
          <div className="grid grid-cols-12 gap-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
            <div className="col-span-1">Código</div>
            <div className="col-span-7">Cuenta</div>
            <div className="col-span-2 text-right">Débito</div>
            <div className="col-span-2 text-right">Crédito</div>
          </div>
        </div>

        {grouped.map(([type, groupLines]) => (
          <div key={type}>
            <div className="bg-slate-50 px-6 py-1.5 dark:bg-slate-800/60">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">{typeLabel[type] ?? type}</p>
            </div>
            {groupLines.map(l => (
              <div key={l.accountId} className="grid grid-cols-12 gap-2 items-center border-t border-slate-50 px-6 py-2 dark:border-slate-700/30 hover:bg-slate-50/50 dark:hover:bg-slate-700/20">
                <div className="col-span-1 font-mono text-xs text-slate-400">{l.accountCode}</div>
                <div className="col-span-7 text-sm text-slate-700 dark:text-slate-300">{l.accountName}</div>
                <div className="col-span-2">
                  <input
                    type="number" min="0" step="0.01" placeholder="0.00"
                    value={l.debit}
                    onChange={e => updateLine(l.accountId, "debit", e.target.value)}
                    className={inputClass}
                  />
                </div>
                <div className="col-span-2">
                  <input
                    type="number" min="0" step="0.01" placeholder="0.00"
                    value={l.credit}
                    onChange={e => updateLine(l.accountId, "credit", e.target.value)}
                    className={inputClass}
                  />
                </div>
              </div>
            ))}
          </div>
        ))}

        {/* Totals row */}
        <div className="grid grid-cols-12 gap-2 border-t-2 border-slate-200 px-6 py-3 dark:border-slate-600">
          <div className="col-span-8 text-right text-sm font-bold text-slate-700 dark:text-slate-300">Totales</div>
          <div className={`col-span-2 text-right text-sm font-bold tabular-nums ${!isBalanced && totalDebit > 0 ? "text-red-600" : "text-slate-900 dark:text-slate-100"}`}>
            {totalDebit.toFixed(2)}
          </div>
          <div className={`col-span-2 text-right text-sm font-bold tabular-nums ${!isBalanced && totalCredit > 0 ? "text-red-600" : "text-slate-900 dark:text-slate-100"}`}>
            {totalCredit.toFixed(2)}
          </div>
        </div>

        {!isBalanced && (totalDebit > 0 || totalCredit > 0) && (
          <div className="border-t border-red-100 bg-red-50 px-6 py-2 dark:border-red-900/30 dark:bg-red-950/20">
            <p className="text-xs text-red-600 dark:text-red-400">
              Diferencia: {Math.abs(diff).toFixed(2)} — el asiento debe balancear antes de guardar.
            </p>
          </div>
        )}
        {isBalanced && (totalDebit > 0 || totalCredit > 0) && (
          <div className="border-t border-emerald-100 bg-emerald-50 px-6 py-2 dark:border-emerald-900/30 dark:bg-emerald-950/20">
            <p className="text-xs text-emerald-600 dark:text-emerald-400">✓ Balanceado</p>
          </div>
        )}
      </div>

      <div className="flex gap-3">
        <button type="submit" disabled={isPending || !isBalanced} className="rounded-lg bg-indigo-600 px-6 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
          {isPending ? "Guardando…" : "Registrar saldos iniciales"}
        </button>
        <button type="button" onClick={() => router.back()} className="rounded-lg border border-slate-300 px-6 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300">
          Cancelar
        </button>
      </div>
    </form>
  );
}

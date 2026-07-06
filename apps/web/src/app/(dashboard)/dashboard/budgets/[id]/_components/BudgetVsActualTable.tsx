"use client";

import type { BudgetVsActual } from "@accounting/types";

interface Props { data: BudgetVsActual; months: string[]; }

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 0, maximumFractionDigits: 0 });
}

export function BudgetVsActualTable({ data, months }: Props) {
  if (data.lines.length === 0) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white py-10 text-center dark:border-slate-700 dark:bg-slate-800">
        <p className="text-sm text-slate-400">Agrega cuentas al presupuesto para ver la comparación.</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-slate-200 dark:border-slate-700">
      <table className="min-w-full text-xs">
        <thead className="bg-slate-50 dark:bg-slate-800/60">
          <tr>
            <th className="whitespace-nowrap px-4 py-2 text-left font-semibold text-slate-400 min-w-40">Cuenta</th>
            {months.map(m => (
              <th key={m} colSpan={2} className="px-2 py-2 text-center font-semibold text-slate-400 border-l border-slate-200 dark:border-slate-700">
                {m}
              </th>
            ))}
            <th colSpan={3} className="px-3 py-2 text-center font-semibold text-slate-400 border-l border-slate-200 dark:border-slate-700">Total</th>
          </tr>
          <tr className="border-b border-slate-200 dark:border-slate-700">
            <th className="px-4 py-1" />
            {months.map(m => (
              <>
                <th key={`${m}-b`} className="px-1 py-1 text-right text-slate-300 border-l border-slate-200 dark:border-slate-700 font-normal">Ppto</th>
                <th key={`${m}-a`} className="px-1 py-1 text-right text-slate-300 font-normal">Real</th>
              </>
            ))}
            <th className="px-2 py-1 text-right text-slate-300 border-l border-slate-200 dark:border-slate-700 font-normal">Ppto</th>
            <th className="px-2 py-1 text-right text-slate-300 font-normal">Real</th>
            <th className="px-2 py-1 text-right text-slate-300 font-normal">Var.</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30 bg-white dark:bg-slate-800">
          {data.lines.map(l => (
            <tr key={l.accountId} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
              <td className="whitespace-nowrap px-4 py-2 text-slate-700 dark:text-slate-300">
                <span className="font-mono text-slate-400 mr-1">{l.accountCode}</span>{l.accountName}
              </td>
              {l.budget.map((bv, i) => {
                const av  = l.actual[i];
                const ok  = av >= bv;
                return (
                  <>
                    <td key={`b${i}`} className="px-1 py-2 text-right tabular-nums text-slate-500 border-l border-slate-100 dark:border-slate-700/50">{bv ? fmt(bv) : "—"}</td>
                    <td key={`a${i}`} className={`px-1 py-2 text-right tabular-nums ${av ? (ok ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400") : "text-slate-300"}`}>
                      {av ? fmt(av) : "—"}
                    </td>
                  </>
                );
              })}
              <td className="px-2 py-2 text-right tabular-nums font-medium text-slate-700 dark:text-slate-300 border-l border-slate-200 dark:border-slate-700">
                {fmt(l.totalBudget)}
              </td>
              <td className="px-2 py-2 text-right tabular-nums font-medium text-slate-700 dark:text-slate-300">
                {fmt(l.totalActual)}
              </td>
              <td className={`px-2 py-2 text-right tabular-nums font-semibold ${l.variance >= 0 ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                {l.variance >= 0 ? "+" : ""}{fmt(l.variance)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

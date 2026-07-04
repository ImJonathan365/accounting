import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { DateRangeFilter } from "../_components/DateRangeFilter";
import { ExportButton } from "../_components/ExportButton";
import type { TrialBalanceLine } from "@accounting/types";

export const dynamic = "force-dynamic";

const TYPE_LABELS: Record<string, string> = {
  Asset: "Activo", Liability: "Pasivo", Equity: "Capital",
  Income: "Ingreso", Expense: "Gasto",
};

const TYPE_COLORS: Record<string, string> = {
  Asset:     "bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
  Liability: "bg-orange-50 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
  Equity:    "bg-purple-50 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400",
  Income:    "bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400",
  Expense:   "bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400",
};

function fmt(n: number) {
  return n > 0
    ? n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    : "";
}

function defaultFrom() {
  return `${new Date().getFullYear()}-01-01`;
}
function defaultTo() {
  const n = new Date();
  return `${n.getFullYear()}-${String(n.getMonth()+1).padStart(2,"0")}-${String(n.getDate()).padStart(2,"0")}`;
}

export default async function TrialBalancePage({
  searchParams,
}: {
  searchParams: Promise<{ from?: string; to?: string }>;
}) {
  const sp = await searchParams;
  const from = sp.from ?? defaultFrom();
  const to   = sp.to   ?? defaultTo();

  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const report = await apiClient.reports.trialBalance(orgId, from, to, token);

  const grouped = Object.groupBy(report.lines, (l: TrialBalanceLine) => l.type);
  const typeOrder = ["Asset", "Liability", "Equity", "Income", "Expense"] as const;

  return (
    <>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Balance de comprobación</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Período: {from} — {to}
        </p>
      </div>

      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <DateRangeFilter basePath="/dashboard/reports/trial-balance" from={from} to={to} />
        <ExportButton
          token={token}
          baseName={`balance-comprobacion_${from}_${to}`}
          buildPath={(fmt) =>
            `/api/organizations/${orgId}/reports/trial-balance/export?from=${from}&to=${to}&format=${fmt}`
          }
        />
      </div>

      {report.lines.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white py-16 text-center dark:border-slate-600 dark:bg-slate-800">
          <p className="text-sm font-medium text-slate-600 dark:text-slate-300">Sin movimientos en este período</p>
          <p className="mt-1 text-sm text-slate-400 dark:text-slate-500">Registra asientos en el diario contable primero.</p>
        </div>
      ) : (
        <div className="space-y-2">
          {/* Balance verification badge */}
          <div className="flex justify-end">
            {report.isBalanced ? (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-green-50 px-3 py-1 text-sm font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400">
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd"/>
                </svg>
                Libros balanceados
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-red-50 px-3 py-1 text-sm font-medium text-red-700 dark:bg-red-900/30 dark:text-red-400">
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd"/>
                </svg>
                Libros desbalanceados — revisa los asientos
              </span>
            )}
          </div>

          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
              <thead>
                <tr className="bg-slate-50 dark:bg-slate-700/50">
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Código</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Cuenta</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden md:table-cell">Tipo</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Debe</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Haber</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Saldo deudor</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Saldo acreedor</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
                {typeOrder.map((type) => {
                  const lines = grouped[type] ?? [];
                  if (lines.length === 0) return null;
                  const subtotalDebit   = lines.reduce((s, l) => s + l.totalDebit,   0);
                  const subtotalCredit  = lines.reduce((s, l) => s + l.totalCredit,  0);
                  const subtotalDebitB  = lines.reduce((s, l) => s + l.debitBalance, 0);
                  const subtotalCreditB = lines.reduce((s, l) => s + l.creditBalance,0);
                  return (
                    <>
                      {/* Section header */}
                      <tr key={`hdr-${type}`} className="bg-slate-50/70 dark:bg-slate-700/30">
                        <td colSpan={7} className="px-4 py-1.5">
                          <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${TYPE_COLORS[type]}`}>
                            {TYPE_LABELS[type]}s
                          </span>
                        </td>
                      </tr>
                      {lines.map((l) => (
                        <tr key={l.accountId} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                          <td className="px-4 py-2.5 font-mono text-sm font-medium text-slate-600 dark:text-slate-400">{l.code}</td>
                          <td className="px-4 py-2.5 text-sm text-slate-800 dark:text-slate-200">{l.name}</td>
                          <td className="px-4 py-2.5 hidden md:table-cell">
                            <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${TYPE_COLORS[l.type]}`}>
                              {TYPE_LABELS[l.type]}
                            </span>
                          </td>
                          <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-700 dark:text-slate-300">{fmt(l.totalDebit)}</td>
                          <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-700 dark:text-slate-300">{fmt(l.totalCredit)}</td>
                          <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-700 dark:text-slate-300">{fmt(l.debitBalance)}</td>
                          <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-700 dark:text-slate-300">{fmt(l.creditBalance)}</td>
                        </tr>
                      ))}
                      {/* Subtotal row */}
                      <tr key={`sub-${type}`} className="bg-slate-50 dark:bg-slate-700/20">
                        <td colSpan={3} className="px-4 py-2 text-xs font-semibold text-slate-500 dark:text-slate-400">
                          Subtotal {TYPE_LABELS[type]}s
                        </td>
                        <td className="px-4 py-2 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">{fmt(subtotalDebit)}</td>
                        <td className="px-4 py-2 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">{fmt(subtotalCredit)}</td>
                        <td className="px-4 py-2 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">{fmt(subtotalDebitB)}</td>
                        <td className="px-4 py-2 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">{fmt(subtotalCreditB)}</td>
                      </tr>
                    </>
                  );
                })}
              </tbody>
              {/* Grand totals */}
              <tfoot>
                <tr className="border-t-2 border-slate-300 bg-slate-100 dark:border-slate-600 dark:bg-slate-700/50">
                  <td colSpan={3} className="px-4 py-3 text-sm font-bold text-slate-700 dark:text-slate-200">TOTALES</td>
                  <td className="px-4 py-3 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
                    {report.totalDebit.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
                  </td>
                  <td className="px-4 py-3 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
                    {report.totalCredit.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
                  </td>
                  <td className="px-4 py-3 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
                    {report.totalDebitBalance.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
                  </td>
                  <td className="px-4 py-3 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
                    {report.totalCreditBalance.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </>
  );
}

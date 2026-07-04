import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { DateRangeFilter } from "../_components/DateRangeFilter";
import { ExportButton } from "../_components/ExportButton";
import type { IncomeStatementLine } from "@accounting/types";

export const dynamic = "force-dynamic";

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function pct(amount: number, total: number) {
  if (total === 0) return "";
  return `${((amount / total) * 100).toFixed(1)}%`;
}

function defaultFrom() { return `${new Date().getFullYear()}-01-01`; }
function defaultTo() {
  const n = new Date();
  return `${n.getFullYear()}-${String(n.getMonth()+1).padStart(2,"0")}-${String(n.getDate()).padStart(2,"0")}`;
}

function groupByParent(lines: IncomeStatementLine[]) {
  const groups = new Map<string, { parentName: string; lines: IncomeStatementLine[] }>();
  for (const line of lines) {
    const key  = line.parentCode ?? "__root__";
    const name = line.parentName ?? "";
    if (!groups.has(key)) groups.set(key, { parentName: name, lines: [] });
    groups.get(key)!.lines.push(line);
  }
  return groups;
}

export default async function IncomeStatementPage({
  searchParams,
}: {
  searchParams: Promise<{ from?: string; to?: string }>;
}) {
  const sp   = await searchParams;
  const from = sp.from ?? defaultFrom();
  const to   = sp.to   ?? defaultTo();

  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const report = await apiClient.reports.incomeStatement(orgId, from, to, token);
  const totalIncome = report.income.total;

  const incomeGroups  = groupByParent(report.income.lines);
  const expenseGroups = groupByParent(report.expenses.lines);
  const hasData = report.income.lines.length > 0 || report.expenses.lines.length > 0;

  return (
    <>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Estado de resultados</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Período: {from} — {to}</p>
      </div>

      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <DateRangeFilter basePath="/dashboard/reports/income-statement" from={from} to={to} />
        <ExportButton
          token={token}
          baseName={`estado-resultados_${from}_${to}`}
          buildPath={(fmt) =>
            `/api/organizations/${orgId}/reports/income-statement/export?from=${from}&to=${to}&format=${fmt}`
          }
        />
      </div>

      {!hasData ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white py-16 text-center dark:border-slate-600 dark:bg-slate-800">
          <p className="text-sm font-medium text-slate-600 dark:text-slate-300">Sin movimientos en este período</p>
          <p className="mt-1 text-sm text-slate-400 dark:text-slate-500">Registra asientos en el diario contable primero.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Net income summary card */}
          <div className={`rounded-xl border p-5 ${
            report.isProfit
              ? "border-green-200 bg-green-50 dark:border-green-800 dark:bg-green-900/20"
              : "border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-900/20"
          }`}>
            <p className="text-sm font-medium text-slate-600 dark:text-slate-400">
              {report.isProfit ? "Utilidad neta del período" : "Pérdida neta del período"}
            </p>
            <p className={`mt-1 text-3xl font-bold ${
              report.isProfit
                ? "text-green-700 dark:text-green-400"
                : "text-red-700 dark:text-red-400"
            }`}>
              {report.isProfit ? "" : "−"}{fmt(Math.abs(report.netIncome))}
            </p>
            <div className="mt-2 flex gap-6 text-xs text-slate-500 dark:text-slate-400">
              <span>Ingresos: <strong className="text-slate-700 dark:text-slate-300">{fmt(report.income.total)}</strong></span>
              <span>Gastos: <strong className="text-slate-700 dark:text-slate-300">{fmt(report.expenses.total)}</strong></span>
            </div>
          </div>

          {/* Income section */}
          <Section
            title="Ingresos"
            color="green"
            groups={incomeGroups}
            sectionTotal={report.income.total}
            referenceTotal={totalIncome}
          />

          {/* Expenses section */}
          <Section
            title="Gastos"
            color="red"
            groups={expenseGroups}
            sectionTotal={report.expenses.total}
            referenceTotal={totalIncome}
          />
        </div>
      )}
    </>
  );
}

function Section({
  title, color, groups, sectionTotal, referenceTotal,
}: {
  title: string;
  color: "green" | "red";
  groups: Map<string, { parentName: string; lines: IncomeStatementLine[] }>;
  sectionTotal: number;
  referenceTotal: number;
}) {
  const headerColors = {
    green: "bg-green-50 text-green-800 dark:bg-green-900/20 dark:text-green-300",
    red:   "bg-red-50 text-red-800 dark:bg-red-900/20 dark:text-red-300",
  };
  const totalColors = {
    green: "text-green-700 dark:text-green-400",
    red:   "text-red-700 dark:text-red-400",
  };

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
      {/* Section header */}
      <div className={`px-4 py-3 ${headerColors[color]}`}>
        <h3 className="font-semibold">{title}</h3>
      </div>

      <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
        <thead>
          <tr className="bg-slate-50 dark:bg-slate-700/30">
            <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Código</th>
            <th className="px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Cuenta</th>
            <th className="px-4 py-2 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Monto</th>
            <th className="px-4 py-2 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">% Ingresos</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
          {Array.from(groups.entries()).map(([key, group]) => (
            <>
              {group.parentName && (
                <tr key={`grp-${key}`} className="bg-slate-50/50 dark:bg-slate-700/20">
                  <td colSpan={4} className="px-4 py-1.5 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">
                    {group.parentName}
                  </td>
                </tr>
              )}
              {group.lines.map((l) => (
                <tr key={l.accountId} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                  <td className="px-4 py-2.5 font-mono text-sm text-slate-500 dark:text-slate-400 pl-6">{l.code}</td>
                  <td className="px-4 py-2.5 text-sm text-slate-700 dark:text-slate-300">{l.name}</td>
                  <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-900 dark:text-slate-100">{fmt(l.amount)}</td>
                  <td className="px-4 py-2.5 text-right text-xs text-slate-400 dark:text-slate-500 hidden sm:table-cell">
                    {pct(l.amount, referenceTotal)}
                  </td>
                </tr>
              ))}
              {/* Group subtotal if has parent */}
              {group.parentName && (
                <tr key={`gsub-${key}`} className="bg-slate-50 dark:bg-slate-700/20">
                  <td colSpan={2} className="px-4 py-1.5 pl-6 text-xs font-semibold text-slate-500 dark:text-slate-400">
                    Total {group.parentName}
                  </td>
                  <td className="px-4 py-1.5 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">
                    {fmt(group.lines.reduce((s, l) => s + l.amount, 0))}
                  </td>
                  <td className="hidden sm:table-cell" />
                </tr>
              )}
            </>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t-2 border-slate-200 dark:border-slate-600">
            <td colSpan={2} className={`px-4 py-3 text-sm font-bold ${totalColors[color]}`}>
              Total {title}
            </td>
            <td className={`px-4 py-3 text-right font-mono text-sm font-bold ${totalColors[color]}`}>
              {fmt(sectionTotal)}
            </td>
            <td className="px-4 py-3 text-right text-xs font-semibold text-slate-400 hidden sm:table-cell">
              {pct(sectionTotal, referenceTotal)}
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

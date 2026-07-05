import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { DateFilter } from "../_components/DateFilter";
import { ExportButton } from "../_components/ExportButton";
import type { BalanceSheetGroup, BalanceSheetSection, BalanceSheetLine } from "@accounting/types";

export const dynamic = "force-dynamic";

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function fmtSigned(n: number) {
  return (n < 0 ? "(" : "") + fmt(Math.abs(n)) + (n < 0 ? ")" : "");
}

function defaultAsOf() {
  const n = new Date();
  return `${n.getFullYear()}-${String(n.getMonth()+1).padStart(2,"0")}-${String(n.getDate()).padStart(2,"0")}`;
}

function formatDate(iso: string) {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}

export default async function BalanceSheetPage({
  searchParams,
}: {
  searchParams: Promise<{ asOf?: string }>;
}) {
  const sp  = await searchParams;
  const asOf = sp.asOf ?? defaultAsOf();

  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const report = await apiClient.reports.balanceSheet(orgId, asOf, token);
  const hasData = report.assets.total !== 0 || report.liabilities.total !== 0 || report.totalEquity !== 0;

  return (
    <>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Balance general</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Al {formatDate(asOf)}</p>
      </div>

      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <DateFilter basePath="/dashboard/reports/balance-sheet" asOf={asOf} />
        <ExportButton
          token={token}
          baseName={`balance-general_${asOf}`}
          pdfPath={`/api/organizations/${orgId}/reports/balance-sheet/export?asOf=${asOf}&format=pdf`}
          csvPath={`/api/organizations/${orgId}/reports/balance-sheet/export?asOf=${asOf}&format=csv`}
        />
      </div>

      {!hasData ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white py-16 text-center dark:border-slate-600 dark:bg-slate-800">
          <p className="text-sm font-medium text-slate-600 dark:text-slate-300">Sin movimientos hasta esta fecha</p>
          <p className="mt-1 text-sm text-slate-400 dark:text-slate-500">Registra asientos en el diario contable primero.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Balance verification */}
          <div className="flex justify-end">
            {report.isBalanced ? (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-green-50 px-3 py-1 text-sm font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400">
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd"/>
                </svg>
                Ecuación balanceada — Activos = Pasivos + Capital
              </span>
            ) : (
              <span className="inline-flex items-center gap-1.5 rounded-full bg-red-50 px-3 py-1 text-sm font-medium text-red-700 dark:bg-red-900/30 dark:text-red-400">
                <svg className="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd"/>
                </svg>
                Ecuación desbalanceada — revisa los asientos
              </span>
            )}
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* LEFT: Assets */}
            <GroupCard group={report.assets} color="blue" />

            {/* RIGHT: Liabilities + Equity stacked */}
            <div className="space-y-4">
              <GroupCard group={report.liabilities} color="orange" />

              {/* Equity with net income */}
              <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <div className="bg-purple-50 px-4 py-3 dark:bg-purple-900/20">
                  <h3 className="font-semibold text-purple-800 dark:text-purple-300">Capital</h3>
                </div>
                <table className="min-w-full">
                  <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
                    {report.equity.sections.map((section: BalanceSheetSection) => (
                      <>
                        {section.sectionName && (
                          <tr key={`hdr-${section.sectionCode}`} className="bg-slate-50/50 dark:bg-slate-700/20">
                            <td colSpan={2} className="px-4 py-1.5 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                              {section.sectionName}
                            </td>
                          </tr>
                        )}
                        {section.lines.map((line: BalanceSheetLine) => (
                          <tr key={line.accountId} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                            <td className="px-4 py-2.5 pl-6">
                              <span className="font-mono text-xs text-slate-500 dark:text-slate-400 mr-2">{line.code}</span>
                              <span className="text-sm text-slate-700 dark:text-slate-300">{line.name}</span>
                            </td>
                            <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-900 dark:text-slate-100">
                              {line.balance !== 0 ? fmtSigned(line.balance) : "—"}
                            </td>
                          </tr>
                        ))}
                        {section.lines.length > 1 && (
                          <tr key={`sub-${section.sectionCode}`} className="bg-slate-50 dark:bg-slate-700/20">
                            <td className="px-4 py-1.5 pl-6 text-xs font-semibold text-slate-500 dark:text-slate-400">
                              Subtotal {section.sectionName}
                            </td>
                            <td className="px-4 py-1.5 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">
                              {fmt(section.subtotal)}
                            </td>
                          </tr>
                        )}
                      </>
                    ))}
                    {/* Net income line */}
                    <tr className={`${report.netIncome >= 0 ? "bg-green-50/50 dark:bg-green-900/10" : "bg-red-50/50 dark:bg-red-900/10"}`}>
                      <td className="px-4 py-2.5 pl-6 text-sm font-medium text-slate-700 dark:text-slate-300">
                        {report.netIncome >= 0 ? "Utilidad acumulada" : "Pérdida acumulada"}
                      </td>
                      <td className={`px-4 py-2.5 text-right font-mono text-sm font-semibold ${
                        report.netIncome >= 0
                          ? "text-green-700 dark:text-green-400"
                          : "text-red-700 dark:text-red-400"
                      }`}>
                        {fmtSigned(report.netIncome)}
                      </td>
                    </tr>
                  </tbody>
                  <tfoot>
                    <tr className="border-t-2 border-purple-200 dark:border-purple-800">
                      <td className="px-4 py-3 text-sm font-bold text-purple-700 dark:text-purple-400">Total Capital</td>
                      <td className="px-4 py-3 text-right font-mono text-sm font-bold text-purple-700 dark:text-purple-400">
                        {fmt(report.totalEquity)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              {/* Totals summary */}
              <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-800/60">
                <div className="flex justify-between text-sm">
                  <span className="font-medium text-slate-600 dark:text-slate-400">Total Pasivos + Capital</span>
                  <span className="font-mono font-bold text-slate-900 dark:text-slate-100">
                    {fmt(report.totalLiabilitiesAndEquity)}
                  </span>
                </div>
                <div className="mt-2 flex justify-between text-sm">
                  <span className="font-medium text-slate-600 dark:text-slate-400">Total Activos</span>
                  <span className="font-mono font-bold text-slate-900 dark:text-slate-100">
                    {fmt(report.assets.total)}
                  </span>
                </div>
                <div className={`mt-3 border-t pt-3 flex justify-between text-sm ${
                  report.isBalanced
                    ? "border-green-200 dark:border-green-800"
                    : "border-red-200 dark:border-red-800"
                }`}>
                  <span className={`font-semibold ${report.isBalanced ? "text-green-700 dark:text-green-400" : "text-red-700 dark:text-red-400"}`}>
                    Diferencia
                  </span>
                  <span className={`font-mono font-bold ${report.isBalanced ? "text-green-700 dark:text-green-400" : "text-red-700 dark:text-red-400"}`}>
                    {fmt(Math.abs(report.assets.total - report.totalLiabilitiesAndEquity))}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function GroupCard({ group, color }: { group: BalanceSheetGroup; color: "blue" | "orange" }) {
  const headerColors = {
    blue:   "bg-blue-50 text-blue-800 dark:bg-blue-900/20 dark:text-blue-300",
    orange: "bg-orange-50 text-orange-800 dark:bg-orange-900/20 dark:text-orange-300",
  };
  const totalColors = {
    blue:   "text-blue-700 dark:text-blue-400",
    orange: "text-orange-700 dark:text-orange-400",
  };
  const borderColors = {
    blue:   "border-blue-200 dark:border-blue-800",
    orange: "border-orange-200 dark:border-orange-800",
  };

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <div className={`px-4 py-3 ${headerColors[color]}`}>
        <h3 className="font-semibold">{group.title}</h3>
      </div>
      <table className="min-w-full">
        <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
          {group.sections.map((section: BalanceSheetSection) => (
            <>
              {section.sectionName && (
                <tr key={`hdr-${section.sectionCode}`} className="bg-slate-50/50 dark:bg-slate-700/20">
                  <td colSpan={2} className="px-4 py-1.5 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                    {section.sectionName}
                  </td>
                </tr>
              )}
              {section.lines.map((line: BalanceSheetLine) => (
                <tr key={line.accountId} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                  <td className="px-4 py-2.5 pl-6">
                    <span className="font-mono text-xs text-slate-500 dark:text-slate-400 mr-2">{line.code}</span>
                    <span className="text-sm text-slate-700 dark:text-slate-300">{line.name}</span>
                  </td>
                  <td className="px-4 py-2.5 text-right font-mono text-sm text-slate-900 dark:text-slate-100">
                    {line.balance !== 0
                      ? line.balance < 0
                        ? <span className="text-red-600 dark:text-red-400">({fmt(Math.abs(line.balance))})</span>
                        : fmt(line.balance)
                      : "—"}
                  </td>
                </tr>
              ))}
              {section.lines.length > 1 && (
                <tr key={`sub-${section.sectionCode}`} className="bg-slate-50 dark:bg-slate-700/20">
                  <td className="px-4 py-1.5 pl-6 text-xs font-semibold text-slate-500 dark:text-slate-400">
                    Subtotal {section.sectionName}
                  </td>
                  <td className="px-4 py-1.5 text-right font-mono text-xs font-semibold text-slate-700 dark:text-slate-300">
                    {fmt(section.subtotal)}
                  </td>
                </tr>
              )}
            </>
          ))}
        </tbody>
        <tfoot>
          <tr className={`border-t-2 ${borderColors[color]}`}>
            <td className={`px-4 py-3 text-sm font-bold ${totalColors[color]}`}>Total {group.title}</td>
            <td className={`px-4 py-3 text-right font-mono text-sm font-bold ${totalColors[color]}`}>
              {fmt(group.total)}
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

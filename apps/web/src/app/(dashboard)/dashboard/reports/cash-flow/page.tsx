import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { DateRangeFilter } from "../_components/DateRangeFilter";
import { ExportButton } from "../_components/ExportButton";
import type { CashFlow, CashFlowActivitySection, OrgSettings } from "@accounting/types";

function today() {
  return new Date().toISOString().slice(0, 10);
}
function yearStart() {
  return `${new Date().getFullYear()}-01-01`;
}

function fmt(v: number, sym: string) {
  return `${sym} ${v.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function SectionTable({
  section, sym, includeNetIncome,
}: {
  section: CashFlowActivitySection;
  sym: string;
  includeNetIncome: boolean;
}) {
  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-700">
      <div className="bg-slate-50 px-6 py-3 dark:bg-slate-800/60">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">{section.title}</h3>
      </div>
      <table className="w-full text-sm">
        <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
          {includeNetIncome && (
            <tr className="bg-white dark:bg-slate-800">
              <td className="px-6 py-3 text-slate-700 dark:text-slate-300">Utilidad (Pérdida) neta del período</td>
              <td className={`px-6 py-3 text-right font-medium tabular-nums ${section.netIncome >= 0 ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                {fmt(section.netIncome, sym)}
              </td>
            </tr>
          )}
          {section.lines.map((l, i) => (
            <tr key={i} className="bg-white dark:bg-slate-800">
              <td className="px-6 py-3 pl-10 text-slate-600 dark:text-slate-400">
                <span className="mr-2 font-mono text-xs text-slate-400">{l.accountCode}</span>
                {l.accountName}
              </td>
              <td className={`px-6 py-3 text-right tabular-nums ${l.amount >= 0 ? "text-slate-900 dark:text-slate-100" : "text-red-600 dark:text-red-400"}`}>
                {l.amount >= 0 ? fmt(l.amount, sym) : `(${fmt(Math.abs(l.amount), sym)})`}
              </td>
            </tr>
          ))}
          {section.lines.length === 0 && !includeNetIncome && (
            <tr className="bg-white dark:bg-slate-800">
              <td colSpan={2} className="px-6 py-4 text-center text-xs text-slate-400">Sin movimientos</td>
            </tr>
          )}
        </tbody>
        <tfoot>
          <tr className="border-t border-slate-200 bg-slate-50 font-semibold dark:border-slate-700 dark:bg-slate-800/60">
            <td className="px-6 py-3 text-slate-900 dark:text-slate-100">Total {section.title}</td>
            <td className={`px-6 py-3 text-right tabular-nums ${section.total >= 0 ? "text-slate-900 dark:text-slate-100" : "text-red-600 dark:text-red-400"}`}>
              {section.total >= 0 ? fmt(section.total, sym) : `(${fmt(Math.abs(section.total), sym)})`}
            </td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

export default async function CashFlowPage({
  searchParams,
}: {
  searchParams: Promise<{ from?: string; to?: string }>;
}) {
  const token = await getServerToken();
  const orgId = await getCurrentOrgId();
  if (!token || !orgId) redirect("/login");

  const sp = await searchParams;
  const from = sp.from ?? yearStart();
  const to   = sp.to   ?? today();

  let data: CashFlow;
  let settings: OrgSettings;
  try {
    [data, settings] = await Promise.all([
      apiClient.reports.cashFlow(orgId, from, to, token),
      apiClient.settings.get(orgId, token),
    ]);
  } catch {
    return <PageError message="No se pudo cargar el estado de flujo de efectivo." />;
  }
  const sym = settings.currencySymbol;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
          Estado de Flujo de Efectivo
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Método indirecto · {from} al {to}
        </p>
      </div>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <DateRangeFilter basePath="/dashboard/reports/cash-flow" from={from} to={to} />
        <ExportButton
          pdfPath={`/api/organizations/${orgId}/reports/cash-flow/export?from=${from}&to=${to}&format=pdf`}
          csvPath={`/api/organizations/${orgId}/reports/cash-flow/export?from=${from}&to=${to}&format=csv`}
          baseName={`flujo-efectivo_${from}_${to}`}
        />
      </div>

      {/* Summary bar */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        {[
          { label: "Saldo inicial", value: data.beginningCash },
          { label: "Cambio neto",   value: data.netChange },
          { label: "Saldo final",   value: data.endingCash },
        ].map(({ label, value }) => (
          <div key={label} className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-800">
            <p className="text-xs text-slate-500 dark:text-slate-400">{label}</p>
            <p className={`mt-1 text-xl font-semibold tabular-nums ${value >= 0 ? "text-slate-900 dark:text-slate-100" : "text-red-600 dark:text-red-400"}`}>
              {fmt(value, sym)}
            </p>
          </div>
        ))}
        <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-800">
          <p className="text-xs text-slate-500 dark:text-slate-400">Cuadra</p>
          <p className={`mt-1 text-xl font-semibold ${Math.abs(data.beginningCash + data.netChange - data.endingCash) < 0.01 ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
            {Math.abs(data.beginningCash + data.netChange - data.endingCash) < 0.01 ? "✓ Sí" : "✗ No"}
          </p>
        </div>
      </div>

      {/* Sections */}
      <SectionTable section={data.operating}  sym={sym} includeNetIncome />
      <SectionTable section={data.investing}  sym={sym} includeNetIncome={false} />
      <SectionTable section={data.financing}  sym={sym} includeNetIncome={false} />

      {/* Footer reconciliation */}
      <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-700">
        <table className="w-full text-sm">
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50 bg-white dark:bg-slate-800">
            <tr>
              <td className="px-6 py-3 text-slate-700 dark:text-slate-300">Saldo inicial de efectivo</td>
              <td className="px-6 py-3 text-right tabular-nums text-slate-900 dark:text-slate-100">{fmt(data.beginningCash, sym)}</td>
            </tr>
            <tr>
              <td className="px-6 py-3 text-slate-700 dark:text-slate-300">Variación neta del período</td>
              <td className={`px-6 py-3 text-right tabular-nums font-medium ${data.netChange >= 0 ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                {data.netChange >= 0 ? fmt(data.netChange, sym) : `(${fmt(Math.abs(data.netChange), sym)})`}
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-slate-300 bg-slate-50 font-bold dark:border-slate-600 dark:bg-slate-800/60">
              <td className="px-6 py-4 text-slate-900 dark:text-slate-100">Saldo final de efectivo</td>
              <td className="px-6 py-4 text-right tabular-nums text-slate-900 dark:text-slate-100">{fmt(data.endingCash, sym)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>
  );
}

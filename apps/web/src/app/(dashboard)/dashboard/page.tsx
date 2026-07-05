import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import type { DashboardSummary, RecentEntry } from "@accounting/types";

export const dynamic = "force-dynamic";

function fmt(n: number, symbol: string) {
  return `${symbol} ${Math.abs(n).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function formatDate(iso: string) {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}

function StatusDot({ status }: { status: string }) {
  if (status === "Voided")
    return <span className="inline-block h-2 w-2 rounded-full bg-red-400" title="Anulado" />;
  if (status === "Posted")
    return <span className="inline-block h-2 w-2 rounded-full bg-green-500" title="Registrado" />;
  return <span className="inline-block h-2 w-2 rounded-full bg-amber-400" title="Borrador" />;
}

interface MetricCardProps {
  label: string;
  value: string;
  sub?: string;
  color: "indigo" | "green" | "red" | "slate";
  icon: React.ReactNode;
}

function MetricCard({ label, value, sub, color, icon }: MetricCardProps) {
  const colors = {
    indigo: "bg-indigo-50 text-indigo-600 dark:bg-indigo-950/40 dark:text-indigo-400",
    green:  "bg-green-50  text-green-600  dark:bg-green-950/40  dark:text-green-400",
    red:    "bg-red-50    text-red-600    dark:bg-red-950/40    dark:text-red-400",
    slate:  "bg-slate-100 text-slate-500  dark:bg-slate-700     dark:text-slate-400",
  };

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">{label}</p>
          <p className="mt-1.5 truncate text-2xl font-bold text-slate-900 dark:text-slate-100">{value}</p>
          {sub && <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">{sub}</p>}
        </div>
        <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${colors[color]}`}>
          {icon}
        </div>
      </div>
    </div>
  );
}

export default async function DashboardPage() {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  let summary: DashboardSummary | null = null;
  try {
    summary = await apiClient.dashboard.getSummary(orgId, token);
  } catch {
    // If no data yet (empty org), summary stays null
  }

  const s = summary?.currencySymbol ?? "Q";
  const hasData = summary !== null;

  return (
    <>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Inicio</h2>
          {hasData && (
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{summary!.periodLabel}</p>
          )}
        </div>
      </div>

      {/* KPI cards */}
      <div className="mb-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          label="Total activos"
          value={hasData ? fmt(summary!.totalAssets, s) : "—"}
          color="indigo"
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v12m-3-2.818.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33" />
            </svg>
          }
        />
        <MetricCard
          label="Pasivos + Capital"
          value={hasData ? fmt(summary!.totalLiabilities + summary!.totalEquity, s) : "—"}
          sub={hasData && summary!.isBalanced ? "Libros balanceados ✓" : hasData ? "⚠ Libros desbalanceados" : undefined}
          color={hasData && summary!.isBalanced ? "slate" : "red"}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M3 6l3 1m0 0-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
            </svg>
          }
        />
        <MetricCard
          label={`Resultado ${new Date().getFullYear()}`}
          value={hasData ? fmt(summary!.netIncome, s) : "—"}
          sub={hasData ? (summary!.isProfit ? "Ganancia" : "Pérdida") : undefined}
          color={!hasData ? "slate" : summary!.isProfit ? "green" : "red"}
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
              <path strokeLinecap="round" strokeLinejoin="round" d={summary?.isProfit === false
                ? "M2.25 6 9 12.75l4.286-4.286a11.948 11.948 0 014.306 6.43l.776 2.898M2.25 6H7.5M2.25 6V10.5"
                : "M2.25 18 9 11.25l4.286 4.286a11.948 11.948 0 014.306-6.43l.776-2.898M2.25 18H7.5M2.25 18v-4.5"} />
            </svg>
          }
        />
        <MetricCard
          label="Capital contable"
          value={hasData ? fmt(summary!.totalEquity, s) : "—"}
          color="slate"
          icon={
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Z" />
            </svg>
          }
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Recent entries */}
        <div className="lg:col-span-2">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">Últimos asientos</h3>
            <Link
              href="/dashboard/journal"
              className="text-xs font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300"
            >
              Ver todos →
            </Link>
          </div>

          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            {!hasData || summary!.recentEntries.length === 0 ? (
              <div className="py-12 text-center">
                <p className="text-sm text-slate-500 dark:text-slate-400">No hay asientos aún</p>
                <Link
                  href="/dashboard/journal/new"
                  className="mt-3 inline-block rounded-lg bg-indigo-600 px-4 py-2 text-xs font-semibold text-white hover:bg-indigo-700 transition-colors"
                >
                  + Crear primer asiento
                </Link>
              </div>
            ) : (
              <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
                <thead>
                  <tr className="bg-slate-50 dark:bg-slate-700/50">
                    <th className="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Fecha</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Descripción</th>
                    <th className="px-4 py-2.5 text-right text-xs font-semibold uppercase tracking-wide text-slate-400">Monto</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30">
                  {summary!.recentEntries.map((e: RecentEntry) => (
                    <tr
                      key={e.id}
                      className={`hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors ${e.status === "Voided" ? "opacity-50" : ""}`}
                    >
                      <td className="px-4 py-2.5 font-mono text-xs text-slate-500 dark:text-slate-400 whitespace-nowrap">
                        {formatDate(e.date)}
                      </td>
                      <td className="px-4 py-2.5 text-sm text-slate-700 dark:text-slate-300 max-w-xs">
                        <div className="flex items-center gap-2">
                          <StatusDot status={e.status} />
                          <Link
                            href={`/dashboard/journal/${e.id}`}
                            className="truncate hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
                          >
                            {e.description}
                          </Link>
                        </div>
                      </td>
                      <td className="px-4 py-2.5 text-right font-mono text-sm font-medium text-slate-900 dark:text-slate-100 whitespace-nowrap">
                        {fmt(e.totalDebit, s)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>

        {/* Quick links */}
        <div>
          <h3 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-300">Accesos rápidos</h3>
          <div className="space-y-2">
            {[
              { href: "/dashboard/journal/new",     label: "Nuevo asiento",         icon: "+" },
              { href: "/dashboard/accounts",         label: "Catálogo de cuentas",   icon: "≡" },
              { href: "/dashboard/reports/trial-balance",   label: "Balance de comprobación", icon: "⚖" },
              { href: "/dashboard/reports/income-statement",label: "Estado de resultados",     icon: "📈" },
              { href: "/dashboard/reports/balance-sheet",   label: "Balance general",          icon: "🏛" },
              { href: "/dashboard/settings",         label: "Configuración",         icon: "⚙" },
            ].map((l) => (
              <Link
                key={l.href}
                href={l.href}
                className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 hover:border-indigo-300 hover:bg-indigo-50 hover:text-indigo-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300 dark:hover:border-indigo-600 dark:hover:bg-indigo-950/30 dark:hover:text-indigo-300 transition-colors"
              >
                <span className="w-5 text-center text-base">{l.icon}</span>
                {l.label}
              </Link>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}

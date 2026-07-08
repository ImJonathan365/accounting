import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import type { DashboardSummary, RecentEntry, OverdueInvoice } from "@accounting/types";
import { IncomeExpenseChart, EquityTrendChart, RatioCards } from "./_components/DashboardCharts";

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

      {/* KPI cards — contabilidad */}
      <div className="mb-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
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

      {/* KPI cards — facturación */}
      {hasData && (
        <div className="mb-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <MetricCard
            label="Por cobrar"
            value={fmt(summary!.pendingReceivable, s)}
            sub="Facturas emitidas pendientes"
            color="green"
            icon={
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 18.75a60.07 60.07 0 0 1 15.797 2.101c.727.198 1.453-.342 1.453-1.096V18.75M3.75 4.5v.75A.75.75 0 0 1 3 6h-.75m0 0v-.375c0-.621.504-1.125 1.125-1.125H20.25M2.25 6v9m18-10.5v.75c0 .414.336.75.75.75h.75m-1.5-1.5h.375c.621 0 1.125.504 1.125 1.125v9.75c0 .621-.504 1.125-1.125 1.125h-.375m1.5-1.5H21a.75.75 0 0 0-.75.75v.75m0 0H3.75m0 0h-.375a1.125 1.125 0 0 1-1.125-1.125V15m1.5 1.5v-.75A.75.75 0 0 0 3 15h-.75M15 10.5a3 3 0 1 1-6 0 3 3 0 0 1 6 0Zm3 0h.008v.008H18V10.5Zm-12 0h.008v.008H6V10.5Z" />
              </svg>
            }
          />
          <MetricCard
            label="Por pagar"
            value={fmt(summary!.pendingPayable, s)}
            sub="Facturas de proveedores pendientes"
            color="slate"
            icon={
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Z" />
              </svg>
            }
          />
          <MetricCard
            label="Facturas vencidas"
            value={String(summary!.overdueCount)}
            sub={summary!.overdueCount > 0 ? fmt(summary!.overdueAmount, s) + " pendiente" : "Sin vencidas"}
            color={summary!.overdueCount > 0 ? "red" : "slate"}
            icon={
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
              </svg>
            }
          />
          <Link href="/dashboard/invoices/new" className="block">
            <div className="h-full rounded-xl border border-dashed border-indigo-300 bg-indigo-50/50 p-5 hover:bg-indigo-50 dark:border-indigo-700 dark:bg-indigo-950/20 dark:hover:bg-indigo-950/40 transition-colors">
              <p className="text-xs font-medium uppercase tracking-wide text-indigo-400">Acción rápida</p>
              <p className="mt-1.5 text-xl font-bold text-indigo-700 dark:text-indigo-300">+ Nueva factura</p>
              <p className="mt-1 text-xs text-indigo-400">Cobro o pago</p>
            </div>
          </Link>
        </div>
      )}

      {/* Charts section */}
      {hasData && summary!.monthlyTrend.length > 0 && (
        <div className="mb-8 space-y-6">
          <div className="grid gap-6 lg:grid-cols-2">
            <IncomeExpenseChart data={summary!.monthlyTrend} />
            <EquityTrendChart   data={summary!.monthlyTrend} />
          </div>
          <RatioCards ratios={summary!.ratios} />
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Recent entries */}
        <div className="lg:col-span-2 space-y-6">
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">Últimos asientos</h3>
            <Link
              href="/dashboard/journal"
              className="text-xs font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300"
            >
              Ver todos →
            </Link>
          </div>

          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
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

        {/* Right column: overdue + quick links */}
        <div className="space-y-6">
        {/* Overdue invoices panel */}
        <div>
          <div className="mb-3 flex items-center justify-between">
            <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">
              Facturas vencidas
              {hasData && summary!.overdueCount > 0 && (
                <span className="ml-2 inline-flex h-5 w-5 items-center justify-center rounded-full bg-red-100 text-xs font-bold text-red-600 dark:bg-red-900/40 dark:text-red-400">
                  {summary!.overdueCount}
                </span>
              )}
            </h3>
            <Link href="/dashboard/invoices" className="text-xs font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400">
              Ver facturas →
            </Link>
          </div>
          <div className="rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            {!hasData || summary!.overdueInvoices.length === 0 ? (
              <div className="py-10 text-center">
                <p className="text-sm text-slate-400">Sin facturas vencidas</p>
              </div>
            ) : (
              <ul className="divide-y divide-slate-50 dark:divide-slate-700/50">
                {summary!.overdueInvoices.slice(0, 5).map((inv: OverdueInvoice) => {
                  const due  = new Date(inv.dueDate);
                  const days = Math.floor((Date.now() - due.getTime()) / 86400000);
                  return (
                    <li key={inv.id}>
                      <Link
                        href={`/dashboard/invoices/${inv.id}`}
                        className="flex items-center justify-between gap-3 px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors"
                      >
                        <div className="min-w-0">
                          <p className="text-sm font-medium text-slate-800 dark:text-slate-200">{inv.number}</p>
                          <p className="text-xs text-slate-400 truncate">{inv.contactName}</p>
                        </div>
                        <div className="shrink-0 text-right">
                          <p className="text-sm font-semibold tabular-nums">{fmt(inv.balance, s)}</p>
                          <p className="text-xs text-red-500">{days}d vencida</p>
                        </div>
                      </Link>
                    </li>
                  );
                })}
              </ul>
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
        </div>{/* end right column space-y-6 */}
      </div>
    </>
  );
}

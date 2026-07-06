import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { LedgerFilterForm } from "./_components/LedgerFilterForm";
import type { Ledger } from "@accounting/types";

export const dynamic = "force-dynamic";

function today() {
  return new Date().toISOString().split("T")[0];
}

function firstOfYear() {
  return `${new Date().getFullYear()}-01-01`;
}

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function fmtDate(iso: string) {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}

function BalanceCell({ value }: { value: number }) {
  const abs = Math.abs(value);
  const label = value >= 0 ? "D" : "C";
  const color = value >= 0
    ? "text-blue-700 dark:text-blue-400"
    : "text-orange-700 dark:text-orange-400";
  return (
    <span className={`font-mono ${color}`}>
      {fmt(abs)} <span className="text-xs opacity-60">{label}</span>
    </span>
  );
}

const PAGE_SIZE = 50;

export default async function LedgerPage({
  searchParams,
}: {
  searchParams: Promise<{ accountId?: string; from?: string; to?: string; page?: string }>;
}) {
  const sp = await searchParams;

  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const accounts = await apiClient.accounts.list(orgId, token);

  const fromDate = sp.from || firstOfYear();
  const toDate   = sp.to   || today();
  const page     = Math.max(1, parseInt(sp.page ?? "1", 10) || 1);

  let ledger: Ledger | null = null;
  let error: string | null  = null;

  if (sp.accountId) {
    try {
      ledger = await apiClient.reports.ledger(orgId, sp.accountId, fromDate, toDate, token, page, PAGE_SIZE);
    } catch (e) {
      error = e instanceof Error ? e.message : "Error al cargar el libro mayor.";
    }
  }

  const current = {
    accountId: sp.accountId ?? "",
    from: fromDate,
    to:   toDate,
  };

  return (
    <>
      <div className="mb-6 flex items-center gap-3">
        <Link
          href="/dashboard/reports"
          className="text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
        >
          ← Reportes
        </Link>
        <span className="text-slate-300 dark:text-slate-600">/</span>
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Libro mayor</h2>
      </div>

      <div className="mb-6 rounded-xl border border-slate-200 bg-white px-5 py-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <LedgerFilterForm accounts={accounts} current={current} />
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-600 dark:border-red-800 dark:bg-red-950/30 dark:text-red-400">
          {error}
        </div>
      )}

      {!sp.accountId && !error && (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white py-20 text-center dark:border-slate-600 dark:bg-slate-800">
          <p className="text-sm font-medium text-slate-600 dark:text-slate-300">
            Selecciona una cuenta y un período para consultar su libro mayor.
          </p>
        </div>
      )}

      {ledger && (
        <>
          {/* Summary header */}
          <div className="mb-4 grid grid-cols-2 gap-4 sm:grid-cols-4">
            {[
              { label: "Cuenta",           value: `${ledger.accountCode} — ${ledger.accountName}` },
              { label: "Período",          value: `${fmtDate(ledger.from)} al ${fmtDate(ledger.to)}` },
              { label: "Saldo inicial",    value: fmt(Math.abs(ledger.openingBalance)), sub: ledger.openingBalance >= 0 ? "Deudor" : "Acreedor" },
              { label: "Saldo final",      value: fmt(Math.abs(ledger.closingBalance)), sub: ledger.closingBalance >= 0 ? "Deudor" : "Acreedor" },
            ].map((card) => (
              <div key={card.label} className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm dark:border-slate-700 dark:bg-slate-800">
                <p className="text-xs text-slate-500 dark:text-slate-400">{card.label}</p>
                <p className="mt-0.5 font-semibold text-slate-900 dark:text-slate-100 truncate">{card.value}</p>
                {card.sub && <p className="text-xs text-slate-400">{card.sub}</p>}
              </div>
            ))}
          </div>

          {ledger.lines.length === 0 ? (
            <div className="rounded-xl border border-dashed border-slate-300 bg-white py-16 text-center dark:border-slate-600 dark:bg-slate-800">
              <p className="text-sm text-slate-500 dark:text-slate-400">
                No hay movimientos en este período para la cuenta seleccionada.
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
              <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
                <thead>
                  <tr className="bg-slate-50 dark:bg-slate-700/50">
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Fecha</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Descripción</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Referencia</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Débito</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Crédito</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Saldo</th>
                    <th className="px-4 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
                  {/* Opening balance row */}
                  <tr className="bg-slate-50/50 dark:bg-slate-700/20">
                    <td className="px-4 py-2 text-xs text-slate-400 dark:text-slate-500" colSpan={5}>
                      Saldo inicial al {fmtDate(ledger.from)}
                    </td>
                    <td className="px-4 py-2 text-right">
                      <BalanceCell value={ledger.openingBalance} />
                    </td>
                    <td />
                  </tr>

                  {ledger.lines.map((line, i) => (
                    <tr key={`${line.entryId}-${i}`} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                      <td className="px-4 py-3 font-mono text-sm text-slate-700 dark:text-slate-300 whitespace-nowrap">
                        {fmtDate(line.date)}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300 max-w-xs">
                        <p className="truncate">{line.description}</p>
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-400 dark:text-slate-500 hidden sm:table-cell">
                        {line.reference ?? "—"}
                      </td>
                      <td className="px-4 py-3 text-right font-mono text-sm text-slate-700 dark:text-slate-300">
                        {line.debit > 0 ? fmt(line.debit) : "—"}
                      </td>
                      <td className="px-4 py-3 text-right font-mono text-sm text-slate-700 dark:text-slate-300">
                        {line.credit > 0 ? fmt(line.credit) : "—"}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <BalanceCell value={line.runningBalance} />
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Link
                          href={`/dashboard/journal/${line.entryId}`}
                          className="text-xs font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300"
                        >
                          Ver →
                        </Link>
                      </td>
                    </tr>
                  ))}

                  {/* Closing balance row */}
                  <tr className="bg-slate-50 dark:bg-slate-700/30 font-semibold">
                    <td className="px-4 py-2 text-xs text-slate-500 dark:text-slate-400" colSpan={3}>
                      Saldo al cierre de página — {ledger.totalLines} movimiento{ledger.totalLines !== 1 ? "s" : ""} en total
                    </td>
                    <td className="px-4 py-2 text-right font-mono text-sm text-slate-900 dark:text-slate-100">
                      {fmt(ledger.lines.reduce((s, l) => s + l.debit, 0))}
                    </td>
                    <td className="px-4 py-2 text-right font-mono text-sm text-slate-900 dark:text-slate-100">
                      {fmt(ledger.lines.reduce((s, l) => s + l.credit, 0))}
                    </td>
                    <td className="px-4 py-2 text-right">
                      <BalanceCell value={ledger.closingBalance} />
                    </td>
                    <td />
                  </tr>
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {ledger.totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between text-sm text-slate-500 dark:text-slate-400">
              <span>
                Página {ledger.page} de {ledger.totalPages} · {ledger.totalLines} movimientos
              </span>
              <div className="flex gap-2">
                {ledger.page > 1 && (
                  <LedgerPageLink sp={sp} page={ledger.page - 1} label="← Anterior" />
                )}
                {ledger.page < ledger.totalPages && (
                  <LedgerPageLink sp={sp} page={ledger.page + 1} label="Siguiente →" />
                )}
              </div>
            </div>
          )}
        </>
      )}
    </>
  );
}

function LedgerPageLink({
  sp, page, label,
}: {
  sp: Record<string, string | undefined>;
  page: number;
  label: string;
}) {
  const params = new URLSearchParams();
  if (sp.accountId) params.set("accountId", sp.accountId);
  if (sp.from)      params.set("from", sp.from);
  if (sp.to)        params.set("to", sp.to);
  params.set("page", String(page));
  return (
    <Link
      href={`/dashboard/reports/ledger?${params.toString()}`}
      className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
    >
      {label}
    </Link>
  );
}

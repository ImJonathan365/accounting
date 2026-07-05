import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { Pagination } from "@/components/Pagination";
import { JournalFilterBar } from "./_components/JournalFilterBar";
import type { JournalEntrySummary, JournalStatus } from "@accounting/types";

export const dynamic = "force-dynamic";

const PAGE_SIZE = 25;

function formatDate(iso: string) {
  const [, m, d] = iso.split("-");
  return `${d}/${m}`;
}

function StatusBadge({ status }: { status: JournalStatus }) {
  if (status === "Voided")
    return (
      <span className="inline-flex rounded-full bg-red-50 px-2.5 py-0.5 text-xs font-medium text-red-600 dark:bg-red-900/30 dark:text-red-400">
        Anulado
      </span>
    );
  if (status === "Posted")
    return (
      <span className="inline-flex rounded-full bg-green-50 px-2.5 py-0.5 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400">
        Registrado
      </span>
    );
  return (
    <span className="inline-flex rounded-full bg-amber-50 px-2.5 py-0.5 text-xs font-medium text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
      Borrador
    </span>
  );
}

function formatAmount(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default async function JournalPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; from?: string; to?: string; status?: string; search?: string }>;
}) {
  const sp   = await searchParams;
  const page = Math.max(1, parseInt(sp.page ?? "1", 10) || 1);

  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const filters = {
    from:   sp.from   || undefined,
    to:     sp.to     || undefined,
    status: sp.status || undefined,
    search: sp.search || undefined,
  };

  const result = await apiClient.journal.list(orgId, token, page, PAGE_SIZE, filters);
  const { items: entries, total, totalPages } = result;

  const filterParams = new URLSearchParams();
  if (filters.from)   filterParams.set("from",   filters.from);
  if (filters.to)     filterParams.set("to",     filters.to);
  if (filters.status) filterParams.set("status", filters.status);
  if (filters.search) filterParams.set("search", filters.search);

  return (
    <>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Diario contable</h2>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {total} asiento{total !== 1 ? "s" : ""} encontrado{total !== 1 ? "s" : ""}
          </p>
        </div>
        <Link
          href="/dashboard/journal/new"
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 transition-colors"
        >
          + Nuevo asiento
        </Link>
      </div>

      <div className="mb-4 rounded-xl border border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-800">
        <JournalFilterBar />
      </div>

      {total === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white py-20 text-center dark:border-slate-600 dark:bg-slate-800">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 dark:bg-slate-700">
            <svg className="h-6 w-6 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <p className="text-sm font-medium text-slate-600 dark:text-slate-300">No hay asientos registrados</p>
          <p className="mt-1 text-sm text-slate-400 dark:text-slate-500">
            Usa el botón &quot;Nuevo asiento&quot; para registrar tu primer movimiento.
          </p>
        </div>
      ) : (
        <>
          <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
              <thead>
                <tr className="bg-slate-50 dark:bg-slate-700/50">
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Fecha</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Descripción</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Referencia</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Total</th>
                  <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden md:table-cell">Estado</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
                {entries.map((e: JournalEntrySummary) => (
                  <tr
                    key={e.id}
                    className={[
                      "hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors",
                      e.status === "Voided" ? "opacity-60" : "",
                    ].join(" ")}
                  >
                    <td className="px-4 py-3 font-mono text-sm text-slate-700 dark:text-slate-300 whitespace-nowrap">
                      {formatDate(e.date)}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300 max-w-xs">
                      <p className="truncate">{e.description}</p>
                      {e.voidsEntryId && (
                        <p className="text-xs text-slate-400 dark:text-slate-500">Anula asiento anterior</p>
                      )}
                    </td>
                    <td className="px-4 py-3 text-sm text-slate-400 dark:text-slate-500 hidden sm:table-cell">
                      {e.reference ?? "—"}
                    </td>
                    <td className="px-4 py-3 text-right font-mono text-sm font-medium text-slate-900 dark:text-slate-100">
                      {formatAmount(e.totalDebit)}
                    </td>
                    <td className="px-4 py-3 text-center hidden md:table-cell">
                      <StatusBadge status={e.status} />
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        href={`/dashboard/journal/${e.id}`}
                        className="text-xs font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300"
                      >
                        Ver →
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination
            page={page}
            totalPages={totalPages}
            total={total}
            pageSize={PAGE_SIZE}
            buildHref={(p) => {
              const params = new URLSearchParams(filterParams);
              params.set("page", String(p));
              return `/dashboard/journal?${params}`;
            }}
          />
        </>
      )}
    </>
  );
}

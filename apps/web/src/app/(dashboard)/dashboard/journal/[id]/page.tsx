import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { VoidModal } from "../_components/VoidModal";
import { PostButton } from "../_components/PostButton";
import { DeleteDraftButton } from "../_components/DeleteDraftButton";
import type { JournalLine, JournalStatus } from "@accounting/types";

export const dynamic = "force-dynamic";

function formatDate(iso: string) {
  const [y, m, d] = iso.split("-");
  return `${d}/${m}/${y}`;
}

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
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

export default async function JournalEntryDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [token, orgId, role] = await Promise.all([getServerToken(), getCurrentOrgId(), getCurrentUserRole()]);
  if (!token || !orgId) redirect("/login");

  let entry;
  try {
    entry = await apiClient.journal.get(orgId, id, token);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    throw err;
  }

  const isVoided    = entry.status === "Voided";
  const isVoidEntry = !!entry.voidsEntryId;
  const canManage   = role === "owner" || role === "admin";

  return (
    <>
      {/* Header */}
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <Link
            href="/dashboard/journal"
            className="mb-2 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-300"
          >
            ← Diario
          </Link>
          <h2 className={`text-2xl font-bold text-slate-900 dark:text-slate-100 ${isVoided ? "line-through opacity-60" : ""}`}>
            {entry.description}
          </h2>
          <div className="mt-1 flex flex-wrap items-center gap-3 text-sm text-slate-500 dark:text-slate-400">
            <span>{formatDate(entry.date)}</span>
            {entry.reference && (
              <span>Ref: <span className="font-medium">{entry.reference}</span></span>
            )}
            <StatusBadge status={entry.status} />
          </div>
        </div>

        <div className="flex flex-wrap gap-2">
          {entry.status === "Draft" && (
            <>
              <Link
                href={`/dashboard/journal/${entry.id}/edit`}
                className="flex items-center gap-1.5 rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700 transition-colors"
              >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z" />
                </svg>
                Editar
              </Link>
              {canManage && <DeleteDraftButton entryId={entry.id} />}
              {canManage && <PostButton entryId={entry.id} />}
            </>
          )}
          {entry.status === "Posted" && !isVoidEntry && canManage && (
            <VoidModal entryId={entry.id} description={entry.description} />
          )}
        </div>
      </div>

      {/* Draft banner */}
      {entry.status === "Draft" && (
        <div className="mb-4 flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm dark:border-amber-800 dark:bg-amber-950/20">
          <svg className="mt-0.5 h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
          </svg>
          <p className="text-amber-800 dark:text-amber-300">
            {canManage
              ? <>Este asiento está en borrador. Usa el botón <strong>Registrar asiento</strong> para publicarlo en el diario.</>
              : "Este asiento está en borrador y aún no ha sido registrado en el diario."}
          </p>
        </div>
      )}

      {/* Void info banners */}
      {isVoidEntry && (
        <div className="mb-4 flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm dark:border-amber-800 dark:bg-amber-950/20">
          <svg className="mt-0.5 h-4 w-4 shrink-0 text-amber-600 dark:text-amber-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
          </svg>
          <div>
            <p className="font-medium text-amber-800 dark:text-amber-300">Contra-asiento de anulación</p>
            <p className="text-amber-700 dark:text-amber-400">
              Este asiento revierte los movimientos de{" "}
              <Link
                href={`/dashboard/journal/${entry.voidsEntryId}`}
                className="underline hover:text-amber-900 dark:hover:text-amber-200"
              >
                asiento original →
              </Link>
            </p>
          </div>
        </div>
      )}

      {isVoided && (
        <div className="mb-4 space-y-1 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm dark:border-red-800 dark:bg-red-950/20">
          <p className="font-medium text-red-700 dark:text-red-400">
            Asiento anulado
            {entry.voidedAtUtc && (
              <span className="ml-2 font-normal text-red-500 dark:text-red-500">
                — {new Date(entry.voidedAtUtc).toLocaleDateString("es-GT")}
              </span>
            )}
          </p>
          {entry.voidReason && (
            <p className="text-red-600 dark:text-red-400">Razón: {entry.voidReason}</p>
          )}
          {entry.voidedByEntryId && (
            <p className="text-red-500 dark:text-red-500">
              Ver{" "}
              <Link
                href={`/dashboard/journal/${entry.voidedByEntryId}`}
                className="underline hover:text-red-700 dark:hover:text-red-300"
              >
                contra-asiento →
              </Link>
            </p>
          )}
        </div>
      )}

      {/* Lines table */}
      <div className={`overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800 ${isVoided ? "opacity-70" : ""}`}>
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
          <thead>
            <tr className="bg-slate-50 dark:bg-slate-700/50">
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Cuenta</th>
              <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 w-36">Débito</th>
              <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 w-36">Crédito</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden md:table-cell">Nota</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
            {entry.lines.map((line: JournalLine) => (
              <tr key={line.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                <td className="px-4 py-3">
                  <span className="font-mono text-sm font-medium text-slate-600 dark:text-slate-400">{line.accountCode}</span>
                  <span className="ml-2 text-sm text-slate-700 dark:text-slate-300">{line.accountName}</span>
                </td>
                <td className="px-4 py-3 text-right font-mono text-sm text-slate-900 dark:text-slate-100">
                  {line.debit > 0 ? fmt(line.debit) : ""}
                </td>
                <td className="px-4 py-3 text-right font-mono text-sm text-slate-900 dark:text-slate-100">
                  {line.credit > 0 ? fmt(line.credit) : ""}
                </td>
                <td className="px-4 py-3 text-sm text-slate-400 dark:text-slate-500 hidden md:table-cell">
                  {line.note ?? "—"}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t-2 border-slate-200 bg-slate-50 dark:border-slate-600 dark:bg-slate-700/50">
              <td className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
                Totales
              </td>
              <td className="px-4 py-3 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
                {fmt(entry.totalDebit)}
              </td>
              <td className="px-4 py-3 text-right font-mono text-sm font-bold text-slate-900 dark:text-slate-100">
                {fmt(entry.totalCredit)}
              </td>
              <td className="hidden md:table-cell" />
            </tr>
          </tfoot>
        </table>
      </div>
    </>
  );
}

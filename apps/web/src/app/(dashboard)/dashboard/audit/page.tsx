import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import type { AuditLog } from "@accounting/types";

export const dynamic = "force-dynamic";

const ACTION_LABELS: Record<string, string> = {
  "journal.draft_created":   "Creó borrador",
  "journal.posted_created":  "Registró asiento",
  "journal.updated":         "Editó borrador",
  "journal.posted":          "Registró asiento",
  "journal.voided":          "Anuló asiento",
  "journal.deleted":         "Eliminó borrador",
  "account.created":         "Creó cuenta",
  "account.updated":         "Editó cuenta",
  "account.toggled":         "Activó/desactivó cuenta",
  "member.invited":          "Invitó miembro",
  "member.role_changed":     "Cambió rol",
  "member.removed":          "Eliminó miembro",
};

const ENTITY_COLORS: Record<string, string> = {
  JournalEntry: "bg-indigo-50 text-indigo-700 dark:bg-indigo-950/40 dark:text-indigo-300",
  Account:      "bg-blue-50 text-blue-700 dark:bg-blue-950/40 dark:text-blue-300",
  Member:       "bg-purple-50 text-purple-700 dark:bg-purple-950/40 dark:text-purple-300",
};

function formatDate(iso: string) {
  return new Date(iso).toLocaleString("es-GT", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
}

export default async function AuditPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string }>;
}) {
  const { page: pageParam } = await searchParams;
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  if (role !== "owner") redirect("/dashboard");

  const page = Math.max(1, parseInt(pageParam ?? "1", 10));

  let result;
  try {
    result = await apiClient.audit.list(orgId, token, page, 50);
  } catch (err) {
    if (err instanceof ApiError && err.status === 403) redirect("/dashboard");
    throw err;
  }

  return (
    <>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Auditoría</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Registro de todas las acciones realizadas por los miembros del equipo.
        </p>
      </div>

      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
          <thead>
            <tr className="bg-slate-50 dark:bg-slate-700/50">
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Fecha</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Usuario</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Acción</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden md:table-cell">Entidad</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden lg:table-cell">Detalle</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
            {result.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-sm text-slate-400 dark:text-slate-500">
                  No hay eventos registrados.
                </td>
              </tr>
            )}
            {result.items.map((log: AuditLog) => (
              <tr key={log.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                <td className="px-4 py-3 text-xs text-slate-500 dark:text-slate-400 whitespace-nowrap">
                  {formatDate(log.createdAtUtc)}
                </td>
                <td className="px-4 py-3 text-sm font-medium text-slate-700 dark:text-slate-300">
                  {log.userName}
                </td>
                <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">
                  {ACTION_LABELS[log.action] ?? log.action}
                </td>
                <td className="px-4 py-3 hidden md:table-cell">
                  <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${ENTITY_COLORS[log.entityType] ?? "bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-400"}`}>
                    {log.entityType}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-slate-500 dark:text-slate-400 hidden lg:table-cell">
                  {log.details ?? "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {result.totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between text-sm text-slate-500 dark:text-slate-400">
          <span>{result.total} eventos en total</span>
          <div className="flex gap-2">
            {page > 1 && (
              <a
                href={`/dashboard/audit?page=${page - 1}`}
                className="rounded-lg border border-slate-200 px-3 py-1.5 hover:bg-slate-50 dark:border-slate-600 dark:hover:bg-slate-700 transition-colors"
              >
                ← Anterior
              </a>
            )}
            <span className="px-3 py-1.5">Página {page} de {result.totalPages}</span>
            {page < result.totalPages && (
              <a
                href={`/dashboard/audit?page=${page + 1}`}
                className="rounded-lg border border-slate-200 px-3 py-1.5 hover:bg-slate-50 dark:border-slate-600 dark:hover:bg-slate-700 transition-colors"
              >
                Siguiente →
              </a>
            )}
          </div>
        </div>
      )}
    </>
  );
}

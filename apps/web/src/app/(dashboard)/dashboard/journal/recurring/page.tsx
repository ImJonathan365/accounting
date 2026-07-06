import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import type { RecurringEntry } from "@accounting/types";
import { RecurringList, GenerateButton } from "./_components/RecurringList";

export const dynamic = "force-dynamic";

const FREQ_COLOR: Record<string, string> = {
  Weekly:    "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  Biweekly:  "bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300",
  Monthly:   "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
  Quarterly: "bg-teal-100 text-teal-700 dark:bg-teal-900/40 dark:text-teal-300",
  Annually:  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
};

export default async function RecurringPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  let entries: RecurringEntry[];
  try {
    entries = await apiClient.recurring.list(orgId, token);
  } catch {
    return <PageError message="No se pudieron cargar los asientos recurrentes." />;
  }

  const today    = new Date().toISOString().slice(0, 10);
  const pending  = entries.filter((e) => e.isActive && e.nextDate <= today).length;
  const canEdit  = role === "owner" || role === "admin";

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Asientos Recurrentes
          </h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Plantillas que generan borradores de asiento automáticamente según su frecuencia.
          </p>
        </div>
        {canEdit && (
          <div className="flex gap-2">
            {pending > 0 && (
              <GenerateButton orgId={orgId} token={token} pending={pending} />
            )}
            <Link
              href="/dashboard/journal/recurring/new"
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 transition-colors"
            >
              + Nueva plantilla
            </Link>
          </div>
        )}
      </div>

      {entries.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500 dark:text-slate-400">No hay asientos recurrentes configurados.</p>
          {canEdit && (
            <Link
              href="/dashboard/journal/recurring/new"
              className="mt-3 inline-block rounded-lg bg-indigo-600 px-4 py-2 text-xs font-semibold text-white hover:bg-indigo-700"
            >
              + Crear primera plantilla
            </Link>
          )}
        </div>
      ) : (
        <RecurringList entries={entries} orgId={orgId} token={token} canEdit={canEdit} freqColor={FREQ_COLOR} />
      )}
    </div>
  );
}

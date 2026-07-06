import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import type { Account, YearEndStatus } from "@accounting/types";
import { YearEndForm } from "./_components/YearEndForm";

export const dynamic = "force-dynamic";

export default async function YearEndPage({
  searchParams,
}: {
  searchParams: Promise<{ year?: string }>;
}) {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  if (role !== "owner") redirect("/dashboard");

  const sp        = await searchParams;
  const year      = parseInt(sp.year ?? "") || new Date().getFullYear() - 1;
  const thisYear  = new Date().getFullYear();

  let status: YearEndStatus;
  let equityAccounts: Account[];

  try {
    [status, equityAccounts] = await Promise.all([
      apiClient.yearEnd.getStatus(orgId, year, token),
      apiClient.accounts.list(orgId, token).then((accs: Account[]) =>
        accs.filter((a) => a.type === "Equity" && a.isPostable)),
    ]);
  } catch {
    return <PageError message="No se pudo cargar el cierre anual." />;
  }

  const availableYears = Array.from(
    { length: Math.max(1, thisYear - 2000) },
    (_, i) => thisYear - 1 - i,
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
          Cierre Anual
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Traslada la utilidad o pérdida del ejercicio a resultados acumulados.
        </p>
      </div>

      {/* Year selector */}
      <div className="flex items-center gap-3">
        <span className="text-sm text-slate-600 dark:text-slate-400">Año fiscal:</span>
        <div className="flex gap-1">
          {availableYears.slice(0, 5).map((y) => (
            <a
              key={y}
              href={`/dashboard/year-end?year=${y}`}
              className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                y === year
                  ? "bg-indigo-600 text-white"
                  : "border border-slate-300 text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
              }`}
            >
              {y}
            </a>
          ))}
        </div>
      </div>

      {/* Status / form */}
      {status.isClosed ? (
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-6 dark:border-emerald-800 dark:bg-emerald-950/30">
          <div className="flex items-start gap-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-100 dark:bg-emerald-900/50">
              <svg className="h-5 w-5 text-emerald-600 dark:text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-emerald-900 dark:text-emerald-200">
                El año {year} ya fue cerrado
              </h3>
              {status.closedByName && (
                <p className="mt-1 text-sm text-emerald-700 dark:text-emerald-300">
                  Cerrado por {status.closedByName}
                  {status.closedAtUtc && ` el ${new Date(status.closedAtUtc).toLocaleDateString("es-GT")}`}
                </p>
              )}
              {status.journalEntryId && (
                <a
                  href={`/dashboard/journal/${status.journalEntryId}`}
                  className="mt-2 inline-block text-sm font-medium text-emerald-700 underline hover:text-emerald-900 dark:text-emerald-400 dark:hover:text-emerald-200"
                >
                  Ver asiento de cierre →
                </a>
              )}
            </div>
          </div>
        </div>
      ) : (
        <YearEndForm
          year={year}
          orgId={orgId}
          token={token}
          equityAccounts={equityAccounts}
        />
      )}

      {/* Explanation */}
      <div className="rounded-xl border border-slate-200 bg-slate-50 p-5 dark:border-slate-700 dark:bg-slate-800/50">
        <h3 className="mb-2 text-sm font-semibold text-slate-700 dark:text-slate-300">¿Qué hace el cierre anual?</h3>
        <ul className="space-y-1 text-sm text-slate-600 dark:text-slate-400">
          <li>• Crea un asiento contable que <strong>zeroa</strong> todas las cuentas de ingreso y gasto del año.</li>
          <li>• Transfiere la utilidad (o pérdida) neta a la cuenta de <strong>Resultados Acumulados</strong> que selecciones.</li>
          <li>• El asiento de cierre <strong>no afecta</strong> el estado de resultados ni el balance de comprobación del año cerrado — solo el Balance General.</li>
          <li>• Solo puede hacerlo el <strong>propietario</strong> de la organización, y no se puede deshacer.</li>
        </ul>
      </div>
    </div>
  );
}

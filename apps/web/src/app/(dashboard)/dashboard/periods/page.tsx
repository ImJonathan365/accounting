import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { redirect } from "next/navigation";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { PeriodGrid } from "./_components/PeriodGrid";

export default async function PeriodsPage({
  searchParams,
}: {
  searchParams: Promise<{ year?: string }>;
}) {
  const sp   = await searchParams;
  const [token, orgId, role] = await Promise.all([
    getServerToken(),
    getCurrentOrgId(),
    getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  const year = parseInt(sp.year ?? String(new Date().getFullYear()), 10) || new Date().getFullYear();

  let periods;
  try {
    periods = await apiClient.periods.list(orgId, token, year);
  } catch {
    return <PageError message="No se pudieron cargar los períodos contables." />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Períodos contables</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Cierra los períodos para bloquear la edición de asientos históricos.
          </p>
        </div>
        <YearNav year={year} />
      </div>

      {role === "member" && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-300">
          Solo propietarios y administradores pueden cerrar períodos. Solo el propietario puede reabrirlos.
        </div>
      )}

      <PeriodGrid
        periods={periods}
        year={year}
        orgId={orgId}
        role={role ?? "member"}
        token={token}
      />
    </div>
  );
}

function YearNav({ year }: { year: number }) {
  const now = new Date().getFullYear();
  return (
    <div className="flex items-center gap-2">
      <a
        href={`/dashboard/periods?year=${year - 1}`}
        className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
      >
        ← {year - 1}
      </a>
      <span className="px-2 text-sm font-semibold text-slate-900 dark:text-slate-100">{year}</span>
      {year < now && (
        <a
          href={`/dashboard/periods?year=${year + 1}`}
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
        >
          {year + 1} →
        </a>
      )}
    </div>
  );
}

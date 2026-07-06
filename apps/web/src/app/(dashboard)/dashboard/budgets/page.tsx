import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { CreateBudgetButton } from "./_components/CreateBudgetButton";

export const dynamic = "force-dynamic";

export default async function BudgetsPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit = role === "owner" || role === "admin";

  let budgets;
  try {
    budgets = await apiClient.budgets.list(orgId, token);
  } catch {
    return <PageError message="No se pudo cargar los presupuestos." />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Presupuestos</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Compara lo presupuestado vs lo ejecutado.</p>
        </div>
        {canEdit && <CreateBudgetButton orgId={orgId} token={token} />}
      </div>

      {budgets.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500">No hay presupuestos. Crea uno para empezar.</p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {budgets.map(b => (
            <Link
              key={b.id}
              href={`/dashboard/budgets/${b.id}`}
              className="group rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition-all hover:border-indigo-300 hover:shadow-md dark:border-slate-700 dark:bg-slate-800"
            >
              <div className="flex items-start justify-between">
                <div>
                  <p className="font-semibold text-slate-900 group-hover:text-indigo-700 dark:text-slate-100 dark:group-hover:text-indigo-300">
                    {b.name}
                  </p>
                  <p className="mt-0.5 text-sm text-slate-400">Año {b.year}</p>
                  <p className="mt-1 text-xs text-slate-400">{b.lines.length} línea{b.lines.length !== 1 ? "s" : ""}</p>
                </div>
                {b.isActive && (
                  <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">
                    Activo
                  </span>
                )}
              </div>
              <p className="mt-3 text-xs text-indigo-600 dark:text-indigo-400">Ver detalle →</p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

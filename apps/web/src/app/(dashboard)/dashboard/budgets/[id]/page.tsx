import { redirect, notFound } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { BudgetGrid } from "./_components/BudgetGrid";
import { BudgetVsActualTable } from "./_components/BudgetVsActualTable";

export const dynamic = "force-dynamic";

const MONTHS = ["Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic"];

export default async function BudgetDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const { id } = await params;
  const canEdit = role === "owner" || role === "admin";

  try {
    const [budget, vsActual, accounts] = await Promise.all([
      apiClient.budgets.getById(orgId, id, token),
      apiClient.budgets.getVsActual(orgId, id, token),
      apiClient.accounts.list(orgId, token),
    ]);

    const postableAccounts = accounts.filter(a => a.isPostable);

    return (
      <div className="space-y-8">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">{budget.name}</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Año {budget.year} · {budget.lines.length} cuenta{budget.lines.length !== 1 ? "s" : ""} presupuestadas</p>
        </div>

        <div>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-400">Editar presupuesto</h2>
          <BudgetGrid budget={budget} accounts={postableAccounts} orgId={orgId} token={token} canEdit={canEdit} months={MONTHS} />
        </div>

        <div>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-400">Presupuesto vs Real</h2>
          <BudgetVsActualTable data={vsActual} months={MONTHS} />
        </div>
      </div>
    );
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    return <PageError message="No se pudo cargar el presupuesto." />;
  }
}

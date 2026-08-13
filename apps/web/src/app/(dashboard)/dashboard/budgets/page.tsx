import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { CreateBudgetButton } from "./_components/CreateBudgetButton";
import { BudgetList } from "./_components/BudgetList";

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

      <BudgetList budgets={budgets} orgId={orgId} token={token} canEdit={canEdit} />
    </div>
  );
}

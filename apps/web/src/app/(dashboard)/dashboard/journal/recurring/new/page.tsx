import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import type { Account } from "@accounting/types";
import { CreateRecurringForm } from "../_components/CreateRecurringForm";

export default async function NewRecurringPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  if (role !== "owner" && role !== "admin") redirect("/dashboard/journal/recurring");

  const accounts: Account[] = await apiClient.accounts.list(orgId, token).catch(() => []);
  const postable = accounts.filter((a) => a.isPostable && a.isActive);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
          Nueva plantilla recurrente
        </h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Define las líneas contables y la frecuencia de generación.
        </p>
      </div>
      <CreateRecurringForm accounts={postable} orgId={orgId} token={token} />
    </div>
  );
}

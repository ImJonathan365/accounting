import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { CreateAccountForm } from "./_components/CreateAccountForm";
import { AccountsTable } from "./_components/AccountsTable";

export const dynamic = "force-dynamic";

export default async function AccountsPage() {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const accounts = await apiClient.accounts.list(orgId, token);
  const active   = accounts.filter((a) => a.isActive).length;

  return (
    <>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Catálogo de cuentas</h2>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {accounts.length} cuenta{accounts.length !== 1 ? "s" : ""} —{" "}
            {active} activa{active !== 1 ? "s" : ""}
          </p>
        </div>
        <CreateAccountForm accounts={accounts} />
      </div>

      {accounts.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-300 bg-white py-20 text-center dark:border-slate-600 dark:bg-slate-800">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 dark:bg-slate-700">
            <svg className="h-6 w-6 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
            </svg>
          </div>
          <p className="text-sm font-medium text-slate-600 dark:text-slate-300">No hay cuentas registradas</p>
          <p className="mt-1 text-sm text-slate-400 dark:text-slate-500">Usa el botón &quot;Nueva cuenta&quot; para comenzar.</p>
        </div>
      ) : (
        <AccountsTable accounts={accounts} />
      )}
    </>
  );
}

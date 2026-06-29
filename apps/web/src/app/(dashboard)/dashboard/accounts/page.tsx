import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import type { Account, AccountType } from "@accounting/types";
import { CreateAccountForm } from "./_components/CreateAccountForm";

export const dynamic = "force-dynamic";

const TYPE_LABELS: Record<AccountType, string> = {
  Asset: "Activo",
  Liability: "Pasivo",
  Equity: "Capital",
  Income: "Ingreso",
  Expense: "Gasto",
};

const TYPE_COLORS: Record<AccountType, string> = {
  Asset: "bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
  Liability: "bg-orange-50 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
  Equity: "bg-purple-50 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400",
  Income: "bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400",
  Expense: "bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400",
};

export default async function AccountsPage() {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const accounts = await apiClient.accounts.list(orgId, token);

  return (
    <>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Catálogo de cuentas</h2>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {accounts.length} cuenta{accounts.length !== 1 ? "s" : ""} registrada{accounts.length !== 1 ? "s" : ""}
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
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
          <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-700/50">
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Código</th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Nombre</th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Tipo</th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Postable</th>
                <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Estado</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
              {accounts.map((account: Account) => (
                <tr key={account.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                  <td className="px-4 py-3 font-mono text-sm font-medium text-slate-900 dark:text-slate-100">{account.code}</td>
                  <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">{account.name}</td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${TYPE_COLORS[account.type]}`}>
                      {TYPE_LABELS[account.type]}
                    </span>
                  </td>
                  <td className="px-4 py-3 hidden sm:table-cell">
                    {account.isPostable ? (
                      <span className="inline-flex rounded-full bg-green-50 px-2 py-0.5 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400">Sí</span>
                    ) : (
                      <span className="inline-flex rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-500 dark:bg-slate-700 dark:text-slate-400">No</span>
                    )}
                  </td>
                  <td className="px-4 py-3 hidden sm:table-cell">
                    {account.isActive ? (
                      <span className="inline-flex rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">Activa</span>
                    ) : (
                      <span className="inline-flex rounded-full bg-red-50 px-2 py-0.5 text-xs font-medium text-red-600 dark:bg-red-900/30 dark:text-red-400">Inactiva</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { CreateBankAccountForm } from "./_components/CreateBankAccountForm";
import type { Account } from "@accounting/types";

export const dynamic = "force-dynamic";

export default async function BankPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit = role === "owner" || role === "admin";

  let bankAccounts, accounts: Account[];
  try {
    [bankAccounts, accounts] = await Promise.all([
      apiClient.bank.list(orgId, token),
      apiClient.accounts.list(orgId, token),
    ]);
  } catch {
    return <PageError message="No se pudo cargar la información bancaria." />;
  }

  const assetAccounts = accounts.filter(a => a.type === "Asset" && a.isPostable);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Conciliación bancaria</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Gestiona tus cuentas bancarias y concilia los movimientos.
          </p>
        </div>
        {canEdit && <CreateBankAccountForm orgId={orgId} token={token} accounts={assetAccounts} />}
      </div>

      {bankAccounts.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500 dark:text-slate-400">No hay cuentas bancarias configuradas.</p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {bankAccounts.map((ba) => (
            <Link
              key={ba.id}
              href={`/dashboard/bank/${ba.id}`}
              className="group rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition-all hover:border-indigo-300 hover:shadow-md dark:border-slate-700 dark:bg-slate-800"
            >
              <div className="flex items-start justify-between">
                <div>
                  <p className="font-semibold text-slate-900 group-hover:text-indigo-700 dark:text-slate-100 dark:group-hover:text-indigo-300">
                    {ba.name}
                  </p>
                  {ba.bankName && (
                    <p className="mt-0.5 text-xs text-slate-400">{ba.bankName}{ba.accountNumber ? ` · ${ba.accountNumber}` : ""}</p>
                  )}
                  <p className="mt-1 text-xs font-mono text-slate-500 dark:text-slate-400">{ba.linkedAccountCode} — {ba.linkedAccountName}</p>
                </div>
                {ba.pendingCount > 0 && (
                  <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-semibold text-amber-700 dark:bg-amber-900/40 dark:text-amber-300">
                    {ba.pendingCount} pendiente{ba.pendingCount !== 1 ? "s" : ""}
                  </span>
                )}
              </div>
              <p className="mt-3 text-xs text-indigo-600 dark:text-indigo-400">Ver conciliación →</p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

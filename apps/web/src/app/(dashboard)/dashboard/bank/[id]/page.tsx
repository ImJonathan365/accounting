import { redirect, notFound } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { ReconciliationView } from "./_components/ReconciliationView";
import { ImportTransactionsButton } from "./_components/ImportTransactionsButton";

export const dynamic = "force-dynamic";

export default async function BankDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const { id } = await params;
  const canEdit = role === "owner" || role === "admin";

  try {
    const data = await apiClient.bank.getReconciliation(orgId, id, token);
    return (
      <div className="space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
              {data.bankAccount.name}
            </h1>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
              {data.bankAccount.bankName ?? "Banco"} · {data.bankAccount.linkedAccountCode} {data.bankAccount.linkedAccountName}
            </p>
          </div>
          {canEdit && <ImportTransactionsButton orgId={orgId} bankAccountId={id} token={token} />}
        </div>

        {/* Summary cards */}
        <div className="grid grid-cols-3 gap-4">
          {[
            { label: "Saldo banco", value: data.bankBalance },
            { label: "Diferencia", value: data.difference },
            { label: "Sin conciliar", value: data.unmatchedTransactions.length, isCnt: true },
          ].map(({ label, value, isCnt }) => (
            <div key={label} className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-800">
              <p className="text-xs text-slate-500 dark:text-slate-400">{label}</p>
              <p className={`mt-1 text-xl font-semibold tabular-nums ${
                isCnt
                  ? value === 0 ? "text-emerald-600 dark:text-emerald-400" : "text-amber-600 dark:text-amber-400"
                  : Math.abs(Number(value)) < 0.01 ? "text-emerald-600 dark:text-emerald-400" : value >= 0 ? "text-slate-900 dark:text-slate-100" : "text-red-600 dark:text-red-400"
              }`}>
                {isCnt ? value : Number(value).toLocaleString("es-GT", { minimumFractionDigits: 2 })}
              </p>
            </div>
          ))}
        </div>

        <ReconciliationView
          data={data}
          orgId={orgId}
          bankAccountId={id}
          token={token}
          canEdit={canEdit}
        />
      </div>
    );
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    return <PageError message="No se pudo cargar la conciliación." />;
  }
}

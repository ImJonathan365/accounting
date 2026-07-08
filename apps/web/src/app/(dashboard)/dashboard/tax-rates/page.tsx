import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { TaxRateList } from "./_components/TaxRateList";

export const dynamic = "force-dynamic";

export default async function TaxRatesPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit = role === "owner" || role === "admin";
  let taxRates, accounts;
  try {
    [taxRates, accounts] = await Promise.all([
      apiClient.taxRates.list(orgId, token),
      apiClient.accounts.list(orgId, token),
    ]);
  } catch {
    return <PageError message="No se pudieron cargar las tasas de impuesto." />;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Tasas de Impuesto</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">IVA y otros impuestos aplicables a las facturas.</p>
      </div>
      <TaxRateList taxRates={taxRates} accounts={accounts} orgId={orgId} token={token} canEdit={canEdit} />
    </div>
  );
}

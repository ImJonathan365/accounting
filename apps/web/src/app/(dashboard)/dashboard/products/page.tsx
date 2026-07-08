import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { ProductList } from "./_components/ProductList";

export const dynamic = "force-dynamic";

export default async function ProductsPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit = role === "owner" || role === "admin";
  let products, accounts, taxRates;
  try {
    [products, accounts, taxRates] = await Promise.all([
      apiClient.products.list(orgId, token),
      apiClient.accounts.list(orgId, token),
      apiClient.taxRates.list(orgId, token),
    ]);
  } catch {
    return <PageError message="No se pudo cargar el catálogo." />;
  }

  const postableAccounts = accounts.filter(a => a.isPostable);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Productos / Servicios</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Catálogo para agregar líneas a las facturas rápidamente.</p>
      </div>
      <ProductList
        products={products}
        accounts={postableAccounts}
        taxRates={taxRates}
        orgId={orgId}
        token={token}
        canEdit={canEdit}
      />
    </div>
  );
}

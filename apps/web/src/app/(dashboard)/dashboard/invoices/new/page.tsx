import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { CreateInvoiceForm } from "../_components/CreateInvoiceForm";

export const dynamic = "force-dynamic";

export default async function NewInvoicePage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  if (role !== "owner" && role !== "admin") redirect("/dashboard/invoices");

  const [contacts, accounts] = await Promise.all([
    apiClient.contacts.list(orgId, token),
    apiClient.accounts.list(orgId, token),
  ]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Nueva factura</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Crea una factura de cobro o pago.</p>
      </div>
      <CreateInvoiceForm orgId={orgId} token={token} contacts={contacts} accounts={accounts} />
    </div>
  );
}

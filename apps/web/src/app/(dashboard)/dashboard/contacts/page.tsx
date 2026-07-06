import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { ContactList } from "./_components/ContactList";

export const dynamic = "force-dynamic";

export default async function ContactsPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit = role === "owner" || role === "admin";

  let contacts;
  try {
    contacts = await apiClient.contacts.list(orgId, token);
  } catch {
    return <PageError message="No se pudo cargar los contactos." />;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Contactos</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Clientes y proveedores.</p>
      </div>
      <ContactList contacts={contacts} orgId={orgId} token={token} canEdit={canEdit} />
    </div>
  );
}

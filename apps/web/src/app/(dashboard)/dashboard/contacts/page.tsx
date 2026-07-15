import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { ContactList } from "./_components/ContactList";

export const dynamic = "force-dynamic";

export default async function ContactsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string; search?: string; type?: string }>;
}) {
  const [token, orgId, role, params] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(), searchParams,
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit   = role === "owner" || role === "admin";
  const page      = Math.max(1, Number(params.page ?? 1));
  const search    = params.search ?? undefined;
  const type      = params.type ?? undefined;

  let result;
  try {
    result = await apiClient.contacts.list(orgId, token, { page, pageSize: 25, search, type });
  } catch {
    return <PageError message="No se pudo cargar los contactos." />;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Contactos</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Clientes y proveedores.</p>
      </div>
      <ContactList
        contacts={result.items}
        totalPages={result.totalPages}
        currentPage={page}
        orgId={orgId}
        token={token}
        canEdit={canEdit}
      />
    </div>
  );
}

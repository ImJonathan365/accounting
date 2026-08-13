import { notFound, redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { ContactDetail } from "./_components/ContactDetail";

export const dynamic = "force-dynamic";

export default async function ContactDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const [token, orgId, role, { id }] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(), params,
  ]);
  if (!token || !orgId) redirect("/login");

  const canEdit = role === "owner" || role === "admin";

  let contact;
  try {
    contact = await apiClient.contacts.getById(orgId, id, token);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    throw err;
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6 py-2">
      <ContactDetail contact={contact} orgId={orgId} token={token} canEdit={canEdit} />
    </div>
  );
}

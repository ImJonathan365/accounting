import { redirect, notFound } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { EditRecurringForm } from "./_components/EditRecurringForm";

export const dynamic = "force-dynamic";

export default async function EditRecurringPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  if (role !== "owner" && role !== "admin") redirect("/dashboard/journal/recurring");

  const { id } = await params;

  try {
    const entry = await apiClient.recurring.getById(orgId, id, token);
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
            Editar plantilla recurrente
          </h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {entry.description}
          </p>
        </div>
        <EditRecurringForm entry={entry} orgId={orgId} token={token} />
      </div>
    );
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    redirect("/dashboard/journal/recurring");
  }
}

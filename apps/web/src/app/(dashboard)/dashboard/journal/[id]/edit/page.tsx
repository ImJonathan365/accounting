import { notFound, redirect } from "next/navigation";
import Link from "next/link";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { EditJournalForm } from "../../_components/EditJournalForm";

export const dynamic = "force-dynamic";

export default async function EditJournalEntryPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  let entry;
  try {
    entry = await apiClient.journal.get(orgId, id, token);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    throw err;
  }

  if (entry.status !== "Draft") redirect(`/dashboard/journal/${id}`);

  const accounts = await apiClient.accounts.list(orgId, token);
  const postableAccounts = accounts.filter((a) => a.isPostable && a.isActive);

  return (
    <>
      <div className="mb-6">
        <Link
          href={`/dashboard/journal/${id}`}
          className="mb-2 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-300"
        >
          ← Volver al asiento
        </Link>
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Editar borrador</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{entry.description}</p>
      </div>
      <EditJournalForm entry={entry} accounts={postableAccounts} />
    </>
  );
}

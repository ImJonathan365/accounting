import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { CreateJournalForm } from "../_components/CreateJournalForm";

export const dynamic = "force-dynamic";

export default async function NewJournalEntryPage() {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  if (!token || !orgId) redirect("/login");

  const accounts = await apiClient.accounts.list(orgId, token);
  const postable = accounts.filter((a) => a.isPostable && a.isActive);

  return (
    <>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Nuevo asiento</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Registra un asiento de doble entrada. Los débitos deben ser iguales a los créditos.
        </p>
      </div>
      <CreateJournalForm accounts={postable} />
    </>
  );
}

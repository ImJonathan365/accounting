import { redirect } from "next/navigation";
import { apiClient } from "@/lib/api-client";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { OrgSettingsForm } from "./_components/OrgSettingsForm";

export const dynamic = "force-dynamic";

export default async function SettingsPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const settings = await apiClient.settings.get(orgId, token);
  const canEdit  = role === "owner";

  return (
    <>
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Configuración de empresa</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Esta información aparecerá en todos los reportes exportados.
        </p>
      </div>
      {!canEdit && (
        <div className="mb-6 flex items-center gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800 dark:border-amber-800 dark:bg-amber-950/20 dark:text-amber-300">
          <svg className="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
          </svg>
          Solo el propietario puede editar la configuración de la empresa.
        </div>
      )}
      <OrgSettingsForm initialSettings={settings} canEdit={canEdit} />
    </>
  );
}

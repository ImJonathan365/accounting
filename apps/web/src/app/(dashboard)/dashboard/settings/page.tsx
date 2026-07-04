import { apiClient } from "@/lib/api-client";
import { getServerToken, getCurrentOrgId } from "@/lib/auth";
import { OrgSettingsForm } from "./_components/OrgSettingsForm";

export default async function SettingsPage() {
  const [token, orgId] = await Promise.all([getServerToken(), getCurrentOrgId()]);
  const settings = token && orgId ? await apiClient.settings.get(orgId, token) : null;

  return (
    <>
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Configuración de empresa</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Esta información aparecerá en todos los reportes exportados.
        </p>
      </div>
      <OrgSettingsForm initialSettings={settings} />
    </>
  );
}

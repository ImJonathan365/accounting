import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { OpeningBalanceForm } from "./_components/OpeningBalanceForm";

export const dynamic = "force-dynamic";

export default async function OpeningBalancesPage() {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");
  if (role !== "owner" && role !== "admin") redirect("/dashboard");

  let accounts;
  try {
    accounts = await apiClient.accounts.list(orgId, token);
  } catch {
    return <PageError message="No se pudo cargar el catálogo de cuentas." />;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Saldos iniciales</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Ingresa los saldos de apertura de cada cuenta. El asiento debe balancear (débitos = créditos).
          Se creará un asiento contable en el Diario.
        </p>
      </div>
      <div className="rounded-xl border border-amber-200 bg-amber-50 px-5 py-3 dark:border-amber-800/40 dark:bg-amber-950/20">
        <p className="text-sm text-amber-700 dark:text-amber-400">
          <strong>Nota:</strong> Este formulario crea un nuevo asiento cada vez. Si ya registraste saldos iniciales, ve al Diario a revisar o anular el asiento anterior antes de crear uno nuevo.
        </p>
      </div>
      <OpeningBalanceForm orgId={orgId} token={token} accounts={accounts} />
    </div>
  );
}

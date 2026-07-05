import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { TeamTable } from "./_components/TeamTable";
import { InviteForm } from "./_components/InviteForm";

export const dynamic = "force-dynamic";

export default async function TeamPage() {
  const [token, orgId, userId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId || !userId) redirect("/login");

  let members;
  try {
    members = await apiClient.members.list(orgId, token);
  } catch {
    return <PageError message="No se pudo cargar el equipo." />;
  }
  const currentUserRole = role ?? "member";
  const canInvite = currentUserRole === "owner" || currentUserRole === "admin";

  return (
    <>
      <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Equipo</h2>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {members.length} {members.length === 1 ? "miembro" : "miembros"}
          </p>
        </div>
        {canInvite && <InviteForm />}
      </div>

      <TeamTable
        members={members}
        currentUserId={userId}
        currentUserRole={currentUserRole}
      />

      <div className="mt-6 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800/50">
        <p className="text-xs text-slate-500 dark:text-slate-400">
          <strong className="text-slate-700 dark:text-slate-300">Propietario</strong> — acceso total, puede cambiar roles y eliminar miembros.{" "}
          <strong className="text-slate-700 dark:text-slate-300">Administrador</strong> — puede invitar y eliminar miembros.{" "}
          <strong className="text-slate-700 dark:text-slate-300">Miembro</strong> — acceso de lectura y escritura al diario y reportes.
        </p>
      </div>
    </>
  );
}

import { redirect } from "next/navigation";
import { getServerToken } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { ProfileForm } from "./_components/ProfileForm";

export const dynamic = "force-dynamic";

export default async function ProfilePage() {
  const token = await getServerToken();
  if (!token) redirect("/login");

  const profile = await apiClient.users.getProfile(token);

  const initials = `${profile.firstName[0]}${profile.lastName[0]}`.toUpperCase();
  const joined = new Date(profile.createdAtUtc).toLocaleDateString("es", {
    year: "numeric", month: "long", day: "numeric",
  });

  return (
    <>
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Mi perfil</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Gestiona tu información personal</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Avatar card */}
        <div className="rounded-xl border border-slate-200 bg-white p-6 text-center dark:border-slate-700 dark:bg-slate-800">
          <div className="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-indigo-100 text-2xl font-bold text-indigo-700 dark:bg-indigo-900 dark:text-indigo-300">
            {initials}
          </div>
          <h3 className="font-semibold text-slate-900 dark:text-slate-100">
            {profile.firstName} {profile.lastName}
          </h3>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{profile.email}</p>
          <div className="mt-4 border-t border-slate-100 pt-4 dark:border-slate-700">
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Estado</dt>
                <dd>
                  {profile.isActive ? (
                    <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700 dark:bg-green-900/50 dark:text-green-400">Activo</span>
                  ) : (
                    <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-700 dark:bg-red-900/50 dark:text-red-400">Inactivo</span>
                  )}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-slate-500 dark:text-slate-400">Miembro desde</dt>
                <dd className="text-right text-slate-700 dark:text-slate-300">{joined}</dd>
              </div>
            </dl>
          </div>
        </div>

        {/* Edit form */}
        <div className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800 lg:col-span-2">
          <h3 className="mb-6 text-base font-semibold text-slate-900 dark:text-slate-100">Información personal</h3>
          <ProfileForm profile={profile} />
        </div>
      </div>
    </>
  );
}

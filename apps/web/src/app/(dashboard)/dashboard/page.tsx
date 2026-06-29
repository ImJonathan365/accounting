import { getServerToken, getCurrentOrgId } from "@/lib/auth";

export const dynamic = "force-dynamic";
import { redirect } from "next/navigation";
import { logoutAction } from "@/lib/actions";

export default async function DashboardPage() {
  const token = await getServerToken();
  if (!token) redirect("/login");

  const orgId = await getCurrentOrgId();

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="border-b border-gray-200 bg-white px-6 py-4">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-semibold text-gray-900">Accounting</h1>
          <form action={logoutAction}>
            <button type="submit" className="text-sm text-gray-500 hover:text-gray-700">
              Cerrar sesión
            </button>
          </form>
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-6 py-8">
        <h2 className="text-2xl font-bold text-gray-900">Dashboard</h2>
        <p className="mt-2 text-gray-500">Organización: {orgId}</p>
        <p className="mt-4 text-gray-400 text-sm">Módulos en construcción...</p>
      </main>
    </div>
  );
}

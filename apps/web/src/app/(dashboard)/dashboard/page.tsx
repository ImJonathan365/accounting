import Link from "next/link";
import { getCurrentOrgId } from "@/lib/auth";

export const dynamic = "force-dynamic";

const modules = [
  {
    href: "/dashboard/accounts",
    title: "Catálogo de cuentas",
    description: "Administra el plan de cuentas de tu organización.",
    icon: "📒",
    disabled: false,
  },
  {
    href: null,
    title: "Diario contable",
    description: "Registra asientos de doble entrada. Próximamente.",
    icon: "📔",
    disabled: true,
  },
  {
    href: null,
    title: "Reportes",
    description: "Balance general, estado de resultados y más. Próximamente.",
    icon: "📊",
    disabled: true,
  },
];

const cardBase =
  "rounded-xl border p-5 transition-all";
const cardActive =
  "border-slate-200 bg-white shadow-sm hover:border-indigo-300 hover:shadow-md dark:border-slate-700 dark:bg-slate-800 dark:hover:border-indigo-500";
const cardDisabled =
  "cursor-not-allowed border-slate-200 bg-white opacity-60 dark:border-slate-700 dark:bg-slate-800";

export default async function DashboardPage() {
  const orgId = await getCurrentOrgId();

  return (
    <>
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Inicio</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          ID de organización:{" "}
          <code className="rounded bg-slate-100 px-1 py-0.5 text-xs dark:bg-slate-700">
            {orgId}
          </code>
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {modules.map((m) =>
          m.disabled ? (
            <div key={m.title} className={`${cardBase} ${cardDisabled}`}>
              <div className="mb-3 text-2xl">{m.icon}</div>
              <h3 className="font-semibold text-slate-900 dark:text-slate-100">{m.title}</h3>
              <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{m.description}</p>
            </div>
          ) : (
            <Link key={m.title} href={m.href!} className={`group ${cardBase} ${cardActive}`}>
              <div className="mb-3 text-2xl">{m.icon}</div>
              <h3 className="font-semibold text-slate-900 dark:text-slate-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                {m.title}
              </h3>
              <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{m.description}</p>
            </Link>
          )
        )}
      </div>
    </>
  );
}

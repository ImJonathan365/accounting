import Link from "next/link";

const REPORTS = [
  {
    href: "/dashboard/reports/balance-sheet",
    title: "Balance general",
    description: "Muestra activos, pasivos y capital al cierre de una fecha. Verifica la ecuación contable fundamental.",
    icon: "🏦",
  },
  {
    href: "/dashboard/reports/trial-balance",
    title: "Balance de comprobación",
    description: "Verifica que débitos y créditos cuadren. Muestra el saldo de todas las cuentas con movimientos en el período.",
    icon: "⚖️",
  },
  {
    href: "/dashboard/reports/income-statement",
    title: "Estado de resultados",
    description: "Compara ingresos contra gastos y calcula la utilidad o pérdida neta del período.",
    icon: "📈",
  },
];

export default function ReportsPage() {
  return (
    <>
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Reportes</h2>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Genera reportes financieros filtrados por período.
        </p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        {REPORTS.map((r) => (
          <Link
            key={r.href}
            href={r.href}
            className="group rounded-xl border border-slate-200 bg-white p-6 shadow-sm transition-all hover:border-indigo-300 hover:shadow-md dark:border-slate-700 dark:bg-slate-800 dark:hover:border-indigo-500"
          >
            <div className="mb-3 text-2xl">{r.icon}</div>
            <h3 className="font-semibold text-slate-900 group-hover:text-indigo-600 dark:text-slate-100 dark:group-hover:text-indigo-400 transition-colors">
              {r.title}
            </h3>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{r.description}</p>
          </Link>
        ))}
      </div>
    </>
  );
}

import Link from "next/link";

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50 text-center dark:bg-slate-900">
      <p className="text-6xl font-bold text-slate-200 dark:text-slate-700">404</p>
      <h1 className="text-xl font-semibold text-slate-800 dark:text-slate-200">Página no encontrada</h1>
      <p className="text-sm text-slate-500 dark:text-slate-400">
        La dirección que buscas no existe o fue movida.
      </p>
      <Link
        href="/dashboard"
        className="mt-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 transition-colors"
      >
        Ir al inicio
      </Link>
    </div>
  );
}

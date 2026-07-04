"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

interface Props {
  basePath: string;
  from: string;
  to: string;
}

export function DateRangeFilter({ basePath, from: initFrom, to: initTo }: Props) {
  const router = useRouter();
  const [from, setFrom] = useState(initFrom);
  const [to, setTo]     = useState(initTo);
  const [error, setError] = useState("");

  function apply(f: string, t: string) {
    if (f > t) { setError("La fecha de inicio no puede ser mayor que la fecha de fin."); return; }
    setError("");
    router.push(`${basePath}?from=${f}&to=${t}`);
  }

  function shortcut(f: string, t: string) {
    setFrom(f);
    setTo(t);
    apply(f, t);
  }

  const shortcuts = [
    { label: "Este mes",      ...thisMonth() },
    { label: "Mes anterior",  ...lastMonth() },
    { label: "Este año",      ...thisYear() },
    { label: "Año anterior",  ...lastYear() },
  ];

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500 dark:text-slate-400">Desde</label>
          <input
            type="date"
            value={from}
            onChange={(e) => { setFrom(e.target.value); setError(""); }}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 dark:focus:ring-indigo-900"
          />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500 dark:text-slate-400">Hasta</label>
          <input
            type="date"
            value={to}
            onChange={(e) => { setTo(e.target.value); setError(""); }}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 dark:focus:ring-indigo-900"
          />
        </div>
        <button
          onClick={() => apply(from, to)}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
        >
          Generar
        </button>
        <div className="flex flex-wrap gap-1.5">
          {shortcuts.map((s) => (
            <button
              key={s.label}
              type="button"
              onClick={() => shortcut(s.from, s.to)}
              className="rounded-md border border-slate-200 px-2.5 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700 transition-colors"
            >
              {s.label}
            </button>
          ))}
        </div>
      </div>
      {error && <p className="mt-2 text-xs text-red-600 dark:text-red-400">{error}</p>}
    </div>
  );
}

function pad(n: number) { return String(n).padStart(2, "0"); }
function fmt(y: number, m: number, d: number) { return `${y}-${pad(m)}-${pad(d)}`; }

function thisMonth()  { const n = new Date(); return { from: fmt(n.getFullYear(), n.getMonth()+1, 1), to: today() }; }
function lastMonth()  {
  const n = new Date(); n.setDate(1); n.setMonth(n.getMonth()-1);
  const last = new Date(n.getFullYear(), n.getMonth()+1, 0);
  return { from: fmt(n.getFullYear(), n.getMonth()+1, 1), to: fmt(last.getFullYear(), last.getMonth()+1, last.getDate()) };
}
function thisYear()   { const y = new Date().getFullYear(); return { from: fmt(y, 1, 1), to: today() }; }
function lastYear()   { const y = new Date().getFullYear()-1; return { from: fmt(y, 1, 1), to: fmt(y, 12, 31) }; }
function today()      { const n = new Date(); return fmt(n.getFullYear(), n.getMonth()+1, n.getDate()); }

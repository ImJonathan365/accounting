"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

interface Props {
  basePath: string;
  asOf: string;
}

export function DateFilter({ basePath, asOf: initAsOf }: Props) {
  const router  = useRouter();
  const [asOf, setAsOf] = useState(initAsOf);
  const [error, setError] = useState("");

  function apply(date: string) {
    if (date > today()) { setError("La fecha no puede ser futura."); return; }
    setError("");
    router.push(`${basePath}?asOf=${date}`);
  }

  const shortcuts = [
    { label: "Hoy",              date: today() },
    { label: "Fin de mes ant.",  date: endOfLastMonth() },
    { label: "Fin de año ant.",  date: endOfLastYear() },
  ];

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-slate-500 dark:text-slate-400">Al día</label>
          <input
            type="date"
            value={asOf}
            max={today()}
            onChange={(e) => { setAsOf(e.target.value); setError(""); }}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 dark:focus:ring-indigo-900"
          />
        </div>
        <button
          onClick={() => apply(asOf)}
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
        >
          Generar
        </button>
        <div className="flex flex-wrap gap-1.5">
          {shortcuts.map((s) => (
            <button
              key={s.label}
              type="button"
              onClick={() => { setAsOf(s.date); apply(s.date); }}
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
function today()        { const n = new Date(); return fmt(n.getFullYear(), n.getMonth()+1, n.getDate()); }
function endOfLastMonth() {
  const n = new Date(); n.setDate(1); n.setDate(0);
  return fmt(n.getFullYear(), n.getMonth()+1, n.getDate());
}
function endOfLastYear() { return fmt(new Date().getFullYear()-1, 12, 31); }

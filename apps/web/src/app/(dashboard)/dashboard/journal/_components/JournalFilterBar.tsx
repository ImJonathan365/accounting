"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import type { JournalStatus } from "@accounting/types";

const STATUS_OPTIONS: { value: JournalStatus | ""; label: string }[] = [
  { value: "",       label: "Todos" },
  { value: "Posted", label: "Registrado" },
  { value: "Voided", label: "Anulado" },
  { value: "Draft",  label: "Borrador" },
];

export function JournalFilterBar() {
  const router      = useRouter();
  const searchParams = useSearchParams();

  const [from,   setFrom]   = useState(searchParams.get("from")   ?? "");
  const [to,     setTo]     = useState(searchParams.get("to")     ?? "");
  const [status, setStatus] = useState(searchParams.get("status") ?? "");
  const [search, setSearch] = useState(searchParams.get("search") ?? "");

  const hasFilters = from || to || status || search;

  function buildUrl(overrides?: { from?: string; to?: string; status?: string; search?: string }) {
    const f = overrides?.from   ?? from;
    const t = overrides?.to     ?? to;
    const s = overrides?.status ?? status;
    const q = overrides?.search ?? search;
    const params = new URLSearchParams();
    if (f)        params.set("from",   f);
    if (t)        params.set("to",     t);
    if (s)        params.set("status", s);
    if (q.trim()) params.set("search", q.trim());
    const qs = params.toString();
    return `/dashboard/journal${qs ? `?${qs}` : ""}`;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    router.push(buildUrl());
  }

  function handleClear() {
    setFrom(""); setTo(""); setStatus(""); setSearch("");
    router.push("/dashboard/journal");
  }

  const inputClass =
    "rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  return (
    <form onSubmit={handleSubmit} className="flex flex-wrap items-end gap-2">
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Desde</label>
        <input
          type="date"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          className={inputClass}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Hasta</label>
        <input
          type="date"
          value={to}
          min={from || undefined}
          onChange={(e) => setTo(e.target.value)}
          className={inputClass}
        />
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Estado</label>
        <select
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          className={inputClass}
        >
          {STATUS_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Buscar</label>
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Descripción o referencia…"
          className={`${inputClass} w-52`}
        />
      </div>

      <button
        type="submit"
        className="rounded-lg bg-indigo-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 transition-colors"
      >
        Filtrar
      </button>

      {hasFilters && (
        <button
          type="button"
          onClick={handleClear}
          className="rounded-lg border border-slate-200 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-700 transition-colors"
        >
          Limpiar
        </button>
      )}
    </form>
  );
}

"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import type { Account } from "@accounting/types";

interface Props {
  accounts: Account[];
  current: { accountId: string; from: string; to: string };
}

export function LedgerFilterForm({ accounts, current }: Props) {
  const router = useRouter();
  const [accountId, setAccountId] = useState(current.accountId);
  const [from,      setFrom]      = useState(current.from);
  const [to,        setTo]        = useState(current.to);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!accountId) return;
    const params = new URLSearchParams({ accountId, from, to });
    router.push(`/dashboard/reports/ledger?${params}`);
  }

  const inputClass =
    "rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  return (
    <form onSubmit={handleSubmit} className="flex flex-wrap items-end gap-3">
      <div className="flex flex-col gap-1 min-w-56">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Cuenta</label>
        <select
          value={accountId}
          onChange={(e) => setAccountId(e.target.value)}
          required
          className={inputClass}
        >
          <option value="">Selecciona una cuenta…</option>
          {accounts.map((a) => (
            <option key={a.id} value={a.id}>
              {a.code} — {a.name}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Desde</label>
        <input
          type="date"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          required
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
          required
          className={inputClass}
        />
      </div>

      <button
        type="submit"
        className="rounded-lg bg-indigo-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 transition-colors"
      >
        Consultar
      </button>
    </form>
  );
}

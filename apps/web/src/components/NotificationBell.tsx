"use client";

import Link from "next/link";
import { useState } from "react";
import type { OverdueInvoice } from "@accounting/types";

interface Props { overdueInvoices: OverdueInvoice[]; }

function daysOverdue(dueDate: string) {
  const due  = new Date(dueDate);
  const now  = new Date();
  const diff = Math.floor((now.getTime() - due.getTime()) / 86400000);
  return diff;
}

export function NotificationBell({ overdueInvoices }: Props) {
  const [open, setOpen] = useState(false);
  const count = overdueInvoices.length;

  if (count === 0) return null;

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(v => !v)}
        className="relative flex h-8 w-8 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-slate-700 dark:hover:text-slate-200"
        aria-label={`${count} facturas vencidas`}
      >
        <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 0 0 5.454-1.31A8.967 8.967 0 0 1 18 9.75V9A6 6 0 0 0 6 9v.75a8.967 8.967 0 0 1-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 0 1-5.714 0m5.714 0a3 3 0 1 1-5.714 0" />
        </svg>
        <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white">
          {count > 9 ? "9+" : count}
        </span>
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-10" onClick={() => setOpen(false)} />
          <div className="absolute right-0 top-full z-20 mt-2 w-80 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl dark:border-slate-700 dark:bg-slate-800">
            <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3 dark:border-slate-700">
              <p className="text-sm font-semibold text-red-600 dark:text-red-400">
                {count} factura{count !== 1 ? "s" : ""} vencida{count !== 1 ? "s" : ""}
              </p>
              <Link
                href="/dashboard/invoices"
                onClick={() => setOpen(false)}
                className="text-xs text-indigo-600 hover:underline dark:text-indigo-400"
              >
                Ver todas →
              </Link>
            </div>
            <ul className="max-h-72 overflow-y-auto divide-y divide-slate-50 dark:divide-slate-700/50">
              {overdueInvoices.slice(0, 10).map(inv => {
                const days = daysOverdue(inv.dueDate);
                return (
                  <li key={inv.id}>
                    <Link
                      href={`/dashboard/invoices/${inv.id}`}
                      onClick={() => setOpen(false)}
                      className="flex items-center justify-between gap-3 px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-700/40"
                    >
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-slate-800 dark:text-slate-200">{inv.number}</p>
                        <p className="truncate text-xs text-slate-500">{inv.contactName}</p>
                      </div>
                      <div className="shrink-0 text-right">
                        <p className="text-sm font-semibold tabular-nums text-slate-700 dark:text-slate-300">
                          {inv.balance.toFixed(2)}
                        </p>
                        <p className="text-xs text-red-500">{days}d vencida</p>
                      </div>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        </>
      )}
    </div>
  );
}

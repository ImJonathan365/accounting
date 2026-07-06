"use client";

import { useState, useTransition } from "react";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { apiClient } from "@/lib/api-client";
import type { Period } from "@accounting/types";

interface Props {
  periods: Period[];
  year:    number;
  orgId:   string;
  role:    string;
  token:   string;
}

const today = new Date();

export function PeriodGrid({ periods, year, orgId, role, token }: Props) {
  const router            = useRouter();
  const [pending, startT] = useTransition();
  const [loadingKey, setLoadingKey] = useState<string | null>(null);

  const canClose  = role === "owner" || role === "admin";
  const canReopen = role === "owner";

  async function handleClose(month: number) {
    const key = `close-${month}`;
    setLoadingKey(key);
    startT(async () => {
      try {
        await apiClient.periods.close(orgId, { year, month }, token);
        toast.success(`Período cerrado correctamente.`);
        router.refresh();
      } catch (err: unknown) {
        toast.error(err instanceof Error ? err.message : "Error al cerrar el período.");
      } finally {
        setLoadingKey(null);
      }
    });
  }

  async function handleReopen(month: number) {
    const key = `reopen-${month}`;
    setLoadingKey(key);
    startT(async () => {
      try {
        await apiClient.periods.reopen(orgId, year, month, token);
        toast.success(`Período reabierto correctamente.`);
        router.refresh();
      } catch (err: unknown) {
        toast.error(err instanceof Error ? err.message : "Error al reabrir el período.");
      } finally {
        setLoadingKey(null);
      }
    });
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {periods.map(p => {
        const isPast   = year < today.getFullYear() || (year === today.getFullYear() && p.month < today.getMonth() + 1);
        const isCurrent = year === today.getFullYear() && p.month === today.getMonth() + 1;
        const isFuture  = year > today.getFullYear() || (year === today.getFullYear() && p.month > today.getMonth() + 1);
        const closingKey = `close-${p.month}`;
        const reopenKey  = `reopen-${p.month}`;
        const isLoading  = loadingKey === closingKey || loadingKey === reopenKey;

        return (
          <div
            key={p.month}
            className={[
              "rounded-xl border p-5 shadow-sm transition-colors",
              p.isClosed
                ? "border-slate-300 bg-slate-50 dark:border-slate-600 dark:bg-slate-800/60"
                : isCurrent
                  ? "border-indigo-300 bg-indigo-50 dark:border-indigo-700 dark:bg-indigo-950/30"
                  : "border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800",
            ].join(" ")}
          >
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold capitalize text-slate-900 dark:text-slate-100">
                  {p.monthName}
                </p>
                <p className="text-xs text-slate-500 dark:text-slate-400">{year}</p>
              </div>
              <StatusBadge isClosed={p.isClosed} isCurrent={isCurrent} isFuture={isFuture} />
            </div>

            {p.isClosed && p.closedByName && (
              <p className="mt-3 text-xs text-slate-500 dark:text-slate-400">
                Cerrado por <span className="font-medium">{p.closedByName}</span>
                {p.closedAtUtc && (
                  <> · {new Date(p.closedAtUtc).toLocaleDateString("es-ES", { day: "2-digit", month: "short", year: "numeric" })}</>
                )}
              </p>
            )}

            <div className="mt-4">
              {p.isClosed ? (
                canReopen && (
                  <button
                    onClick={() => handleReopen(p.month)}
                    disabled={isLoading || pending}
                    className="w-full rounded-lg border border-amber-200 bg-amber-50 px-3 py-1.5 text-xs font-medium text-amber-700 hover:bg-amber-100 disabled:opacity-50 dark:border-amber-700 dark:bg-amber-950/40 dark:text-amber-400 dark:hover:bg-amber-900/40 transition-colors"
                  >
                    {isLoading ? "Reabriendo…" : "Reabrir período"}
                  </button>
                )
              ) : (
                canClose && !isFuture && (
                  <button
                    onClick={() => handleClose(p.month)}
                    disabled={isLoading || pending}
                    className="w-full rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600 transition-colors"
                  >
                    {isLoading ? "Cerrando…" : "Cerrar período"}
                  </button>
                )
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

function StatusBadge({ isClosed, isCurrent, isFuture }: { isClosed: boolean; isCurrent: boolean; isFuture: boolean }) {
  if (isClosed)  return <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs font-medium text-slate-600 dark:bg-slate-700 dark:text-slate-300">Cerrado</span>;
  if (isFuture)  return <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-400 dark:bg-slate-800 dark:text-slate-500">Futuro</span>;
  if (isCurrent) return <span className="rounded-full bg-indigo-100 px-2 py-0.5 text-xs font-medium text-indigo-700 dark:bg-indigo-900/50 dark:text-indigo-300">Actual</span>;
  return          <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700 dark:bg-emerald-900/50 dark:text-emerald-300">Abierto</span>;
}

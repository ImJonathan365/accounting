"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Budget } from "@accounting/types";

interface Props { budgets: Budget[]; orgId: string; token: string; canEdit: boolean; }

export function BudgetList({ budgets, orgId, token, canEdit }: Props) {
  const router = useRouter();
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [isDeleting, startDeleteTransition]   = useTransition();

  function handleDelete(e: React.MouseEvent, id: string) {
    e.preventDefault();
    startDeleteTransition(async () => {
      try {
        await apiClient.budgets.delete(orgId, id, token);
        toast.success("Presupuesto eliminado.");
        setConfirmDeleteId(null);
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error al eliminar.");
        setConfirmDeleteId(null);
      }
    });
  }

  if (budgets.length === 0) {
    return (
      <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
        <p className="text-sm text-slate-500">No hay presupuestos. Crea uno para empezar.</p>
      </div>
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {budgets.map(b => (
        <div
          key={b.id}
          className="group rounded-xl border border-slate-200 bg-white shadow-sm transition-all hover:border-indigo-300 hover:shadow-md dark:border-slate-700 dark:bg-slate-800"
        >
          <Link href={`/dashboard/budgets/${b.id}`} className="block p-5">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-semibold text-slate-900 group-hover:text-indigo-700 dark:text-slate-100 dark:group-hover:text-indigo-300">
                  {b.name}
                </p>
                <p className="mt-0.5 text-sm text-slate-400">Año {b.year}</p>
                <p className="mt-1 text-xs text-slate-400">{b.lines.length} línea{b.lines.length !== 1 ? "s" : ""}</p>
              </div>
              {b.isActive && (
                <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">
                  Activo
                </span>
              )}
            </div>
            <p className="mt-3 text-xs text-indigo-600 dark:text-indigo-400">Ver detalle →</p>
          </Link>

          {canEdit && (
            <div className="border-t border-slate-100 px-5 py-2.5 dark:border-slate-700">
              {confirmDeleteId === b.id ? (
                <div className="flex items-center gap-2">
                  <span className="text-xs text-slate-600 dark:text-slate-400">¿Eliminar?</span>
                  <button
                    onClick={e => handleDelete(e, b.id)}
                    disabled={isDeleting}
                    className="rounded px-2 py-1 text-xs font-semibold text-white bg-red-600 hover:bg-red-700 disabled:opacity-50"
                  >
                    Sí
                  </button>
                  <button
                    onClick={e => { e.preventDefault(); setConfirmDeleteId(null); }}
                    disabled={isDeleting}
                    className="rounded px-2 py-1 text-xs text-slate-600 border border-slate-300 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300"
                  >
                    No
                  </button>
                </div>
              ) : (
                <button
                  onClick={e => { e.preventDefault(); setConfirmDeleteId(b.id); }}
                  className="text-xs font-medium text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
                >
                  Eliminar
                </button>
              )}
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

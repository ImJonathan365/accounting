"use client";

import { useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import Link from "next/link";
import { apiClient, ApiError } from "@/lib/api-client";
import type { RecurringEntry } from "@accounting/types";

interface Props {
  entries:   RecurringEntry[];
  orgId:     string;
  token:     string;
  canEdit:   boolean;
  freqColor: Record<string, string>;
}

function GenerateButton({ orgId, token, pending }: { orgId: string; token: string; pending: number }) {
  const router                       = useRouter();
  const [isPending, startTransition] = useTransition();

  function handleGenerate() {
    startTransition(async () => {
      try {
        const result = await apiClient.recurring.generatePending(orgId, token);
        toast.success(`${result.generated} asiento${result.generated !== 1 ? "s" : ""} generado${result.generated !== 1 ? "s" : ""} como borrador.`);
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error al generar asientos.");
      }
    });
  }

  return (
    <button
      onClick={handleGenerate}
      disabled={isPending}
      className="relative rounded-lg border border-amber-400 bg-amber-50 px-4 py-2 text-sm font-semibold text-amber-700 transition-colors hover:bg-amber-100 disabled:opacity-50 dark:border-amber-600 dark:bg-amber-900/30 dark:text-amber-300"
    >
      {isPending ? "Generando…" : `Generar pendientes (${pending})`}
    </button>
  );
}

function DeleteButton({ orgId, token, id, description }: { orgId: string; token: string; id: string; description: string }) {
  const router                       = useRouter();
  const [isPending, startTransition] = useTransition();

  function handleDelete() {
    if (!confirm(`¿Eliminar la plantilla "${description}"?`)) return;
    startTransition(async () => {
      try {
        await apiClient.recurring.delete(orgId, id, token);
        toast.success("Plantilla eliminada.");
        router.refresh();
      } catch {
        toast.error("Error al eliminar la plantilla.");
      }
    });
  }

  return (
    <button
      onClick={handleDelete}
      disabled={isPending}
      className="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-50 dark:hover:bg-red-950/30 dark:hover:text-red-400"
      title="Eliminar"
    >
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
      </svg>
    </button>
  );
}

export function RecurringList({ entries, orgId, token, canEdit, freqColor }: Props) {
  const today = new Date().toISOString().slice(0, 10);

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
        <thead>
          <tr className="bg-slate-50 dark:bg-slate-700/50">
            <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Descripción</th>
            <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Frecuencia</th>
            <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Próxima fecha</th>
            <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Vence</th>
            <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">Estado</th>
            {canEdit && <th className="px-4 py-3" />}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30">
          {entries.map((e) => {
            const isPending = e.isActive && e.nextDate <= today;
            return (
              <tr key={e.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                <td className="px-4 py-3">
                  <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{e.description}</p>
                  {e.reference && <p className="text-xs text-slate-400">{e.reference}</p>}
                  <p className="mt-0.5 text-xs text-slate-400">{e.lines.length} línea{e.lines.length !== 1 ? "s" : ""}</p>
                </td>
                <td className="px-4 py-3">
                  <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${freqColor[e.frequency] ?? ""}`}>
                    {e.frequencyLabel}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <span className={`font-mono text-sm ${isPending ? "font-semibold text-amber-600 dark:text-amber-400" : "text-slate-700 dark:text-slate-300"}`}>
                    {e.nextDate}
                    {isPending && " ⚠"}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-slate-500 dark:text-slate-400">
                  {e.endDate ?? "—"}
                </td>
                <td className="px-4 py-3">
                  <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                    e.isActive
                      ? "bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300"
                      : "bg-slate-100 text-slate-500 dark:bg-slate-700 dark:text-slate-400"
                  }`}>
                    {e.isActive ? "Activo" : "Inactivo"}
                  </span>
                </td>
                {canEdit && (
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1">
                      <Link
                        href={`/dashboard/journal/recurring/${e.id}`}
                        className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-700"
                        title="Editar"
                      >
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931zm0 0L19.5 7.125" />
                        </svg>
                      </Link>
                      <DeleteButton orgId={orgId} token={token} id={e.id} description={e.description} />
                    </div>
                  </td>
                )}
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export { GenerateButton };

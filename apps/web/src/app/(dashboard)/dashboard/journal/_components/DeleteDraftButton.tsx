"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { deleteJournalEntryAction } from "@/lib/actions";

export function DeleteDraftButton({ entryId }: { entryId: string }) {
  const [confirm, setConfirm] = useState(false);
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  async function handleDelete() {
    setLoading(true);
    try {
      await deleteJournalEntryAction(entryId);
      router.push("/dashboard/journal");
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Error al eliminar el borrador.");
      setConfirm(false);
    } finally {
      setLoading(false);
    }
  }

  if (confirm) {
    return (
      <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-3 py-2 dark:border-red-800 dark:bg-red-950/20">
        <span className="text-sm text-red-700 dark:text-red-400">¿Eliminar borrador?</span>
        <button
          onClick={handleDelete}
          disabled={loading}
          className="rounded-md bg-red-600 px-3 py-1 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
        >
          {loading ? "Eliminando…" : "Confirmar"}
        </button>
        <button
          onClick={() => setConfirm(false)}
          disabled={loading}
          className="rounded-md px-2 py-1 text-xs text-slate-500 hover:text-slate-700 disabled:opacity-50"
        >
          Cancelar
        </button>
      </div>
    );
  }

  return (
    <button
      onClick={() => setConfirm(true)}
      className="flex items-center gap-1.5 rounded-lg border border-red-200 px-3 py-2 text-sm font-medium text-red-600 hover:bg-red-50 dark:border-red-800 dark:text-red-400 dark:hover:bg-red-950/20 transition-colors"
    >
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
      </svg>
      Eliminar borrador
    </button>
  );
}

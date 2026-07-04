"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { voidJournalEntryAction } from "@/lib/actions";

interface Props {
  entryId: string;
  description: string;
}

export function VoidModal({ entryId, description }: Props) {
  const [open,    setOpen]    = useState(false);
  const [reason,  setReason]  = useState("");
  const [voidDate, setVoidDate] = useState(() => {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
  });
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState<string | null>(null);
  const router = useRouter();
  const backdropRef = useRef<HTMLDivElement>(null);

  function handleBackdrop(e: React.MouseEvent) {
    if (e.target === backdropRef.current) setOpen(false);
  }

  async function handleConfirm() {
    setLoading(true);
    setError(null);
    try {
      await voidJournalEntryAction(entryId, {
        reason:   reason.trim() || null,
        voidDate: voidDate || null,
      });
      setOpen(false);
      router.refresh();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Error al anular el asiento");
    } finally {
      setLoading(false);
    }
  }

  const today = new Date().toISOString().split("T")[0];

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="rounded-lg border border-red-200 bg-red-50 px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-100 dark:border-red-800 dark:bg-red-950/30 dark:text-red-400 dark:hover:bg-red-950/50 transition-colors"
      >
        Anular asiento
      </button>

      {open && (
        <div
          ref={backdropRef}
          onClick={handleBackdrop}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm p-4"
        >
          <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white shadow-xl dark:border-slate-700 dark:bg-slate-800">
            {/* Header */}
            <div className="border-b border-slate-100 px-6 py-4 dark:border-slate-700">
              <div className="flex items-center gap-3">
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-red-100 dark:bg-red-950/50">
                  <svg className="h-5 w-5 text-red-600 dark:text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                  </svg>
                </div>
                <div>
                  <h3 className="font-semibold text-slate-900 dark:text-slate-100">Anular asiento</h3>
                  <p className="text-xs text-slate-500 dark:text-slate-400">Esta acción no se puede deshacer</p>
                </div>
              </div>
            </div>

            {/* Body */}
            <div className="px-6 py-5 space-y-4">
              <div className="rounded-lg bg-amber-50 border border-amber-200 px-4 py-3 dark:bg-amber-950/20 dark:border-amber-800">
                <p className="text-sm text-amber-800 dark:text-amber-300">
                  Se creará un <strong>contra-asiento</strong> que revierte todos los movimientos de:
                </p>
                <p className="mt-1 text-sm font-medium text-amber-900 dark:text-amber-200 truncate">
                  &ldquo;{description}&rdquo;
                </p>
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
                  Fecha de anulación
                </label>
                <input
                  type="date"
                  value={voidDate}
                  max={today}
                  onChange={(e) => setVoidDate(e.target.value)}
                  className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
                  Razón de la anulación <span className="font-normal text-slate-400">(opcional)</span>
                </label>
                <textarea
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  maxLength={500}
                  rows={3}
                  placeholder="Ej: Error en monto, cuenta incorrecta…"
                  className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 resize-none"
                />
                <p className="mt-1 text-right text-xs text-slate-400">{reason.length}/500</p>
              </div>

              {error && (
                <p className="rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600 dark:bg-red-950/30 dark:text-red-400">
                  {error}
                </p>
              )}
            </div>

            {/* Footer */}
            <div className="flex justify-end gap-3 border-t border-slate-100 px-6 py-4 dark:border-slate-700">
              <button
                onClick={() => { setOpen(false); setError(null); }}
                disabled={loading}
                className="rounded-lg px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-50 dark:text-slate-400 dark:hover:bg-slate-700 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleConfirm}
                disabled={loading}
                className="flex items-center gap-2 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
              >
                {loading && (
                  <span className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
                )}
                {loading ? "Anulando…" : "Confirmar anulación"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

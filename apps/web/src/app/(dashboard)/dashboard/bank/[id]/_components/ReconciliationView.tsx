"use client";

import { useTransition, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { BankReconciliation, BankTransaction, UnmatchedJournal } from "@accounting/types";

interface Props {
  data:          BankReconciliation;
  orgId:         string;
  bankAccountId: string;
  token:         string;
  canEdit:       boolean;
  currentPage:   number;
}

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export function ReconciliationView({ data, orgId, bankAccountId, token, canEdit, currentPage }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [selectedTx,      setSelectedTx]      = useState<string | null>(null);
  const [selectedJournal, setSelectedJournal] = useState<string | null>(null);

  const { items: pendingTxs, totalPages } = data.unmatchedTransactions;

  function doAction(action: "match" | "exclude" | "unmatch", txId: string, journalId?: string) {
    startTransition(async () => {
      try {
        if (action === "match" && journalId) {
          await apiClient.bank.match(orgId, bankAccountId, txId, journalId, token);
          toast.success("Transacción conciliada.");
          setSelectedTx(null); setSelectedJournal(null);
        } else if (action === "exclude") {
          await apiClient.bank.exclude(orgId, bankAccountId, txId, token);
          toast.success("Transacción excluida.");
        } else if (action === "unmatch") {
          await apiClient.bank.unmatch(orgId, bankAccountId, txId, token);
          toast.success("Conciliación revertida.");
        }
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error al procesar.");
      }
    });
  }

  const statusBadge = (tx: BankTransaction) => {
    if (tx.status === "Matched")  return <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">Conciliado</span>;
    if (tx.status === "Excluded") return <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-500 dark:bg-slate-700 dark:text-slate-400">Excluido</span>;
    return <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700 dark:bg-amber-900/40 dark:text-amber-300">Pendiente</span>;
  };

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      {/* Bank transactions */}
      <div className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Movimientos del banco</h2>
        {pendingTxs.length === 0 ? (
          <p className="rounded-xl border border-slate-200 bg-white py-8 text-center text-sm text-slate-400 dark:border-slate-700 dark:bg-slate-800">
            No hay movimientos pendientes
          </p>
        ) : (
          <div className="space-y-2">
            {pendingTxs.map((tx) => (
              <div
                key={tx.id}
                onClick={() => canEdit && setSelectedTx(selectedTx === tx.id ? null : tx.id)}
                className={`cursor-pointer rounded-xl border p-3 transition-all ${
                  selectedTx === tx.id
                    ? "border-indigo-400 bg-indigo-50 dark:border-indigo-600 dark:bg-indigo-950/30"
                    : "border-slate-200 bg-white hover:border-slate-300 dark:border-slate-700 dark:bg-slate-800"
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-slate-900 dark:text-slate-100">{tx.description}</p>
                    <p className="text-xs text-slate-400">{tx.date}</p>
                  </div>
                  <div className="text-right">
                    <p className={`text-sm font-semibold tabular-nums ${tx.type === "Credit" ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                      {tx.type === "Credit" ? "+" : "-"}{fmt(tx.amount)}
                    </p>
                    {statusBadge(tx)}
                  </div>
                </div>
                {selectedTx === tx.id && canEdit && (
                  <div className="mt-3 flex gap-2 border-t border-slate-100 pt-3 dark:border-slate-700">
                    {selectedJournal && (
                      <button
                        onClick={(e) => { e.stopPropagation(); doAction("match", tx.id, selectedJournal); }}
                        disabled={isPending}
                        className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
                      >
                        Conciliar con seleccionado
                      </button>
                    )}
                    <button
                      onClick={(e) => { e.stopPropagation(); doAction("exclude", tx.id); }}
                      disabled={isPending}
                      className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400"
                    >
                      Excluir
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
        {totalPages > 1 && (
          <div className="flex items-center justify-between pt-1">
            <a
              href={`?page=${currentPage - 1}`}
              className={`rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-700 ${currentPage <= 1 ? "pointer-events-none opacity-40" : ""}`}
            >
              Anterior
            </a>
            <span className="text-xs text-slate-400">{currentPage} / {totalPages}</span>
            <a
              href={`?page=${currentPage + 1}`}
              className={`rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-700 ${currentPage >= totalPages ? "pointer-events-none opacity-40" : ""}`}
            >
              Siguiente
            </a>
          </div>
        )}
      </div>

      {/* Unmatched journal entries */}
      <div className="space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Asientos sin conciliar</h2>
        {data.unmatchedJournalEntries.length === 0 ? (
          <p className="rounded-xl border border-slate-200 bg-white py-8 text-center text-sm text-slate-400 dark:border-slate-700 dark:bg-slate-800">
            Todos los asientos están conciliados
          </p>
        ) : (
          <div className="space-y-2">
            {data.unmatchedJournalEntries.map((j: UnmatchedJournal) => (
              <div
                key={j.journalEntryId}
                onClick={() => canEdit && setSelectedJournal(selectedJournal === j.journalEntryId ? null : j.journalEntryId)}
                className={`cursor-pointer rounded-xl border p-3 transition-all ${
                  selectedJournal === j.journalEntryId
                    ? "border-indigo-400 bg-indigo-50 dark:border-indigo-600 dark:bg-indigo-950/30"
                    : "border-slate-200 bg-white hover:border-slate-300 dark:border-slate-700 dark:bg-slate-800"
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-slate-900 dark:text-slate-100">{j.description}</p>
                    <p className="text-xs text-slate-400">{j.date}{j.reference ? ` · ${j.reference}` : ""}</p>
                  </div>
                  <p className={`text-sm font-semibold tabular-nums ${j.amount >= 0 ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                    {j.amount >= 0 ? "+" : ""}{fmt(j.amount)}
                  </p>
                </div>
              </div>
            ))}
          </div>
        )}
        {selectedTx && selectedJournal && (
          <p className="rounded-lg border border-indigo-200 bg-indigo-50 px-3 py-2 text-xs text-indigo-700 dark:border-indigo-800 dark:bg-indigo-950/30 dark:text-indigo-300">
            Selecciona "Conciliar con seleccionado" en el movimiento bancario para vincularlo.
          </p>
        )}
      </div>
    </div>
  );
}

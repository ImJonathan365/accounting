"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Invoice, Account } from "@accounting/types";

interface Props { invoice: Invoice; orgId: string; token: string; paymentAccounts: Account[]; }

export function InvoiceActions({ invoice, orgId, token, paymentAccounts }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [showPayment, setShowPayment] = useState(false);
  const [payDate, setPayDate]         = useState(new Date().toISOString().slice(0, 10));
  const [payAmount, setPayAmount]     = useState(String(invoice.balance.toFixed(2)));
  const [payAccount, setPayAccount]   = useState(paymentAccounts[0]?.id ?? "");
  const [payNotes, setPayNotes]       = useState("");
  const [error, setError]             = useState<string | null>(null);

  const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  function issue() {
    if (!confirm("¿Emitir esta factura? Se creará el asiento contable.")) return;
    startTransition(async () => {
      try {
        await apiClient.invoices.issue(orgId, invoice.id, token);
        toast.success("Factura emitida.");
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  function voidInvoice() {
    if (!confirm("¿Anular esta factura?")) return;
    startTransition(async () => {
      try {
        await apiClient.invoices.void(orgId, invoice.id, token);
        toast.success("Factura anulada.");
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  function submitPayment(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!payAccount) { setError("Selecciona la cuenta de pago."); return; }
    const amount = parseFloat(payAmount);
    if (!amount || amount <= 0) { setError("Monto inválido."); return; }

    startTransition(async () => {
      try {
        await apiClient.invoices.recordPayment(orgId, invoice.id, {
          date: payDate, amount, paymentAccountId: payAccount, notes: payNotes || undefined,
        }, token);
        toast.success("Pago registrado.");
        setShowPayment(false);
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  return (
    <div className="flex flex-col gap-2 items-end">
      <div className="flex gap-2">
        {invoice.status === "Draft" && (
          <button onClick={issue} disabled={isPending} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            Emitir factura
          </button>
        )}
        {(invoice.status === "Issued" || invoice.status === "PartiallyPaid") && (
          <button onClick={() => setShowPayment(v => !v)} className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700">
            Registrar pago
          </button>
        )}
        {invoice.status !== "Void" && invoice.status !== "Paid" && (
          <button onClick={voidInvoice} disabled={isPending} className="rounded-lg border border-red-300 px-4 py-2 text-sm font-medium text-red-600 hover:bg-red-50 dark:border-red-700 dark:text-red-400 dark:hover:bg-red-950/30">
            Anular
          </button>
        )}
      </div>

      {showPayment && (
        <form onSubmit={submitPayment} className="w-80 rounded-xl border border-slate-200 bg-white p-4 shadow-lg dark:border-slate-700 dark:bg-slate-800 space-y-3">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Registrar pago</h3>
          {error && <p className="text-xs text-red-600 dark:text-red-400">{error}</p>}
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-600 dark:text-slate-400">Fecha</label>
            <input type="date" value={payDate} onChange={e => setPayDate(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-600 dark:text-slate-400">Monto</label>
            <input type="number" step="0.01" value={payAmount} onChange={e => setPayAmount(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-600 dark:text-slate-400">Cuenta de pago</label>
            <select value={payAccount} onChange={e => setPayAccount(e.target.value)} className={inputClass}>
              <option value="">Selecciona…</option>
              {paymentAccounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-slate-600 dark:text-slate-400">Notas</label>
            <input value={payNotes} onChange={e => setPayNotes(e.target.value)} className={inputClass} placeholder="Opcional" />
          </div>
          <div className="flex gap-2">
            <button type="submit" disabled={isPending} className="flex-1 rounded-lg bg-emerald-600 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-50">
              {isPending ? "Guardando…" : "Guardar"}
            </button>
            <button type="button" onClick={() => setShowPayment(false)} className="flex-1 rounded-lg border border-slate-300 py-2 text-sm text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400">
              Cancelar
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

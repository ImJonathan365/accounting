"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Account, Contact, InvoiceType, CreateInvoiceLineRequest } from "@accounting/types";

interface LineState { description: string; quantity: string; unitPrice: string; accountId: string; }
function today() { return new Date().toISOString().slice(0, 10); }
function inDays(n: number) { const d = new Date(); d.setDate(d.getDate() + n); return d.toISOString().slice(0, 10); }

interface Props { orgId: string; token: string; contacts: Contact[]; accounts: Account[]; }

export function CreateInvoiceForm({ orgId, token, contacts, accounts }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [type, setType]             = useState<InvoiceType>("Receivable");
  const [contactId, setContactId]   = useState("");
  const [number, setNumber]         = useState("");
  const [date, setDate]             = useState(today());
  const [dueDate, setDueDate]       = useState(inDays(30));
  const [arApAccountId, setArApAccountId] = useState("");
  const [notes, setNotes]           = useState("");
  const [lines, setLines]           = useState<LineState[]>([
    { description: "", quantity: "1", unitPrice: "", accountId: "" },
  ]);
  const [error, setError] = useState<string | null>(null);

  const postableAccounts = accounts.filter(a => a.isPostable);
  const arApAccounts     = accounts.filter(a => a.isPostable && (a.type === "Asset" || a.type === "Liability"));

  const total = lines.reduce((s, l) => s + (parseFloat(l.quantity) || 0) * (parseFloat(l.unitPrice) || 0), 0);

  function updateLine(i: number, field: keyof LineState, value: string) {
    setLines(ls => ls.map((l, idx) => idx === i ? { ...l, [field]: value } : l));
  }

  const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!contactId) { setError("Selecciona un contacto."); return; }
    if (!number.trim()) { setError("El número de factura es requerido."); return; }
    if (!arApAccountId) { setError("Selecciona la cuenta AR/AP."); return; }
    if (lines.some(l => !l.description || !l.accountId || !l.unitPrice)) {
      setError("Completa todas las líneas."); return;
    }

    const parsedLines: CreateInvoiceLineRequest[] = lines.map(l => ({
      description: l.description.trim(),
      quantity:    parseFloat(l.quantity) || 1,
      unitPrice:   parseFloat(l.unitPrice) || 0,
      accountId:   l.accountId,
    }));

    startTransition(async () => {
      try {
        const inv = await apiClient.invoices.create(orgId, {
          type, contactId, number: number.trim(), date, dueDate,
          arApAccountId, notes: notes.trim() || undefined, lines: parsedLines,
        }, token);
        toast.success("Factura creada como borrador.");
        router.push(`/dashboard/invoices/${inv.id}`);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error al crear la factura.");
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">{error}</div>}

      <div className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800 space-y-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Encabezado</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Tipo</label>
            <select value={type} onChange={e => setType(e.target.value as InvoiceType)} className={inputClass}>
              <option value="Receivable">Por cobrar (cliente)</option>
              <option value="Payable">Por pagar (proveedor)</option>
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Contacto <span className="text-red-500">*</span></label>
            <select value={contactId} onChange={e => setContactId(e.target.value)} className={inputClass}>
              <option value="">Selecciona…</option>
              {contacts.filter(c => c.isActive).map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Número <span className="text-red-500">*</span></label>
            <input value={number} onChange={e => setNumber(e.target.value)} className={inputClass} placeholder="FAC-001" />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Cuenta {type === "Receivable" ? "por cobrar" : "por pagar"} <span className="text-red-500">*</span></label>
            <select value={arApAccountId} onChange={e => setArApAccountId(e.target.value)} className={inputClass}>
              <option value="">Selecciona…</option>
              {arApAccounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Fecha</label>
            <input type="date" value={date} onChange={e => setDate(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Fecha de vencimiento</label>
            <input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} className={inputClass} />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Notas</label>
          <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={2} className={inputClass} />
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800 space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Líneas</h2>
        {lines.map((l, i) => (
          <div key={i} className="grid gap-2 sm:grid-cols-12 items-end">
            <div className="sm:col-span-4">
              {i === 0 && <label className="mb-1 block text-xs text-slate-500">Descripción</label>}
              <input value={l.description} onChange={e => updateLine(i, "description", e.target.value)} className={inputClass} placeholder="Servicio…" />
            </div>
            <div className="sm:col-span-2">
              {i === 0 && <label className="mb-1 block text-xs text-slate-500">Cant.</label>}
              <input type="number" min="0.01" step="0.01" value={l.quantity} onChange={e => updateLine(i, "quantity", e.target.value)} className={inputClass} />
            </div>
            <div className="sm:col-span-2">
              {i === 0 && <label className="mb-1 block text-xs text-slate-500">Precio</label>}
              <input type="number" min="0" step="0.01" value={l.unitPrice} onChange={e => updateLine(i, "unitPrice", e.target.value)} className={inputClass} />
            </div>
            <div className="sm:col-span-3">
              {i === 0 && <label className="mb-1 block text-xs text-slate-500">Cuenta</label>}
              <select value={l.accountId} onChange={e => updateLine(i, "accountId", e.target.value)} className={inputClass}>
                <option value="">Selecciona…</option>
                {postableAccounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
              </select>
            </div>
            <div className="sm:col-span-1 flex justify-end">
              {lines.length > 1 && (
                <button type="button" onClick={() => setLines(ls => ls.filter((_, idx) => idx !== i))} className="rounded p-1 text-red-400 hover:bg-red-50 hover:text-red-600">
                  ✕
                </button>
              )}
            </div>
          </div>
        ))}
        <button type="button" onClick={() => setLines(ls => [...ls, { description: "", quantity: "1", unitPrice: "", accountId: "" }])} className="text-sm text-indigo-600 hover:underline dark:text-indigo-400">
          + Agregar línea
        </button>
        <div className="flex justify-end border-t border-slate-100 pt-3 dark:border-slate-700">
          <div className="text-right">
            <p className="text-xs text-slate-400">Total</p>
            <p className="text-xl font-bold text-slate-900 dark:text-slate-100 tabular-nums">
              {total.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
            </p>
          </div>
        </div>
      </div>

      <div className="flex gap-3">
        <button type="submit" disabled={isPending} className="rounded-lg bg-indigo-600 px-6 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
          {isPending ? "Guardando…" : "Crear factura"}
        </button>
        <button type="button" onClick={() => router.back()} className="rounded-lg border border-slate-300 px-6 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700">
          Cancelar
        </button>
      </div>
    </form>
  );
}

"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Account, Contact, InvoiceType, CreateInvoiceLineRequest, TaxRate, Product } from "@accounting/types";

interface LineState {
  description: string;
  quantity:    string;
  unitPrice:   string;
  accountId:   string;
  taxRateId:   string;
}

function today()       { return new Date().toISOString().slice(0, 10); }
function inDays(n: number) { const d = new Date(); d.setDate(d.getDate() + n); return d.toISOString().slice(0, 10); }

interface Props {
  orgId:    string;
  token:    string;
  contacts: Contact[];
  accounts: Account[];
  taxRates: TaxRate[];
  products: Product[];
}

export function CreateInvoiceForm({ orgId, token, contacts, accounts, taxRates, products }: Props) {
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
    { description: "", quantity: "1", unitPrice: "", accountId: "", taxRateId: "" },
  ]);
  const [error, setError] = useState<string | null>(null);

  const postableAccounts = accounts.filter(a => a.isPostable);
  const arApAccounts     = accounts.filter(a => a.isPostable && (a.type === "Asset" || a.type === "Liability"));
  const activeProducts   = products.filter(p => p.isActive);
  const activeTaxRates   = taxRates.filter(t => t.isActive);

  function updateLine(i: number, field: keyof LineState, value: string) {
    setLines(ls => ls.map((l, idx) => idx === i ? { ...l, [field]: value } : l));
  }

  function applyProduct(i: number, productId: string) {
    const p = products.find(x => x.id === productId);
    if (!p) return;
    setLines(ls => ls.map((l, idx) => idx === i ? {
      ...l,
      description: p.name,
      unitPrice:   p.defaultPrice > 0 ? String(p.defaultPrice) : l.unitPrice,
      accountId:   p.accountId,
      taxRateId:   p.taxRateId ?? "",
    } : l));
  }

  function lineTotal(l: LineState) {
    const sub  = (parseFloat(l.quantity) || 0) * (parseFloat(l.unitPrice) || 0);
    const rate = taxRates.find(t => t.id === l.taxRateId)?.rate ?? 0;
    return sub + sub * rate / 100;
  }

  const subTotal = lines.reduce((s, l) => {
    const sub = (parseFloat(l.quantity) || 0) * (parseFloat(l.unitPrice) || 0);
    return s + sub;
  }, 0);
  const taxTotal = lines.reduce((s, l) => {
    const sub  = (parseFloat(l.quantity) || 0) * (parseFloat(l.unitPrice) || 0);
    const rate = taxRates.find(t => t.id === l.taxRateId)?.rate ?? 0;
    return s + sub * rate / 100;
  }, 0);
  const total = subTotal + taxTotal;

  const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!contactId)    { setError("Selecciona un contacto."); return; }
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
      taxRateId:   l.taxRateId || undefined,
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

      {/* Encabezado */}
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

      {/* Líneas */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 dark:border-slate-700 dark:bg-slate-800 space-y-3">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-400">Líneas</h2>

        {lines.map((l, i) => (
          <div key={i} className="space-y-2 rounded-lg border border-slate-100 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-800/60">
            {activeProducts.length > 0 && (
              <div>
                {i === 0 && <p className="mb-1 text-xs text-slate-400">Producto (opcional)</p>}
                <select
                  onChange={e => { if (e.target.value) applyProduct(i, e.target.value); e.target.value = ""; }}
                  className={inputClass + " text-slate-400"}
                  defaultValue=""
                >
                  <option value="">Selecciona un producto…</option>
                  {activeProducts.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </div>
            )}
            <div className="grid gap-2 sm:grid-cols-12 items-end">
              <div className="sm:col-span-4">
                {i === 0 && <label className="mb-1 block text-xs text-slate-500">Descripción <span className="text-red-400">*</span></label>}
                <input value={l.description} onChange={e => updateLine(i, "description", e.target.value)} className={inputClass} placeholder="Servicio…" />
              </div>
              <div className="sm:col-span-1">
                {i === 0 && <label className="mb-1 block text-xs text-slate-500">Cant.</label>}
                <input type="number" min="0.01" step="0.01" value={l.quantity} onChange={e => updateLine(i, "quantity", e.target.value)} className={inputClass} />
              </div>
              <div className="sm:col-span-2">
                {i === 0 && <label className="mb-1 block text-xs text-slate-500">Precio unit.</label>}
                <input type="number" min="0" step="0.01" value={l.unitPrice} onChange={e => updateLine(i, "unitPrice", e.target.value)} className={inputClass} />
              </div>
              <div className="sm:col-span-2">
                {i === 0 && <label className="mb-1 block text-xs text-slate-500">Impuesto</label>}
                <select value={l.taxRateId} onChange={e => updateLine(i, "taxRateId", e.target.value)} className={inputClass}>
                  <option value="">Sin impuesto</option>
                  {activeTaxRates.map(t => <option key={t.id} value={t.id}>{t.name} ({t.rate}%)</option>)}
                </select>
              </div>
              <div className="sm:col-span-2">
                {i === 0 && <label className="mb-1 block text-xs text-slate-500">Cuenta <span className="text-red-400">*</span></label>}
                <select value={l.accountId} onChange={e => updateLine(i, "accountId", e.target.value)} className={inputClass}>
                  <option value="">Selecciona…</option>
                  {postableAccounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
                </select>
              </div>
              <div className="sm:col-span-1 flex flex-col items-end gap-1">
                {i === 0 && <span className="mb-1 block text-xs text-slate-500">Total</span>}
                <span className="text-sm font-semibold tabular-nums text-slate-700 dark:text-slate-300">
                  {lineTotal(l).toFixed(2)}
                </span>
                {lines.length > 1 && (
                  <button type="button" onClick={() => setLines(ls => ls.filter((_, idx) => idx !== i))} className="text-xs text-red-400 hover:text-red-600">
                    Eliminar
                  </button>
                )}
              </div>
            </div>
          </div>
        ))}

        <button
          type="button"
          onClick={() => setLines(ls => [...ls, { description: "", quantity: "1", unitPrice: "", accountId: "", taxRateId: "" }])}
          className="text-sm text-indigo-600 hover:underline dark:text-indigo-400"
        >
          + Agregar línea
        </button>

        {/* Totals */}
        <div className="flex justify-end border-t border-slate-100 pt-3 dark:border-slate-700">
          <div className="w-48 space-y-1 text-right">
            {taxTotal > 0 && (
              <>
                <div className="flex justify-between text-sm text-slate-500">
                  <span>Subtotal</span>
                  <span className="tabular-nums">{subTotal.toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-sm text-slate-500">
                  <span>Impuesto</span>
                  <span className="tabular-nums">{taxTotal.toFixed(2)}</span>
                </div>
              </>
            )}
            <div className="flex justify-between border-t border-slate-200 pt-1 dark:border-slate-600">
              <span className="text-sm font-bold text-slate-700 dark:text-slate-300">Total</span>
              <span className="text-lg font-bold tabular-nums text-slate-900 dark:text-slate-100">
                {total.toLocaleString("es-GT", { minimumFractionDigits: 2 })}
              </span>
            </div>
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

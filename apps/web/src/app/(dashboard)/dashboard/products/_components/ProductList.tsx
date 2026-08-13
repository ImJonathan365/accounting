"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Product, CreateProductRequest, UpdateProductRequest, TaxRate } from "@accounting/types";
import type { Account } from "@accounting/types";

interface Props { products: Product[]; accounts: Account[]; taxRates: TaxRate[]; orgId: string; token: string; canEdit: boolean; }

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

function CreateProductModal({ accounts, taxRates, orgId, token, onClose }: {
  accounts: Account[]; taxRates: TaxRate[]; orgId: string; token: string; onClose: () => void;
}) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [name, setName]             = useState("");
  const [description, setDesc]      = useState("");
  const [price, setPrice]           = useState("");
  const [accountId, setAccountId]   = useState("");
  const [taxRateId, setTaxRateId]   = useState("");
  const [error, setError]           = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim())  { setError("El nombre es requerido."); return; }
    if (!accountId)    { setError("Selecciona la cuenta contable."); return; }
    const priceNum = parseFloat(price) || 0;

    startTransition(async () => {
      try {
        await apiClient.products.create(orgId, {
          name: name.trim(),
          description: description.trim() || undefined,
          defaultPrice: priceNum,
          accountId,
          taxRateId: taxRateId || undefined,
        } as CreateProductRequest, token);
        toast.success("Producto creado.");
        onClose();
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Nuevo producto / servicio</h2>
        {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre <span className="text-red-500">*</span></label>
          <input value={name} onChange={e => setName(e.target.value)} className={inputClass} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Descripción</label>
          <input value={description} onChange={e => setDesc(e.target.value)} className={inputClass} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Precio predeterminado</label>
            <input type="number" min="0" step="0.01" value={price} onChange={e => setPrice(e.target.value)} className={inputClass} placeholder="0.00" />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Impuesto</label>
            <select value={taxRateId} onChange={e => setTaxRateId(e.target.value)} className={inputClass}>
              <option value="">Sin impuesto</option>
              {taxRates.filter(t => t.isActive).map(t => <option key={t.id} value={t.id}>{t.name} ({t.rate}%)</option>)}
            </select>
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Cuenta contable <span className="text-red-500">*</span></label>
          <select value={accountId} onChange={e => setAccountId(e.target.value)} className={inputClass}>
            <option value="">Selecciona cuenta…</option>
            {accounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
          </select>
        </div>
        <div className="flex gap-3 pt-2">
          <button type="submit" disabled={isPending} className="flex-1 rounded-lg bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            {isPending ? "Guardando…" : "Crear"}
          </button>
          <button type="button" onClick={onClose} className="flex-1 rounded-lg border border-slate-300 py-2 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300">
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

function EditProductModal({ product, accounts, taxRates, orgId, token, onClose }: {
  product: Product; accounts: Account[]; taxRates: TaxRate[]; orgId: string; token: string; onClose: () => void;
}) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [name, setName]           = useState(product.name);
  const [description, setDesc]    = useState(product.description ?? "");
  const [price, setPrice]         = useState(String(product.defaultPrice));
  const [accountId, setAccountId] = useState(product.accountId);
  const [taxRateId, setTaxRateId] = useState(product.taxRateId ?? "");
  const [isActive, setIsActive]   = useState(product.isActive);
  const [error, setError]         = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim())  { setError("El nombre es requerido."); return; }
    if (!accountId)    { setError("Selecciona la cuenta contable."); return; }
    const priceNum = parseFloat(price);
    if (isNaN(priceNum) || priceNum < 0) { setError("El precio debe ser un número mayor o igual a 0."); return; }

    const payload: UpdateProductRequest = {
      name: name.trim(),
      description: description.trim() || null,
      defaultPrice: priceNum,
      accountId,
      taxRateId: taxRateId || EMPTY_GUID,
      isActive,
    };

    startTransition(async () => {
      try {
        await apiClient.products.update(orgId, product.id, payload, token);
        toast.success("Producto actualizado.");
        onClose();
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onClick={e => { if (e.target === e.currentTarget && !isPending) onClose(); }}>
      <form onSubmit={handleSubmit} className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Editar producto / servicio</h2>
          <button type="button" onClick={onClose} className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-700">
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre <span className="text-red-500">*</span></label>
          <input value={name} onChange={e => setName(e.target.value)} className={inputClass} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Descripción</label>
          <input value={description} onChange={e => setDesc(e.target.value)} className={inputClass} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Precio predeterminado</label>
            <input type="number" min="0" step="0.01" value={price} onChange={e => setPrice(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Impuesto</label>
            <select value={taxRateId} onChange={e => setTaxRateId(e.target.value)} className={inputClass}>
              <option value="">Sin impuesto</option>
              {taxRates.filter(t => t.isActive || t.id === product.taxRateId).map(t => (
                <option key={t.id} value={t.id}>{t.name} ({t.rate}%)</option>
              ))}
            </select>
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Cuenta contable <span className="text-red-500">*</span></label>
          <select value={accountId} onChange={e => setAccountId(e.target.value)} className={inputClass}>
            <option value="">Selecciona cuenta…</option>
            {accounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
          </select>
        </div>
        <div className="flex items-center gap-3">
          <button
            type="button"
            role="switch"
            aria-checked={isActive}
            onClick={() => setIsActive(!isActive)}
            className={`relative inline-flex h-5 w-9 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 ${isActive ? "bg-indigo-600" : "bg-slate-200 dark:bg-slate-600"}`}
          >
            <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${isActive ? "translate-x-4" : "translate-x-0"}`} />
          </button>
          <span className="text-sm text-slate-700 dark:text-slate-300">Activo</span>
        </div>
        <div className="flex gap-3 pt-2">
          <button type="submit" disabled={isPending} className="flex-1 rounded-lg bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            {isPending ? "Guardando…" : "Guardar cambios"}
          </button>
          <button type="button" onClick={onClose} className="flex-1 rounded-lg border border-slate-300 py-2 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300">
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

export function ProductList({ products, accounts, taxRates, orgId, token, canEdit }: Props) {
  const router = useRouter();
  const [showCreate, setShowCreate]           = useState(false);
  const [editing, setEditing]                 = useState<Product | null>(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [isDeleting, startDeleteTransition]   = useTransition();

  function handleDelete(id: string) {
    startDeleteTransition(async () => {
      try {
        await apiClient.products.delete(orgId, id, token);
        toast.success("Producto eliminado.");
        setConfirmDeleteId(null);
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error al eliminar.");
        setConfirmDeleteId(null);
      }
    });
  }

  return (
    <>
      {showCreate && <CreateProductModal accounts={accounts} taxRates={taxRates} orgId={orgId} token={token} onClose={() => setShowCreate(false)} />}
      {editing    && <EditProductModal product={editing} accounts={accounts} taxRates={taxRates} orgId={orgId} token={token} onClose={() => setEditing(null)} />}

      <div className="flex justify-end">
        {canEdit && (
          <button onClick={() => setShowCreate(true)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            + Nuevo producto
          </button>
        )}
      </div>

      {products.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500">No hay productos. Crea uno para agilizar la facturación.</p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
          <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
            <thead className="bg-slate-50 dark:bg-slate-700/50">
              <tr>
                {["Nombre", "Precio", "Impuesto", "Cuenta", "Estado", ...(canEdit ? ["Acciones"] : [])].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30">
              {products.map(p => (
                <tr key={p.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
                  <td className="px-4 py-3">
                    <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{p.name}</p>
                    {p.description && <p className="text-xs text-slate-400 truncate max-w-xs">{p.description}</p>}
                  </td>
                  <td className="px-4 py-3 text-sm tabular-nums text-slate-700 dark:text-slate-300">
                    {p.defaultPrice.toFixed(2)}
                  </td>
                  <td className="px-4 py-3 text-sm text-slate-500">
                    {p.taxRateName ? `${p.taxRateName} (${p.taxRate}%)` : "—"}
                  </td>
                  <td className="px-4 py-3 text-sm text-slate-500">
                    <span className="font-mono text-slate-400 mr-1">{p.accountCode}</span>{p.accountName}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${p.isActive ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300" : "bg-slate-100 text-slate-500"}`}>
                      {p.isActive ? "Activo" : "Inactivo"}
                    </span>
                  </td>
                  {canEdit && (
                    <td className="px-4 py-3">
                      {confirmDeleteId === p.id ? (
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-slate-600 dark:text-slate-400">¿Eliminar?</span>
                          <button
                            onClick={() => handleDelete(p.id)}
                            disabled={isDeleting}
                            className="rounded px-2 py-1 text-xs font-semibold text-white bg-red-600 hover:bg-red-700 disabled:opacity-50"
                          >
                            Sí
                          </button>
                          <button
                            onClick={() => setConfirmDeleteId(null)}
                            disabled={isDeleting}
                            className="rounded px-2 py-1 text-xs text-slate-600 border border-slate-300 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300"
                          >
                            No
                          </button>
                        </div>
                      ) : (
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => setEditing(p)}
                            className="rounded px-2 py-1 text-xs font-medium text-indigo-600 hover:bg-indigo-50 dark:text-indigo-400 dark:hover:bg-indigo-900/30"
                          >
                            Editar
                          </button>
                          <button
                            onClick={() => setConfirmDeleteId(p.id)}
                            className="rounded px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/30"
                          >
                            Eliminar
                          </button>
                        </div>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

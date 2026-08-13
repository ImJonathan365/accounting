"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { TaxRate, CreateTaxRateRequest, UpdateTaxRateRequest } from "@accounting/types";
import type { Account } from "@accounting/types";

interface Props { taxRates: TaxRate[]; accounts: Account[]; orgId: string; token: string; canEdit: boolean; }

const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

function CreateTaxRateModal({ accounts, orgId, token, onClose }: { accounts: Account[]; orgId: string; token: string; onClose: () => void }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [name, setName]           = useState("");
  const [rate, setRate]           = useState("");
  const [accountId, setAccountId] = useState("");
  const [error, setError]         = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const rateNum = parseFloat(rate);
    if (!name.trim())             { setError("El nombre es requerido."); return; }
    if (isNaN(rateNum) || rateNum < 0 || rateNum > 100) { setError("La tasa debe ser entre 0 y 100."); return; }
    if (!accountId)               { setError("Selecciona la cuenta contable."); return; }

    startTransition(async () => {
      try {
        await apiClient.taxRates.create(orgId, { name: name.trim(), rate: rateNum, taxAccountId: accountId } as CreateTaxRateRequest, token);
        toast.success("Tasa de impuesto creada.");
        onClose();
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Nueva tasa de impuesto</h2>
        {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre <span className="text-red-500">*</span></label>
          <input value={name} onChange={e => setName(e.target.value)} className={inputClass} placeholder="Ej. IVA 12%" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Tasa (%) <span className="text-red-500">*</span></label>
          <input type="number" min="0" max="100" step="0.01" value={rate} onChange={e => setRate(e.target.value)} className={inputClass} placeholder="12" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Cuenta contable <span className="text-red-500">*</span></label>
          <select value={accountId} onChange={e => setAccountId(e.target.value)} className={inputClass}>
            <option value="">Selecciona cuenta…</option>
            {accounts.filter(a => a.isPostable).map(a => (
              <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
            ))}
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

function EditTaxRateModal({ taxRate, accounts, orgId, token, onClose }: { taxRate: TaxRate; accounts: Account[]; orgId: string; token: string; onClose: () => void }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [name, setName]           = useState(taxRate.name);
  const [rate, setRate]           = useState(String(taxRate.rate));
  const [accountId, setAccountId] = useState(taxRate.taxAccountId);
  const [isActive, setIsActive]   = useState(taxRate.isActive);
  const [error, setError]         = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const rateNum = parseFloat(rate);
    if (!name.trim())             { setError("El nombre es requerido."); return; }
    if (isNaN(rateNum) || rateNum < 0 || rateNum > 100) { setError("La tasa debe ser entre 0 y 100."); return; }
    if (!accountId)               { setError("Selecciona la cuenta contable."); return; }

    const payload: UpdateTaxRateRequest = { name: name.trim(), rate: rateNum, taxAccountId: accountId, isActive };

    startTransition(async () => {
      try {
        await apiClient.taxRates.update(orgId, taxRate.id, payload, token);
        toast.success("Tasa de impuesto actualizada.");
        onClose();
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" onClick={e => { if (e.target === e.currentTarget && !isPending) onClose(); }}>
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Editar tasa de impuesto</h2>
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
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Tasa (%) <span className="text-red-500">*</span></label>
          <input type="number" min="0" max="100" step="0.01" value={rate} onChange={e => setRate(e.target.value)} className={inputClass} />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Cuenta contable <span className="text-red-500">*</span></label>
          <select value={accountId} onChange={e => setAccountId(e.target.value)} className={inputClass}>
            <option value="">Selecciona cuenta…</option>
            {accounts.filter(a => a.isPostable).map(a => (
              <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
            ))}
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
          <span className="text-sm text-slate-700 dark:text-slate-300">Activa</span>
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

export function TaxRateList({ taxRates, accounts, orgId, token, canEdit }: Props) {
  const router = useRouter();
  const [showCreate, setShowCreate]           = useState(false);
  const [editing, setEditing]                 = useState<TaxRate | null>(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [isDeleting, startDeleteTransition]   = useTransition();

  function handleDelete(id: string) {
    startDeleteTransition(async () => {
      try {
        await apiClient.taxRates.delete(orgId, id, token);
        toast.success("Tasa de impuesto eliminada.");
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
      {showCreate && <CreateTaxRateModal accounts={accounts} orgId={orgId} token={token} onClose={() => setShowCreate(false)} />}
      {editing    && <EditTaxRateModal taxRate={editing} accounts={accounts} orgId={orgId} token={token} onClose={() => setEditing(null)} />}

      <div className="flex justify-end">
        {canEdit && (
          <button onClick={() => setShowCreate(true)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            + Nueva tasa
          </button>
        )}
      </div>

      {taxRates.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500">No hay tasas de impuesto. Crea una para usarla en las facturas.</p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
          <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
            <thead className="bg-slate-50 dark:bg-slate-700/50">
              <tr>
                {["Nombre", "Tasa", "Cuenta contable", "Estado", ...(canEdit ? ["Acciones"] : [])].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30">
              {taxRates.map(t => (
                <tr key={t.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30">
                  <td className="px-4 py-3 text-sm font-medium text-slate-900 dark:text-slate-100">{t.name}</td>
                  <td className="px-4 py-3 text-sm tabular-nums font-semibold text-indigo-600 dark:text-indigo-400">{t.rate}%</td>
                  <td className="px-4 py-3 text-sm text-slate-500">{t.taxAccountName}</td>
                  <td className="px-4 py-3">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${t.isActive ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300" : "bg-slate-100 text-slate-500"}`}>
                      {t.isActive ? "Activa" : "Inactiva"}
                    </span>
                  </td>
                  {canEdit && (
                    <td className="px-4 py-3">
                      {confirmDeleteId === t.id ? (
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-slate-600 dark:text-slate-400">¿Eliminar?</span>
                          <button
                            onClick={() => handleDelete(t.id)}
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
                            onClick={() => setEditing(t)}
                            className="rounded px-2 py-1 text-xs font-medium text-indigo-600 hover:bg-indigo-50 dark:text-indigo-400 dark:hover:bg-indigo-900/30"
                          >
                            Editar
                          </button>
                          <button
                            onClick={() => setConfirmDeleteId(t.id)}
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

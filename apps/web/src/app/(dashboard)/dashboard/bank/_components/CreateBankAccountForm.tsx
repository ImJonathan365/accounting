"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Account } from "@accounting/types";

interface Props { orgId: string; token: string; accounts: Account[]; }

export function CreateBankAccountForm({ orgId, token, accounts }: Props) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [name, setName]             = useState("");
  const [bankName, setBankName]     = useState("");
  const [accountNumber, setAccountNumber] = useState("");
  const [linkedAccountId, setLinkedAccountId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim()) { setError("El nombre es requerido."); return; }
    if (!linkedAccountId) { setError("Selecciona la cuenta contable."); return; }

    startTransition(async () => {
      try {
        await apiClient.bank.create(orgId, {
          name: name.trim(),
          bankName: bankName.trim() || undefined,
          accountNumber: accountNumber.trim() || undefined,
          linkedAccountId,
        }, token);
        toast.success("Cuenta bancaria creada.");
        setOpen(false);
        setName(""); setBankName(""); setAccountNumber(""); setLinkedAccountId("");
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error al crear la cuenta.");
      }
    });
  }

  if (!open) {
    return (
      <button onClick={() => setOpen(true)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
        + Nueva cuenta bancaria
      </button>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Nueva cuenta bancaria</h2>
        {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">{error}</p>}
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre <span className="text-red-500">*</span></label>
          <input value={name} onChange={e => setName(e.target.value)} className={inputClass} placeholder="Ej. Cuenta Operativa" />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Banco</label>
            <input value={bankName} onChange={e => setBankName(e.target.value)} className={inputClass} placeholder="Ej. Banrural" />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">No. de cuenta</label>
            <input value={accountNumber} onChange={e => setAccountNumber(e.target.value)} className={inputClass} placeholder="Ej. 123-456-7" />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Cuenta contable (Activo) <span className="text-red-500">*</span></label>
          <select value={linkedAccountId} onChange={e => setLinkedAccountId(e.target.value)} className={inputClass}>
            <option value="">Selecciona…</option>
            {accounts.map(a => <option key={a.id} value={a.id}>{a.code} — {a.name}</option>)}
          </select>
        </div>
        <div className="flex gap-3 pt-2">
          <button type="submit" disabled={isPending} className="flex-1 rounded-lg bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            {isPending ? "Creando…" : "Crear cuenta"}
          </button>
          <button type="button" onClick={() => setOpen(false)} className="flex-1 rounded-lg border border-slate-300 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700">
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

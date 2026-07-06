"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";

interface Props { orgId: string; token: string; }

export function CreateBudgetButton({ orgId, token }: Props) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [name, setName] = useState("");
  const [year, setYear] = useState(new Date().getFullYear());
  const [error, setError] = useState<string | null>(null);

  const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim()) { setError("El nombre es requerido."); return; }
    startTransition(async () => {
      try {
        const b = await apiClient.budgets.create(orgId, { name: name.trim(), year }, token);
        toast.success("Presupuesto creado.");
        setOpen(false);
        router.push(`/dashboard/budgets/${b.id}`);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error.");
      }
    });
  }

  if (!open) {
    return (
      <button onClick={() => setOpen(true)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
        + Nuevo presupuesto
      </button>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Nuevo presupuesto</h2>
        {error && <p className="text-sm text-red-600 dark:text-red-400">{error}</p>}
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre <span className="text-red-500">*</span></label>
          <input value={name} onChange={e => setName(e.target.value)} className={inputClass} placeholder="Ej. Presupuesto Operativo 2026" />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Año</label>
          <input type="number" value={year} onChange={e => setYear(Number(e.target.value))} className={inputClass} min={2000} max={2035} />
        </div>
        <div className="flex gap-3 pt-2">
          <button type="submit" disabled={isPending} className="flex-1 rounded-lg bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            {isPending ? "Creando…" : "Crear"}
          </button>
          <button type="button" onClick={() => setOpen(false)} className="flex-1 rounded-lg border border-slate-300 py-2 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300">
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

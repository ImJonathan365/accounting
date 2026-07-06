"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { RecurringEntry, RecurringFrequency } from "@accounting/types";

const FREQUENCIES: { value: RecurringFrequency; label: string }[] = [
  { value: "Weekly",    label: "Semanal" },
  { value: "Biweekly",  label: "Quincenal" },
  { value: "Monthly",   label: "Mensual" },
  { value: "Quarterly", label: "Trimestral" },
  { value: "Annually",  label: "Anual" },
];

interface Props {
  entry: RecurringEntry;
  orgId: string;
  token: string;
}

export function EditRecurringForm({ entry, orgId, token }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  const [description, setDescription] = useState(entry.description);
  const [reference,   setReference]   = useState(entry.reference ?? "");
  const [frequency,   setFrequency]   = useState<RecurringFrequency>(entry.frequency);
  const [nextDate,    setNextDate]     = useState(entry.nextDate);
  const [endDate,     setEndDate]      = useState(entry.endDate ?? "");
  const [isActive,    setIsActive]     = useState(entry.isActive);
  const [error,       setError]        = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!description.trim()) { setError("La descripción es requerida."); return; }

    startTransition(async () => {
      try {
        await apiClient.recurring.update(orgId, entry.id, {
          description: description.trim(),
          reference:   reference.trim() || null,
          frequency,
          nextDate,
          endDate:     endDate || null,
          isActive,
        } as never, token);
        toast.success("Plantilla actualizada.");
        router.push("/dashboard/journal/recurring");
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error al actualizar.");
      }
    });
  }

  const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && (
        <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {error}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Descripción <span className="text-red-500">*</span>
          </label>
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className={inputClass}
            maxLength={300}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Referencia
          </label>
          <input
            type="text"
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            className={inputClass}
            maxLength={100}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Frecuencia
          </label>
          <select value={frequency} onChange={(e) => setFrequency(e.target.value as RecurringFrequency)} className={inputClass}>
            {FREQUENCIES.map((f) => (
              <option key={f.value} value={f.value}>{f.label}</option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Próxima fecha
          </label>
          <input
            type="date"
            value={nextDate}
            onChange={(e) => setNextDate(e.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Fecha de vencimiento
          </label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            className={inputClass}
          />
        </div>
      </div>

      <label className="flex cursor-pointer items-center gap-3">
        <input
          type="checkbox"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
          className="h-4 w-4 rounded border-slate-300 text-indigo-600"
        />
        <span className="text-sm text-slate-700 dark:text-slate-300">Plantilla activa</span>
      </label>

      <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-800/40">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
          Líneas de asiento (solo lectura)
        </p>
        <table className="w-full text-sm">
          <thead>
            <tr className="text-left text-xs text-slate-400">
              <th className="pb-2 font-medium">Cuenta</th>
              <th className="pb-2 pr-4 text-right font-medium">Débito</th>
              <th className="pb-2 text-right font-medium">Crédito</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
            {entry.lines.map((l) => (
              <tr key={l.id}>
                <td className="py-1.5 text-slate-700 dark:text-slate-300">
                  <span className="mr-2 font-mono text-xs text-slate-400">{l.accountCode}</span>
                  {l.accountName}
                </td>
                <td className="py-1.5 pr-4 text-right tabular-nums text-slate-700 dark:text-slate-300">
                  {l.debit > 0 ? l.debit.toFixed(2) : "—"}
                </td>
                <td className="py-1.5 text-right tabular-nums text-slate-700 dark:text-slate-300">
                  {l.credit > 0 ? l.credit.toFixed(2) : "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <p className="mt-2 text-xs text-slate-400">Para modificar las líneas, elimina esta plantilla y crea una nueva.</p>
      </div>

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={isPending}
          className="rounded-lg bg-indigo-600 px-6 py-2 text-sm font-semibold text-white transition-colors hover:bg-indigo-700 disabled:opacity-50"
        >
          {isPending ? "Guardando…" : "Guardar cambios"}
        </button>
        <button
          type="button"
          onClick={() => router.back()}
          className="rounded-lg border border-slate-300 px-6 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}

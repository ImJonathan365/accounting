"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Account, RecurringFrequency, CreateRecurringLine } from "@accounting/types";

const FREQUENCIES: { value: RecurringFrequency; label: string }[] = [
  { value: "Weekly",    label: "Semanal" },
  { value: "Biweekly",  label: "Quincenal" },
  { value: "Monthly",   label: "Mensual" },
  { value: "Quarterly", label: "Trimestral" },
  { value: "Annually",  label: "Anual" },
];

interface LineState {
  accountId: string;
  debit:     string;
  credit:    string;
  note:      string;
}

function today() { return new Date().toISOString().slice(0, 10); }

interface Props {
  accounts: Account[];
  orgId:    string;
  token:    string;
}

export function CreateRecurringForm({ accounts, orgId, token }: Props) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  const [description, setDescription] = useState("");
  const [reference,   setReference]   = useState("");
  const [frequency,   setFrequency]   = useState<RecurringFrequency>("Monthly");
  const [startDate,   setStartDate]   = useState(today());
  const [endDate,     setEndDate]     = useState("");
  const [lines,       setLines]       = useState<LineState[]>([
    { accountId: "", debit: "", credit: "", note: "" },
    { accountId: "", debit: "", credit: "", note: "" },
  ]);
  const [error, setError] = useState<string | null>(null);

  function addLine() {
    setLines((l) => [...l, { accountId: "", debit: "", credit: "", note: "" }]);
  }

  function removeLine(i: number) {
    setLines((l) => l.filter((_, idx) => idx !== i));
  }

  function updateLine(i: number, field: keyof LineState, value: string) {
    setLines((l) => l.map((line, idx) => idx === i ? { ...line, [field]: value } : line));
  }

  const totalDebit  = lines.reduce((s, l) => s + (parseFloat(l.debit)  || 0), 0);
  const totalCredit = lines.reduce((s, l) => s + (parseFloat(l.credit) || 0), 0);
  const balanced    = Math.abs(totalDebit - totalCredit) < 0.001;

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!description.trim()) { setError("La descripción es requerida."); return; }
    if (lines.some((l) => !l.accountId)) { setError("Todas las líneas deben tener una cuenta."); return; }
    if (lines.some((l) => (parseFloat(l.debit) || 0) + (parseFloat(l.credit) || 0) === 0)) {
      setError("Cada línea debe tener un monto mayor a cero."); return;
    }
    if (!balanced)            { setError("Los débitos y créditos deben ser iguales."); return; }
    if (totalDebit === 0)     { setError("El monto total debe ser mayor a cero."); return; }

    const parsedLines: CreateRecurringLine[] = lines.map((l) => ({
      accountId: l.accountId,
      debit:     parseFloat(l.debit)  || 0,
      credit:    parseFloat(l.credit) || 0,
      note:      l.note || undefined,
    }));

    startTransition(async () => {
      try {
        await apiClient.recurring.create(orgId, {
          description: description.trim(),
          reference:   reference.trim() || undefined,
          frequency,
          startDate,
          endDate:     endDate || undefined,
          lines:       parsedLines,
        }, token);
        toast.success("Plantilla recurrente creada.");
        router.push("/dashboard/journal/recurring");
      } catch (err) {
        const msg = err instanceof ApiError ? err.message : "Error al crear la plantilla.";
        setError(msg);
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && (
        <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {error}
        </div>
      )}

      {/* Header fields */}
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Descripción <span className="text-red-500">*</span>
          </label>
          <input
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            maxLength={300}
            className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
            placeholder="Ej. Arrendamiento mensual de oficina"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Referencia</label>
          <input
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            maxLength={100}
            className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Frecuencia</label>
          <select
            value={frequency}
            onChange={(e) => setFrequency(e.target.value as RecurringFrequency)}
            className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
          >
            {FREQUENCIES.map((f) => (
              <option key={f.value} value={f.value}>{f.label}</option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Fecha de inicio <span className="text-red-500">*</span>
          </label>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Fecha fin <span className="text-xs font-normal text-slate-400">(opcional)</span>
          </label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
          />
        </div>
      </div>

      {/* Lines */}
      <div>
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-300">Líneas contables</h3>
          <div className={`text-xs font-medium ${balanced ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
            Débito: {totalDebit.toFixed(2)} · Crédito: {totalCredit.toFixed(2)}
            {!balanced && "  ✗ No cuadra"}
            {balanced && totalDebit > 0 && "  ✓ Cuadra"}
          </div>
        </div>

        <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-700">
          <table className="min-w-full">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-700/50">
                <th className="px-3 py-2 text-left text-xs font-semibold text-slate-400">Cuenta</th>
                <th className="px-3 py-2 text-right text-xs font-semibold text-slate-400">Débito</th>
                <th className="px-3 py-2 text-right text-xs font-semibold text-slate-400">Crédito</th>
                <th className="px-3 py-2 text-left text-xs font-semibold text-slate-400">Nota</th>
                <th className="w-8 px-2 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50 bg-white dark:bg-slate-800">
              {lines.map((line, i) => (
                <tr key={i}>
                  <td className="px-3 py-2">
                    <select
                      value={line.accountId}
                      onChange={(e) => updateLine(i, "accountId", e.target.value)}
                      className="w-full rounded border border-slate-300 px-2 py-1 text-sm dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
                    >
                      <option value="">Seleccionar…</option>
                      {accounts.map((a) => (
                        <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                      ))}
                    </select>
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={line.debit}
                      onChange={(e) => updateLine(i, "debit", e.target.value)}
                      className="w-24 rounded border border-slate-300 px-2 py-1 text-right text-sm dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={line.credit}
                      onChange={(e) => updateLine(i, "credit", e.target.value)}
                      className="w-24 rounded border border-slate-300 px-2 py-1 text-right text-sm dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      value={line.note}
                      onChange={(e) => updateLine(i, "note", e.target.value)}
                      className="w-full rounded border border-slate-300 px-2 py-1 text-sm dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
                    />
                  </td>
                  <td className="px-2 py-2">
                    <button
                      type="button"
                      onClick={() => removeLine(i)}
                      disabled={lines.length <= 2}
                      className="rounded p-0.5 text-slate-400 hover:text-red-500 disabled:opacity-30"
                    >
                      ×
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <button
          type="button"
          onClick={addLine}
          className="mt-2 text-sm font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400"
        >
          + Agregar línea
        </button>
      </div>

      {/* Actions */}
      <div className="flex gap-3">
        <button
          type="submit"
          disabled={isPending}
          className="rounded-lg bg-indigo-600 px-6 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors"
        >
          {isPending ? "Guardando…" : "Guardar plantilla"}
        </button>
        <a
          href="/dashboard/journal/recurring"
          className="rounded-lg border border-slate-300 px-6 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700 transition-colors"
        >
          Cancelar
        </a>
      </div>
    </form>
  );
}

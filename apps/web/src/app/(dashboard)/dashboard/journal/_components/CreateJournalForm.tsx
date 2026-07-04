"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createJournalEntryAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";
import type { Account } from "@accounting/types";

interface LineRow {
  id: number;
  accountId: string;
  debit: string;
  credit: string;
  note: string;
}

function emptyLine(id: number): LineRow {
  return { id, accountId: "", debit: "", credit: "", note: "" };
}

let nextId = 3;

export function CreateJournalForm({ accounts }: { accounts: Account[] }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  const [date, setDate] = useState(today());
  const [description, setDescription] = useState("");
  const [reference, setReference] = useState("");
  const [lines, setLines] = useState<LineRow[]>([emptyLine(1), emptyLine(2)]);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [globalError, setGlobalError] = useState<string | null>(null);

  const totalDebit  = lines.reduce((s, l) => s + (parseFloat(l.debit)  || 0), 0);
  const totalCredit = lines.reduce((s, l) => s + (parseFloat(l.credit) || 0), 0);
  const isBalanced  = totalDebit > 0 && Math.abs(totalDebit - totalCredit) < 0.001;

  function updateLine(id: number, field: keyof LineRow, value: string) {
    setLines((prev) => prev.map((l) => {
      if (l.id !== id) return l;
      const updated = { ...l, [field]: value };
      // clear the other amount when one is entered
      if (field === "debit" && value) updated.credit = "";
      if (field === "credit" && value) updated.debit = "";
      return updated;
    }));
    setErrors({});
  }

  function addLine() {
    setLines((prev) => [...prev, emptyLine(nextId++)]);
  }

  function removeLine(id: number) {
    if (lines.length <= 2) return;
    setLines((prev) => prev.filter((l) => l.id !== id));
  }

  function validate() {
    const e: Record<string, string> = {};
    if (!date) e.date = "La fecha es requerida.";
    if (!description.trim()) e.description = "La descripción es requerida.";
    if (description.trim().length > 500) e.description = "Máximo 500 caracteres.";
    if (reference.trim().length > 100) e.reference = "Máximo 100 caracteres.";

    lines.forEach((l, i) => {
      if (!l.accountId) e[`line_${i}_account`] = "Selecciona una cuenta.";
      const d = parseFloat(l.debit) || 0;
      const c = parseFloat(l.credit) || 0;
      if (d === 0 && c === 0) e[`line_${i}_amount`] = "Ingresa débito o crédito.";
      if (d > 0 && c > 0)     e[`line_${i}_amount`] = "Solo débito o crédito, no ambos.";
    });

    if (!isBalanced) e.balance = "La suma de débitos debe ser igual a la suma de créditos.";
    return e;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const errs = validate();
    if (Object.keys(errs).length) { setErrors(errs); return; }
    setErrors({});
    setGlobalError(null);

    startTransition(async () => {
      try {
        await createJournalEntryAction({
          date,
          description: description.trim(),
          reference: reference.trim() || undefined,
          lines: lines.map((l) => ({
            accountId: l.accountId,
            debit:     parseFloat(l.debit)  || 0,
            credit:    parseFloat(l.credit) || 0,
            note:      l.note.trim() || undefined,
          })),
        });
        router.push("/dashboard/journal");
      } catch (err) {
        if (err instanceof ApiError && err.fieldErrors) {
          const mapped: Record<string, string> = {};
          for (const [k, v] of Object.entries(err.fieldErrors)) mapped[k] = v[0];
          setErrors(mapped);
        } else {
          setGlobalError(err instanceof Error ? err.message : "Error al registrar el asiento.");
        }
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {globalError && (
        <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
          {globalError}
        </div>
      )}

      {/* Header */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h3 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Encabezado
        </h3>
        <div className="grid gap-4 sm:grid-cols-3">
          <Field label="Fecha" error={errors.date}>
            <input
              type="date"
              value={date}
              onChange={(e) => { setDate(e.target.value); setErrors({}); }}
              className={inp(!!errors.date)}
            />
          </Field>
          <Field label="Descripción" error={errors.description} className="sm:col-span-2">
            <input
              type="text"
              placeholder="Ej: Pago de servicios públicos"
              value={description}
              onChange={(e) => { setDescription(e.target.value); setErrors({}); }}
              className={inp(!!errors.description)}
            />
          </Field>
          <Field label={<>Referencia <span className="font-normal text-slate-400">(opcional)</span></>} error={errors.reference}>
            <input
              type="text"
              placeholder="Ej: Factura #001"
              value={reference}
              onChange={(e) => { setReference(e.target.value); setErrors({}); }}
              className={inp(!!errors.reference)}
            />
          </Field>
        </div>
      </div>

      {/* Lines */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800 overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-700">
          <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            Líneas del asiento
          </h3>
          <button type="button" onClick={addLine}
            className="rounded-lg border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700 transition-colors">
            + Agregar línea
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-700/50">
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-500 dark:text-slate-400">Cuenta</th>
                <th className="px-4 py-2 text-right text-xs font-semibold text-slate-500 dark:text-slate-400 w-36">Débito</th>
                <th className="px-4 py-2 text-right text-xs font-semibold text-slate-500 dark:text-slate-400 w-36">Crédito</th>
                <th className="px-4 py-2 text-left text-xs font-semibold text-slate-500 dark:text-slate-400 hidden md:table-cell">Nota</th>
                <th className="w-10" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
              {lines.map((line, i) => (
                <tr key={line.id}>
                  <td className="px-4 py-2">
                    <div>
                      <select
                        value={line.accountId}
                        onChange={(e) => updateLine(line.id, "accountId", e.target.value)}
                        className={inp(!!errors[`line_${i}_account`])}
                      >
                        <option value="">Selecciona cuenta</option>
                        {accounts.map((a) => (
                          <option key={a.id} value={a.id}>
                            {a.code} — {a.name}
                          </option>
                        ))}
                      </select>
                      {errors[`line_${i}_account`] && (
                        <p className="mt-0.5 text-xs text-red-600 dark:text-red-400">{errors[`line_${i}_account`]}</p>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <div>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        placeholder="0.00"
                        value={line.debit}
                        onChange={(e) => updateLine(line.id, "debit", e.target.value)}
                        className={`${inp(!!errors[`line_${i}_amount`])} text-right`}
                      />
                    </div>
                  </td>
                  <td className="px-4 py-2">
                    <div>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        placeholder="0.00"
                        value={line.credit}
                        onChange={(e) => updateLine(line.id, "credit", e.target.value)}
                        className={`${inp(!!errors[`line_${i}_amount`])} text-right`}
                      />
                      {errors[`line_${i}_amount`] && (
                        <p className="mt-0.5 text-xs text-red-600 dark:text-red-400">{errors[`line_${i}_amount`]}</p>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-2 hidden md:table-cell">
                    <input
                      type="text"
                      placeholder="Opcional"
                      value={line.note}
                      onChange={(e) => updateLine(line.id, "note", e.target.value)}
                      className={inp(false)}
                    />
                  </td>
                  <td className="px-4 py-2 text-center">
                    <button
                      type="button"
                      onClick={() => removeLine(line.id)}
                      disabled={lines.length <= 2}
                      className="text-slate-400 hover:text-red-500 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                      title="Eliminar línea"
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Totals */}
        <div className="border-t border-slate-200 px-4 py-3 dark:border-slate-700">
          <div className="flex items-center justify-end gap-8">
            <div className="text-right">
              <p className="text-xs text-slate-500 dark:text-slate-400">Total débitos</p>
              <p className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
                {fmt(totalDebit)}
              </p>
            </div>
            <div className="text-right">
              <p className="text-xs text-slate-500 dark:text-slate-400">Total créditos</p>
              <p className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
                {fmt(totalCredit)}
              </p>
            </div>
            <div className="flex items-center gap-1.5">
              {isBalanced ? (
                <span className="flex items-center gap-1 rounded-full bg-green-50 px-3 py-1 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-400">
                  <svg className="h-3 w-3" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                  Balanceado
                </span>
              ) : (
                <span className="flex items-center gap-1 rounded-full bg-red-50 px-3 py-1 text-xs font-medium text-red-700 dark:bg-red-900/30 dark:text-red-400">
                  <svg className="h-3 w-3" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                  Desbalanceado
                </span>
              )}
            </div>
          </div>
          {errors.balance && (
            <p className="mt-2 text-right text-xs text-red-600 dark:text-red-400">{errors.balance}</p>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="flex gap-3">
        <button
          type="submit"
          disabled={isPending || !isBalanced}
          className="rounded-lg bg-indigo-600 px-5 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {isPending ? "Guardando…" : "Registrar asiento"}
        </button>
        <button
          type="button"
          onClick={() => router.push("/dashboard/journal")}
          className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700 transition-colors"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}

function Field({
  label, error, children, className = "",
}: {
  label: React.ReactNode; error?: string; children: React.ReactNode; className?: string;
}) {
  return (
    <div className={className}>
      <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{error}</p>}
    </div>
  );
}

const inp = (hasError: boolean) =>
  `block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 transition focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 ${
    hasError
      ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
      : "border-slate-300 focus:border-indigo-500 focus:ring-indigo-200 dark:border-slate-600 dark:focus:ring-indigo-900"
  }`;

function today() {
  return new Date().toISOString().split("T")[0];
}

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

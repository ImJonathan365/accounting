"use client";

import { useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { ImportBankTransactionRow } from "@accounting/types";

interface Props { orgId: string; bankAccountId: string; token: string; }

function parseCsv(text: string): ImportBankTransactionRow[] {
  const lines = text.trim().split("\n").slice(1); // skip header
  return lines.map((line) => {
    const [date, description, amountStr] = line.split(",").map(s => s.trim().replace(/^"|"$/g, ""));
    const amount = parseFloat(amountStr ?? "0");
    const txType: "Credit" | "Debit" = amount >= 0 ? "Credit" : "Debit";
    return { date, description, amount: Math.abs(amount), type: txType };
  }).filter(r => r.date && r.description && r.amount !== 0);
}

export function ImportTransactionsButton({ orgId, bankAccountId, token }: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
  const router  = useRouter();
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setError(null);
    const reader = new FileReader();
    reader.onload = (ev) => {
      const text = ev.target?.result as string;
      const rows = parseCsv(text);
      if (rows.length === 0) { setError("El archivo no contiene transacciones válidas."); return; }
      startTransition(async () => {
        try {
          const result = await apiClient.bank.import(orgId, bankAccountId, rows, token);
          toast.success(`${result.length} transacciones importadas.`);
          router.refresh();
        } catch (err) {
          setError(err instanceof ApiError ? err.message : "Error al importar.");
        }
      });
    };
    reader.readAsText(file);
    e.target.value = "";
  }

  return (
    <div>
      <input ref={fileRef} type="file" accept=".csv" className="hidden" onChange={handleFile} />
      <div className="flex flex-col items-end gap-1">
        <button
          onClick={() => fileRef.current?.click()}
          disabled={isPending}
          className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
        >
          {isPending ? "Importando…" : "Importar CSV"}
        </button>
        {error && <p className="text-xs text-red-600 dark:text-red-400">{error}</p>}
        <p className="text-xs text-slate-400">Formato: fecha, descripción, monto</p>
      </div>
    </div>
  );
}

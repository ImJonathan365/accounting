"use client";

import { useState } from "react";
import { apiClient, ApiError } from "@/lib/api-client";

interface Props {
  /** Path relativo al API, sin el baseUrl. Ej: /api/organizations/{id}/reports/trial-balance/export */
  buildPath: (format: "pdf" | "csv") => string;
  token: string;
  baseName: string; // Para el nombre del archivo si el servidor no lo envía
}

export function ExportButton({ buildPath, token, baseName }: Props) {
  const [loading, setLoading] = useState<"pdf" | "csv" | null>(null);
  const [error,   setError]   = useState<string | null>(null);

  async function download(format: "pdf" | "csv") {
    setLoading(format);
    setError(null);
    try {
      const path = buildPath(format);
      const blob = await apiClient.export.download(path, token);

      // Obtener nombre de archivo del header Content-Disposition si está disponible
      const url  = URL.createObjectURL(blob);
      const a    = document.createElement("a");
      a.href     = url;
      a.download = `${baseName}.${format}`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err: unknown) {
      setError(err instanceof ApiError ? err.message : "Error al exportar");
    } finally {
      setLoading(null);
    }
  }

  return (
    <div className="flex items-center gap-2">
      <span className="text-sm text-slate-500 dark:text-slate-400">Exportar:</span>
      <button
        onClick={() => download("pdf")}
        disabled={loading !== null}
        className="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-200 dark:hover:bg-slate-600 transition-colors"
      >
        {loading === "pdf" ? (
          <span className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-slate-600" />
        ) : (
          <PdfIcon />
        )}
        PDF
      </button>
      <button
        onClick={() => download("csv")}
        disabled={loading !== null}
        className="flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-200 dark:hover:bg-slate-600 transition-colors"
      >
        {loading === "csv" ? (
          <span className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-slate-600" />
        ) : (
          <CsvIcon />
        )}
        CSV
      </button>
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  );
}

function PdfIcon() {
  return (
    <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round"
        d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
    </svg>
  );
}

function CsvIcon() {
  return (
    <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round"
        d="M3.375 19.5h17.25m-17.25 0a1.125 1.125 0 01-1.125-1.125M3.375 19.5h1.5C5.496 19.5 6 18.996 6 18.375m-3.75.125V5.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125H6M3.375 19.5C2.754 19.5 2.25 18.996 2.25 18.375V5.625" />
    </svg>
  );
}

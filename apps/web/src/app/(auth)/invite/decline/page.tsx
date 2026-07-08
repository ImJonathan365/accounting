"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { declineInvitationAction } from "@/lib/actions";

export default function DeclineInvitePage() {
  const params   = useSearchParams();
  const rawToken = params.get("token") ?? "";
  const [done,  setDone]  = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!rawToken) { setError("El enlace no es válido."); return; }
    declineInvitationAction(rawToken)
      .then(() => setDone(true))
      .catch((err) => setError(err instanceof Error ? err.message : "Error al rechazar la invitación."));
  }, [rawToken]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm dark:border-slate-700 dark:bg-slate-800">
          {!done && !error && (
            <>
              <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
              <p className="text-slate-600 dark:text-slate-300">Procesando…</p>
            </>
          )}
          {done && (
            <>
              <div className="mb-4 text-4xl">👍</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">Invitación rechazada</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">Has rechazado la invitación. Puedes cerrar esta página.</p>
            </>
          )}
          {error && (
            <>
              <div className="mb-4 text-4xl">⚠️</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">Error</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">{error}</p>
            </>
          )}
        </div>
        <p className="mt-6 text-center text-sm">
          <Link href="/login" className="font-medium text-indigo-600 hover:underline dark:text-indigo-400">
            Ir al inicio de sesión
          </Link>
        </p>
      </div>
    </div>
  );
}

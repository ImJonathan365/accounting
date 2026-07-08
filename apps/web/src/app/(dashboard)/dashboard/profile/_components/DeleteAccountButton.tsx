"use client";

import { useState, useTransition } from "react";
import { deleteAccountAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

export function DeleteAccountButton() {
  const [open,      setOpen]      = useState(false);
  const [password,  setPassword]  = useState("");
  const [error,     setError]     = useState("");
  const [isPending, startTransition] = useTransition();

  function handleOpen() {
    setPassword("");
    setError("");
    setOpen(true);
  }

  function handleClose() {
    if (isPending) return;
    setOpen(false);
    setPassword("");
    setError("");
  }

  function handleConfirm() {
    if (!password) { setError("Ingresa tu contraseña para confirmar."); return; }
    setError("");
    startTransition(async () => {
      try {
        await deleteAccountAction(password);
      } catch (err) {
        if (err instanceof ApiError) {
          setError(err.message);
        } else if (err instanceof Error && !err.message.includes("NEXT_REDIRECT")) {
          setError(err.message);
        }
      }
    });
  }

  return (
    <>
      <button
        type="button"
        onClick={handleOpen}
        className="rounded-lg border border-red-300 px-4 py-2 text-sm font-medium text-red-600 transition hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 dark:border-red-700 dark:text-red-400 dark:hover:bg-red-900/20 dark:focus:ring-offset-slate-800"
      >
        Eliminar cuenta
      </button>

      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 px-4 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-700 dark:bg-slate-800">
            <div className="mb-4 flex items-start gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-red-100 dark:bg-red-900/30">
                <svg className="h-5 w-5 text-red-600 dark:text-red-400" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
                </svg>
              </div>
              <div>
                <h3 className="font-semibold text-slate-900 dark:text-slate-100">Eliminar cuenta</h3>
                <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
                  Esta acción es permanente e irreversible. Tu cuenta quedará desactivada y no podrás volver a iniciar sesión.
                </p>
              </div>
            </div>

            <div className="mb-4">
              <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
                Ingresa tu contraseña para confirmar
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => { setPassword(e.target.value); setError(""); }}
                placeholder="Tu contraseña actual"
                autoComplete="current-password"
                className={`block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 ${
                  error
                    ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
                    : "border-slate-300 focus:border-red-400 focus:ring-red-200 dark:border-slate-600 dark:focus:ring-red-900"
                }`}
              />
              {error && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{error}</p>}
            </div>

            <div className="flex gap-3">
              <button
                type="button"
                onClick={handleClose}
                disabled={isPending}
                className="flex-1 rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
              >
                Cancelar
              </button>
              <button
                type="button"
                onClick={handleConfirm}
                disabled={isPending || !password}
                className="flex-1 rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-700 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 dark:focus:ring-offset-slate-800"
              >
                {isPending ? "Eliminando…" : "Sí, eliminar mi cuenta"}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

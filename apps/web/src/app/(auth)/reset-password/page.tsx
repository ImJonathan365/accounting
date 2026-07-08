"use client";

import { useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { resetPasswordAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

function validate(password: string, confirm: string) {
  const e: Record<string, string> = {};
  if (!password) e.password = "La contraseña es requerida.";
  else if (password.length < 8) e.password = "Mínimo 8 caracteres.";
  else if (!/[A-Z]/.test(password)) e.password = "Debe incluir al menos una mayúscula.";
  else if (!/[0-9]/.test(password)) e.password = "Debe incluir al menos un número.";
  if (!confirm) e.confirm = "Confirma tu contraseña.";
  else if (password !== confirm) e.confirm = "Las contraseñas no coinciden.";
  return e;
}

export default function ResetPasswordPage() {
  const params = useSearchParams();
  const router = useRouter();
  const token  = params.get("token") ?? "";

  const [password,  setPassword]  = useState("");
  const [confirm,   setConfirm]   = useState("");
  const [errors,    setErrors]    = useState<Record<string, string>>({});
  const [globalErr, setGlobalErr] = useState("");
  const [loading,   setLoading]   = useState(false);
  const [done,      setDone]      = useState(false);

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm dark:border-slate-700 dark:bg-slate-800">
          <div className="mb-4 text-4xl">⚠️</div>
          <p className="mb-4 text-sm text-slate-600 dark:text-slate-300">
            El enlace de recuperación es inválido o ha expirado.
          </p>
          <Link href="/forgot-password" className="text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-400">
            Solicitar nuevo enlace
          </Link>
        </div>
      </div>
    );
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const fieldErrors = validate(password, confirm);
    if (Object.keys(fieldErrors).length) { setErrors(fieldErrors); return; }
    setErrors({});
    setGlobalErr("");
    setLoading(true);
    try {
      await resetPasswordAction(token, password, confirm);
      setDone(true);
      setTimeout(() => router.push("/login"), 2500);
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors) {
        const mapped: Record<string, string> = {};
        for (const [k, v] of Object.entries(err.fieldErrors)) mapped[k] = v[0];
        setErrors(mapped);
      } else {
        setGlobalErr(err instanceof Error ? err.message : "Error al restablecer la contraseña.");
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100">Nueva contraseña</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Elige una contraseña segura para tu cuenta
          </p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
          {done ? (
            <div className="text-center">
              <div className="mb-4 text-4xl">✅</div>
              <h2 className="mb-2 font-bold text-slate-900 dark:text-slate-100">¡Contraseña actualizada!</h2>
              <p className="text-sm text-slate-500 dark:text-slate-400">Redirigiendo al inicio de sesión…</p>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              {globalErr && (
                <div className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
                  {globalErr}
                </div>
              )}

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
                  Nueva contraseña
                </label>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => { setPassword(e.target.value); setErrors((p) => ({ ...p, password: "" })); }}
                  autoComplete="new-password"
                  className={input(!!errors.password)}
                />
                {errors.password
                  ? <p className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.password}</p>
                  : <p className="mt-1 text-xs text-slate-400 dark:text-slate-500">Mínimo 8 caracteres, una mayúscula y un número.</p>
                }
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
                  Confirmar contraseña
                </label>
                <input
                  type="password"
                  value={confirm}
                  onChange={(e) => { setConfirm(e.target.value); setErrors((p) => ({ ...p, confirm: "" })); }}
                  autoComplete="new-password"
                  className={input(!!errors.confirm)}
                />
                {errors.confirm && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{errors.confirm}</p>}
              </div>

              <button type="submit" disabled={loading} className={btn}>
                {loading ? "Guardando…" : "Guardar contraseña"}
              </button>
            </form>
          )}
        </div>

        {!done && (
          <p className="mt-6 text-center text-sm text-slate-500 dark:text-slate-400">
            <Link href="/forgot-password" className="font-medium text-indigo-600 hover:underline dark:text-indigo-400">
              Solicitar nuevo enlace
            </Link>
          </p>
        )}
      </div>
    </div>
  );
}

const input = (hasError: boolean) =>
  `block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 transition focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 ${
    hasError
      ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
      : "border-slate-300 focus:border-indigo-500 focus:ring-indigo-200 dark:border-slate-600 dark:focus:ring-indigo-900"
  }`;

const btn =
  "w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 transition dark:focus:ring-offset-slate-800";

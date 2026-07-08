"use client";

import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { verifyEmailAction, resendVerificationAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

type State = "sent" | "verifying" | "success" | "error" | "resend";

export default function VerifyEmailPage() {
  const params = useSearchParams();
  const router = useRouter();
  const token  = params.get("token");
  const sent   = params.get("sent") === "true";

  const [state,        setState]        = useState<State>(token ? "verifying" : "sent");
  const [errorMsg,     setErrorMsg]     = useState("");
  const [resendEmail,  setResendEmail]  = useState("");
  const [resendDone,   setResendDone]   = useState(false);
  const [resendLoading, setResendLoading] = useState(false);

  // Auto-verify when token is present
  useEffect(() => {
    if (!token) return;
    verifyEmailAction(token)
      .then(() => {
        setState("success");
        setTimeout(() => router.push("/dashboard"), 2500);
      })
      .catch((err) => {
        setErrorMsg(err instanceof Error ? err.message : "El enlace es inválido o ha expirado.");
        setState("error");
      });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function handleResend(e: React.FormEvent) {
    e.preventDefault();
    setResendLoading(true);
    try {
      await resendVerificationAction(resendEmail.trim());
      setResendDone(true);
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : "Error al reenviar.");
    } finally {
      setResendLoading(false);
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-8 shadow-sm dark:border-slate-700 dark:bg-slate-800">
          {/* Verifying */}
          {state === "verifying" && (
            <div className="text-center">
              <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
              <p className="text-slate-600 dark:text-slate-300">Verificando tu email…</p>
            </div>
          )}

          {/* Email sent confirmation */}
          {state === "sent" && !resendDone && (
            <>
              <div className="mb-4 text-center text-4xl">📬</div>
              <h1 className="mb-2 text-center text-xl font-bold text-slate-900 dark:text-slate-100">Revisa tu bandeja</h1>
              <p className="mb-6 text-center text-sm text-slate-500 dark:text-slate-400">
                Te enviamos un enlace de verificación. Puede tardar unos minutos en llegar, también revisa tu carpeta de spam.
              </p>
              {state === "sent" && (
                <button
                  onClick={() => setState("resend")}
                  className="w-full text-center text-sm font-medium text-indigo-600 hover:underline dark:text-indigo-400"
                >
                  ¿No recibiste el email? Reenviar
                </button>
              )}
            </>
          )}

          {/* Resend form */}
          {state === "resend" && !resendDone && (
            <>
              <h1 className="mb-2 text-center text-xl font-bold text-slate-900 dark:text-slate-100">Reenviar verificación</h1>
              <p className="mb-4 text-center text-sm text-slate-500 dark:text-slate-400">
                Ingresa tu email y te enviaremos un nuevo enlace.
              </p>
              <form onSubmit={handleResend} className="space-y-4">
                <input
                  type="email"
                  value={resendEmail}
                  onChange={(e) => setResendEmail(e.target.value)}
                  placeholder="tu@email.com"
                  required
                  className="block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500"
                />
                {errorMsg && <p className="text-xs text-red-600 dark:text-red-400">{errorMsg}</p>}
                <button type="submit" disabled={resendLoading} className={btn}>
                  {resendLoading ? "Enviando…" : "Reenviar enlace"}
                </button>
              </form>
            </>
          )}

          {/* Resend success */}
          {resendDone && (
            <div className="text-center">
              <div className="mb-4 text-4xl">✅</div>
              <p className="text-sm text-slate-600 dark:text-slate-300">
                Si el email está registrado y no verificado, recibirás un nuevo enlace en unos minutos.
              </p>
            </div>
          )}

          {/* Verification success */}
          {state === "success" && (
            <div className="text-center">
              <div className="mb-4 text-4xl">🎉</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">¡Email verificado!</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Tu cuenta está activa. Redirigiendo al dashboard…
              </p>
            </div>
          )}

          {/* Verification error */}
          {state === "error" && (
            <div className="text-center">
              <div className="mb-4 text-4xl">⚠️</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">Enlace inválido</h1>
              <p className="mb-6 text-sm text-slate-500 dark:text-slate-400">{errorMsg}</p>
              <button onClick={() => setState("resend")} className={btn}>
                Solicitar nuevo enlace
              </button>
            </div>
          )}
        </div>

        <p className="mt-6 text-center text-sm text-slate-500 dark:text-slate-400">
          <Link href="/login" className="font-medium text-indigo-600 hover:underline dark:text-indigo-400">
            Volver al inicio de sesión
          </Link>
        </p>
      </div>
    </div>
  );
}

const btn =
  "w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50 transition dark:focus:ring-offset-slate-800";

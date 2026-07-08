"use client";

import { useEffect, useState, useTransition } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { apiClient, ApiError } from "@/lib/api-client";
import { acceptInvitationAction, declineInvitationAction } from "@/lib/actions";
import type { InvitationInfo } from "@accounting/types";

type State = "loading" | "ready" | "accepting" | "declining" | "accepted" | "declined" | "error";

const ROLE_LABELS: Record<string, string> = {
  owner:  "Propietario",
  admin:  "Administrador",
  member: "Miembro",
};

export default function InvitePage() {
  const params    = useSearchParams();
  const router    = useRouter();
  const rawToken  = params.get("token") ?? "";

  const [state,   setState]   = useState<State>("loading");
  const [info,    setInfo]    = useState<InvitationInfo | null>(null);
  const [errorMsg, setErrorMsg] = useState("");
  const [isPending, startTransition] = useTransition();

  useEffect(() => {
    if (!rawToken) { setState("error"); setErrorMsg("El enlace de invitación no es válido."); return; }

    apiClient.invitations.getInfo(rawToken)
      .then((data) => {
        setInfo(data);
        setState(data.isValid ? "ready" : "error");
        if (!data.isValid) setErrorMsg(data.invalidReason);
      })
      .catch(() => {
        setState("error");
        setErrorMsg("No se pudo cargar la invitación.");
      });
  }, [rawToken]);

  function handleAccept() {
    startTransition(async () => {
      setState("accepting");
      try {
        await acceptInvitationAction(rawToken, info!.orgId);
        // Only reached if no redirect (shouldn't normally happen)
        router.push("/dashboard");
      } catch (err) {
        if (err instanceof Error && err.message.includes("NEXT_REDIRECT")) {
          return; // Server is redirecting — let it happen, don't show error
        }
        setErrorMsg(err instanceof ApiError ? err.message : (err instanceof Error ? err.message : "Error al aceptar."));
        setState("error");
      }
    });
  }

  function handleDecline() {
    startTransition(async () => {
      setState("declining");
      try {
        await declineInvitationAction(rawToken);
        setState("declined");
      } catch (err) {
        if (err instanceof Error) setErrorMsg(err.message);
        setState("error");
      }
    });
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-8 shadow-sm dark:border-slate-700 dark:bg-slate-800">

          {/* Loading */}
          {state === "loading" && (
            <div className="text-center">
              <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
              <p className="text-slate-600 dark:text-slate-300">Cargando invitación…</p>
            </div>
          )}

          {/* Ready to accept/decline */}
          {state === "ready" && info && (
            <>
              <div className="mb-4 text-center text-3xl">👋</div>
              <h1 className="mb-1 text-center text-xl font-bold text-slate-900 dark:text-slate-100">
                Invitación a {info.orgName}
              </h1>
              <p className="mb-6 text-center text-sm text-slate-500 dark:text-slate-400">
                <strong>{info.inviterName}</strong> te invitó a unirte como{" "}
                <strong>{ROLE_LABELS[info.role] ?? info.role}</strong>.
              </p>

              <div className="mb-4 rounded-lg bg-slate-50 p-3 text-center dark:bg-slate-700/50">
                <p className="text-xs text-slate-500 dark:text-slate-400">Invitación para</p>
                <p className="text-sm font-medium text-slate-800 dark:text-slate-200">{info.invitedEmail}</p>
              </div>

              <div className="space-y-3">
                <button
                  onClick={handleAccept}
                  disabled={isPending}
                  className="w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 dark:focus:ring-offset-slate-800"
                >
                  {isPending ? "Aceptando…" : "Aceptar invitación"}
                </button>
                <button
                  onClick={handleDecline}
                  disabled={isPending}
                  className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
                >
                  {isPending ? "Rechazando…" : "Rechazar"}
                </button>
              </div>
            </>
          )}

          {/* Accepted */}
          {state === "accepted" && (
            <div className="text-center">
              <div className="mb-4 text-4xl">🎉</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">¡Bienvenido al equipo!</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Ahora eres parte de <strong>{info?.orgName}</strong>. Redirigiendo al dashboard…
              </p>
            </div>
          )}

          {/* Declined */}
          {state === "declined" && (
            <div className="text-center">
              <div className="mb-4 text-4xl">👍</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">Invitación rechazada</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Has rechazado la invitación. Puedes cerrar esta página.
              </p>
            </div>
          )}

          {/* Error / invalid */}
          {state === "error" && (
            <div className="text-center">
              <div className="mb-4 text-4xl">⚠️</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">Invitación inválida</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">{errorMsg}</p>
            </div>
          )}
        </div>

        <p className="mt-6 text-center text-sm text-slate-500 dark:text-slate-400">
          <Link href="/login" className="font-medium text-indigo-600 hover:underline dark:text-indigo-400">
            Ir al inicio de sesión
          </Link>
        </p>
      </div>
    </div>
  );
}

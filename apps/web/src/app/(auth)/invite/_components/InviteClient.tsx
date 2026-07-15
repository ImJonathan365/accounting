"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { acceptInvitationAction, declineInvitationAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";
import type { InvitationInfo } from "@accounting/types";

type State = "ready" | "accepting" | "declining" | "declined" | "error";

const ROLE_LABELS: Record<string, string> = {
  owner:  "Propietario",
  admin:  "Administrador",
  member: "Miembro",
};

interface Props {
  rawToken:        string;
  isAuthenticated: boolean;
  initialInfo:     InvitationInfo | null;
  fetchError:      boolean;
}

export function InviteClient({ rawToken, isAuthenticated, initialInfo, fetchError }: Props) {
  const router = useRouter();

  const [state, setState] = useState<State>(() => {
    if (!rawToken || fetchError || !initialInfo) return "error";
    return initialInfo.isValid ? "ready" : "error";
  });

  const [errorMsg] = useState(() => {
    if (!rawToken)                              return "El enlace de invitación no es válido.";
    if (fetchError)                             return "No se pudo cargar la invitación.";
    if (initialInfo && !initialInfo.isValid)    return initialInfo.invalidReason;
    return "";
  });

  const [isPending, startTransition] = useTransition();

  function handleAccept() {
    startTransition(async () => {
      setState("accepting");
      try {
        await acceptInvitationAction(rawToken, initialInfo!.orgId);
        router.push("/dashboard");
      } catch (err) {
        if (err instanceof Error && err.message.includes("NEXT_REDIRECT")) return;
        setError(err);
      }
    });
  }

  function handleDecline() {
    startTransition(async () => {
      setState("declining");
      try {
        await declineInvitationAction(rawToken);
        setState("declined");
      } catch {
        setState("error");
      }
    });
  }

  function setError(err: unknown) {
    setState("error");
    // errorMsg is read-only from useState initializer; errors here go to console
    console.error(err instanceof ApiError ? err.message : err);
  }

  const acceptViaRouteHandler = `/api/invite/accept?token=${rawToken}`;
  const loginUrl = `/login?next=${encodeURIComponent(acceptViaRouteHandler)}`;

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4 dark:bg-slate-900">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-indigo-600 text-lg font-bold text-white shadow-lg">A</div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-8 shadow-sm dark:border-slate-700 dark:bg-slate-800">

          {state === "accepting" && (
            <div className="text-center">
              <div className="mx-auto mb-4 h-10 w-10 animate-spin rounded-full border-4 border-indigo-200 border-t-indigo-600" />
              <p className="text-slate-600 dark:text-slate-300">Aceptando invitación…</p>
            </div>
          )}

          {state === "ready" && initialInfo && (
            <>
              <div className="mb-4 text-center text-3xl">👋</div>
              <h1 className="mb-1 text-center text-xl font-bold text-slate-900 dark:text-slate-100">
                Invitación a {initialInfo.orgName}
              </h1>
              <p className="mb-6 text-center text-sm text-slate-500 dark:text-slate-400">
                <strong>{initialInfo.inviterName}</strong> te invitó a unirte como{" "}
                <strong>{ROLE_LABELS[initialInfo.role] ?? initialInfo.role}</strong>.
              </p>
              <div className="mb-4 rounded-lg bg-slate-50 p-3 text-center dark:bg-slate-700/50">
                <p className="text-xs text-slate-500 dark:text-slate-400">Invitación para</p>
                <p className="text-sm font-medium text-slate-800 dark:text-slate-200">{initialInfo.invitedEmail}</p>
              </div>

              {isAuthenticated ? (
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
              ) : (
                <div className="space-y-3">
                  <p className="text-center text-sm text-slate-500 dark:text-slate-400">
                    Debes iniciar sesión para aceptar esta invitación.
                  </p>
                  <Link
                    href={loginUrl}
                    className="block w-full rounded-lg bg-indigo-600 px-4 py-2.5 text-center text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 dark:focus:ring-offset-slate-800"
                  >
                    Iniciar sesión para aceptar
                  </Link>
                  <button
                    onClick={handleDecline}
                    disabled={isPending}
                    className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
                  >
                    Rechazar sin iniciar sesión
                  </button>
                </div>
              )}
            </>
          )}

          {state === "declined" && (
            <div className="text-center">
              <div className="mb-4 text-4xl">👍</div>
              <h1 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">Invitación rechazada</h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">Has rechazado la invitación. Puedes cerrar esta página.</p>
            </div>
          )}

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

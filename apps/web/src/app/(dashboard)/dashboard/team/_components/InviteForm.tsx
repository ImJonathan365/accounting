"use client";

import { useState, useTransition } from "react";
import { toast } from "sonner";
import { inviteMemberAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

export function InviteForm() {
  const [open, setOpen] = useState(false);
  const [email, setEmail] = useState("");
  const [role, setRole]   = useState("member");
  const [isPending, startTransition] = useTransition();

  function handleClose() {
    setOpen(false);
    setEmail("");
    setRole("member");
  }

  function handleSubmit(ev: React.FormEvent) {
    ev.preventDefault();
    startTransition(async () => {
      try {
        await inviteMemberAction({ email: email.trim().toLowerCase(), role });
        toast.success("Miembro invitado correctamente.");
        handleClose();
      } catch (err) {
        if (err instanceof ApiError && err.fieldErrors) {
          const msgs = Object.values(err.fieldErrors).flat();
          toast.error(msgs[0] ?? "Error al invitar.");
        } else {
          toast.error(err instanceof Error ? err.message : "Error al invitar al miembro.");
        }
      }
    });
  }

  if (!open) {
    return (
      <button
        onClick={() => setOpen(true)}
        className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 transition-colors"
      >
        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
        </svg>
        Invitar miembro
      </button>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Email del usuario</label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="usuario@ejemplo.com"
          required
          autoFocus
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 w-60"
        />
      </div>
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-slate-500 dark:text-slate-400">Rol</label>
        <select
          value={role}
          onChange={(e) => setRole(e.target.value)}
          className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
        >
          <option value="member">Miembro</option>
          <option value="admin">Administrador</option>
        </select>
      </div>
      <div className="flex items-center gap-2">
        <button
          type="submit"
          disabled={isPending || !email.trim()}
          className="rounded-lg bg-indigo-600 px-4 py-1.5 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors"
        >
          {isPending ? "Invitando…" : "Invitar"}
        </button>
        <button
          type="button"
          onClick={handleClose}
          disabled={isPending}
          className="rounded-lg px-3 py-1.5 text-sm text-slate-500 hover:text-slate-700 disabled:opacity-50 transition-colors"
        >
          Cancelar
        </button>
      </div>
    </form>
  );
}

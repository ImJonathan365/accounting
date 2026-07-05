"use client";

import { useState, useTransition } from "react";
import { toast } from "sonner";
import { updateMemberRoleAction, removeMemberAction } from "@/lib/actions";
import type { Member } from "@accounting/types";

const ROLE_LABELS: Record<string, string> = {
  owner:  "Propietario",
  admin:  "Administrador",
  member: "Miembro",
};

const ROLE_COLORS: Record<string, string> = {
  owner:  "bg-indigo-50 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300",
  admin:  "bg-purple-50 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300",
  member: "bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-300",
};

function initials(firstName: string, lastName: string) {
  return `${firstName[0] ?? ""}${lastName[0] ?? ""}`.toUpperCase();
}

interface Props {
  members: Member[];
  currentUserId: string;
  currentUserRole: string;
}

export function TeamTable({ members, currentUserId, currentUserRole }: Props) {
  const [, startTransition] = useTransition();
  const [removingId, setRemovingId] = useState<string | null>(null);
  const [confirmId,  setConfirmId]  = useState<string | null>(null);
  const [roleEdit,   setRoleEdit]   = useState<string | null>(null);

  const canChangeRole = (m: Member) =>
    currentUserRole === "owner" && m.userId !== currentUserId && m.role !== "owner";

  const canRemove = (m: Member) => {
    if (m.userId === currentUserId) return false;
    if (currentUserRole === "owner") return true;
    if (currentUserRole === "admin" && m.role === "member") return true;
    return false;
  };

  function handleRoleChange(userId: string, newRole: string) {
    setRoleEdit(null);
    startTransition(async () => {
      try {
        await updateMemberRoleAction(userId, { role: newRole });
        toast.success("Rol actualizado correctamente.");
      } catch (err) {
        toast.error(err instanceof Error ? err.message : "Error al cambiar el rol.");
      }
    });
  }

  function handleRemove(userId: string) {
    setConfirmId(null);
    setRemovingId(userId);
    startTransition(async () => {
      try {
        await removeMemberAction(userId);
        toast.success("Miembro eliminado del equipo.");
      } catch (err) {
        toast.error(err instanceof Error ? err.message : "Error al eliminar el miembro.");
      } finally {
        setRemovingId(null);
      }
    });
  }

  return (
    <div>
      <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
          <thead>
            <tr className="bg-slate-50 dark:bg-slate-700/50">
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Miembro</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Rol</th>
              <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400 hidden sm:table-cell">Desde</th>
              <th className="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
            {members.map((m) => (
              <tr key={m.userId} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                <td className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <div className="flex h-8 w-8 items-center justify-center rounded-full bg-indigo-100 text-xs font-bold text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300 shrink-0">
                      {initials(m.firstName, m.lastName)}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
                        {m.firstName} {m.lastName}
                        {m.userId === currentUserId && (
                          <span className="ml-1.5 text-xs text-slate-400">(tú)</span>
                        )}
                      </p>
                      <p className="text-xs text-slate-500 dark:text-slate-400">{m.email}</p>
                    </div>
                  </div>
                </td>
                <td className="px-4 py-3">
                  {roleEdit === m.userId && canChangeRole(m) ? (
                    <select
                      defaultValue={m.role}
                      onBlur={(e) => {
                        if (e.target.value !== m.role) handleRoleChange(m.userId, e.target.value);
                        else setRoleEdit(null);
                      }}
                      onChange={(e) => handleRoleChange(m.userId, e.target.value)}
                      autoFocus
                      className="rounded-md border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
                    >
                      <option value="admin">Administrador</option>
                      <option value="member">Miembro</option>
                    </select>
                  ) : (
                    <button
                      onClick={() => canChangeRole(m) && setRoleEdit(m.userId)}
                      className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors ${ROLE_COLORS[m.role] ?? ROLE_COLORS.member} ${canChangeRole(m) ? "cursor-pointer hover:opacity-80" : "cursor-default"}`}
                    >
                      {ROLE_LABELS[m.role] ?? m.role}
                      {canChangeRole(m) && (
                        <svg className="h-2.5 w-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                        </svg>
                      )}
                    </button>
                  )}
                </td>
                <td className="px-4 py-3 hidden sm:table-cell text-sm text-slate-500 dark:text-slate-400">
                  {new Date(m.joinedAtUtc).toLocaleDateString("es-GT")}
                </td>
                <td className="px-4 py-3 text-right">
                  {canRemove(m) && (
                    confirmId === m.userId ? (
                      <div className="flex items-center justify-end gap-2">
                        <span className="text-xs text-slate-500">¿Confirmar?</span>
                        <button
                          onClick={() => handleRemove(m.userId)}
                          disabled={removingId === m.userId}
                          className="rounded-md bg-red-600 px-2 py-1 text-xs font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
                        >
                          {removingId === m.userId ? "…" : "Sí"}
                        </button>
                        <button
                          onClick={() => setConfirmId(null)}
                          className="rounded-md px-2 py-1 text-xs text-slate-500 hover:text-slate-700"
                        >
                          No
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => setConfirmId(m.userId)}
                        disabled={removingId === m.userId}
                        title="Eliminar miembro"
                        className="rounded-md p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600 disabled:opacity-40 dark:hover:bg-red-900/20 dark:hover:text-red-400 transition-colors"
                      >
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M22 10.5h-6m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM4 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 0110.374 21c-2.331 0-4.512-.645-6.374-1.766z" />
                        </svg>
                      </button>
                    )
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

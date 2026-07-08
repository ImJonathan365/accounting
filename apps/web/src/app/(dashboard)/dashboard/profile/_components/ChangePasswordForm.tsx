"use client";

import { useState, useTransition } from "react";
import { changePasswordAction } from "@/lib/actions";
import { ApiError } from "@/lib/api-client";

export function ChangePasswordForm() {
  const [currentPassword,  setCurrentPassword]  = useState("");
  const [newPassword,      setNewPassword]      = useState("");
  const [confirmPassword,  setConfirmPassword]  = useState("");
  const [errors,           setErrors]           = useState<Record<string, string>>({});
  const [success,          setSuccess]          = useState(false);
  const [isPending,        startTransition]     = useTransition();

  function validate() {
    const e: Record<string, string> = {};
    if (!currentPassword) e.currentPassword = "La contraseña actual es requerida.";
    if (!newPassword) {
      e.newPassword = "La nueva contraseña es requerida.";
    } else if (newPassword.length < 8) {
      e.newPassword = "Mínimo 8 caracteres.";
    } else if (!/[A-Z]/.test(newPassword)) {
      e.newPassword = "Debe incluir al menos una mayúscula.";
    } else if (!/[0-9]/.test(newPassword)) {
      e.newPassword = "Debe incluir al menos un número.";
    } else if (newPassword === currentPassword) {
      e.newPassword = "La nueva contraseña no puede ser igual a la actual.";
    }
    if (confirmPassword !== newPassword) e.confirmPassword = "Las contraseñas no coinciden.";
    return e;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const validation = validate();
    if (Object.keys(validation).length > 0) { setErrors(validation); return; }
    setErrors({});
    setSuccess(false);

    startTransition(async () => {
      try {
        await changePasswordAction(currentPassword, newPassword, confirmPassword);
        setSuccess(true);
        setCurrentPassword("");
        setNewPassword("");
        setConfirmPassword("");
      } catch (err) {
        if (err instanceof ApiError) {
          setErrors({ form: err.message });
        } else if (err instanceof Error && !err.message.includes("NEXT_REDIRECT")) {
          setErrors({ form: err.message });
        }
      }
    });
  }

  const field = (id: string, label: string, value: string, onChange: (v: string) => void) => (
    <div>
      <label htmlFor={id} className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
        {label}
      </label>
      <input
        id={id}
        type="password"
        value={value}
        onChange={(e) => { onChange(e.target.value); setErrors((prev) => ({ ...prev, [id]: "" })); setSuccess(false); }}
        autoComplete={id === "currentPassword" ? "current-password" : "new-password"}
        className={`block w-full rounded-lg border px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500 ${
          errors[id]
            ? "border-red-400 focus:border-red-500 focus:ring-red-200 dark:border-red-500 dark:focus:ring-red-900"
            : "border-slate-300 focus:border-indigo-500 focus:ring-indigo-200 dark:border-slate-600 dark:focus:ring-indigo-900"
        }`}
      />
      {errors[id] && <p className="mt-1 text-xs text-red-600 dark:text-red-400">{errors[id]}</p>}
    </div>
  );

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {field("currentPassword", "Contraseña actual",   currentPassword, setCurrentPassword)}
      {field("newPassword",     "Nueva contraseña",    newPassword,     setNewPassword)}
      {field("confirmPassword", "Confirmar contraseña", confirmPassword, setConfirmPassword)}

      {errors.form && (
        <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600 dark:bg-red-900/20 dark:text-red-400">
          {errors.form}
        </p>
      )}

      {success && (
        <p className="rounded-lg bg-green-50 px-3 py-2 text-sm text-green-700 dark:bg-green-900/20 dark:text-green-400">
          Contraseña cambiada correctamente. Te enviamos un correo de confirmación.
        </p>
      )}

      <div className="flex justify-end pt-2">
        <button
          type="submit"
          disabled={isPending}
          className="rounded-lg bg-indigo-600 px-5 py-2 text-sm font-semibold text-white transition hover:bg-indigo-700 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 dark:focus:ring-offset-slate-800"
        >
          {isPending ? "Guardando…" : "Cambiar contraseña"}
        </button>
      </div>
    </form>
  );
}

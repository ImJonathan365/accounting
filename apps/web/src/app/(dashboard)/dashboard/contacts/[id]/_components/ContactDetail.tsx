"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Contact, ContactType, UpdateContactRequest } from "@accounting/types";

const inputClass =
  "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

const TYPE_LABEL: Record<ContactType, string> = {
  Customer: "Cliente",
  Vendor:   "Proveedor",
  Both:     "Ambos",
};

interface Props {
  contact: Contact;
  orgId:   string;
  token:   string;
  canEdit: boolean;
}

function Field({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div>
      <dt className="text-xs font-medium text-slate-500 dark:text-slate-400">{label}</dt>
      <dd className="mt-0.5 text-sm text-slate-900 dark:text-slate-100">{value || "—"}</dd>
    </div>
  );
}

export function ContactDetail({ contact, orgId, token, canEdit }: Props) {
  const router = useRouter();
  const [isEditing, setIsEditing]     = useState(false);
  const [isPending, startTransition]  = useTransition();
  const [error, setError]             = useState<string | null>(null);

  const [type, setType]       = useState<ContactType>(contact.type);
  const [name, setName]       = useState(contact.name);
  const [email, setEmail]     = useState(contact.email ?? "");
  const [phone, setPhone]     = useState(contact.phone ?? "");
  const [address, setAddress] = useState(contact.address ?? "");
  const [notes, setNotes]     = useState(contact.notes ?? "");
  const [isActive, setIsActive] = useState(contact.isActive);

  function resetForm() {
    setType(contact.type);
    setName(contact.name);
    setEmail(contact.email ?? "");
    setPhone(contact.phone ?? "");
    setAddress(contact.address ?? "");
    setNotes(contact.notes ?? "");
    setIsActive(contact.isActive);
    setError(null);
  }

  function validate(): string | null {
    if (!name.trim())          return "El nombre es requerido.";
    if (name.trim().length > 300) return "El nombre no puede superar 300 caracteres.";
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim()))
      return "El email no tiene un formato válido.";
    if (phone.length > 50)     return "El teléfono no puede superar 50 caracteres.";
    if (address.length > 500)  return "La dirección no puede superar 500 caracteres.";
    if (notes.length > 1000)   return "Las notas no pueden superar 1000 caracteres.";
    return null;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const validationError = validate();
    if (validationError) { setError(validationError); return; }
    setError(null);

    const payload: UpdateContactRequest = {
      type,
      name:    name.trim(),
      email:   email.trim() || undefined,
      phone:   phone.trim() || undefined,
      address: address.trim() || undefined,
      notes:   notes.trim() || undefined,
      isActive,
    };

    startTransition(async () => {
      try {
        await apiClient.contacts.update(orgId, contact.id, payload, token);
        toast.success("Contacto actualizado.");
        setIsEditing(false);
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error al guardar.");
      }
    });
  }

  return (
    <div className="space-y-6">
      {/* Back link */}
      <Link
        href="/dashboard/contacts"
        className="inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
      >
        ← Volver a Contactos
      </Link>

      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4 dark:border-slate-700">
          <div>
            <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">{contact.name}</h2>
            <span className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${
              contact.type === "Customer"
                ? "bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300"
                : contact.type === "Vendor"
                ? "bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300"
                : "bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300"
            }`}>
              {TYPE_LABEL[contact.type]}
            </span>
            {!contact.isActive && (
              <span className="ml-2 inline-block rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-500 dark:bg-slate-700 dark:text-slate-400">
                Inactivo
              </span>
            )}
          </div>
          {canEdit && !isEditing && (
            <button
              onClick={() => setIsEditing(true)}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
            >
              Editar
            </button>
          )}
        </div>

        {isEditing ? (
          <form onSubmit={handleSubmit} className="space-y-4 px-6 py-5">
            {error && (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
                {error}
              </p>
            )}

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Tipo</label>
                <select value={type} onChange={e => setType(e.target.value as ContactType)} className={inputClass}>
                  <option value="Customer">Cliente</option>
                  <option value="Vendor">Proveedor</option>
                  <option value="Both">Ambos</option>
                </select>
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
                  Nombre <span className="text-red-500">*</span>
                </label>
                <input value={name} onChange={e => setName(e.target.value)} className={inputClass} />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Email</label>
                <input type="email" value={email} onChange={e => setEmail(e.target.value)} className={inputClass} />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Teléfono</label>
                <input value={phone} onChange={e => setPhone(e.target.value)} className={inputClass} />
              </div>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Dirección</label>
              <input value={address} onChange={e => setAddress(e.target.value)} className={inputClass} />
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Notas</label>
              <textarea
                value={notes}
                onChange={e => setNotes(e.target.value)}
                rows={3}
                className={inputClass}
              />
            </div>

            <div className="flex items-center gap-3">
              <button
                type="button"
                role="switch"
                aria-checked={isActive}
                onClick={() => setIsActive(v => !v)}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none ${
                  isActive ? "bg-indigo-600" : "bg-slate-300 dark:bg-slate-600"
                }`}
              >
                <span className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${isActive ? "translate-x-6" : "translate-x-1"}`} />
              </button>
              <span className="text-sm text-slate-700 dark:text-slate-300">
                {isActive ? "Activo" : "Inactivo"}
              </span>
            </div>

            <div className="flex gap-3 pt-2">
              <button
                type="submit"
                disabled={isPending}
                className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {isPending ? "Guardando…" : "Guardar cambios"}
              </button>
              <button
                type="button"
                onClick={() => { resetForm(); setIsEditing(false); }}
                disabled={isPending}
                className="rounded-lg border border-slate-300 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
              >
                Cancelar
              </button>
            </div>
          </form>
        ) : (
          <dl className="grid grid-cols-1 gap-4 px-6 py-5 sm:grid-cols-2">
            <Field label="Email"     value={contact.email} />
            <Field label="Teléfono"  value={contact.phone} />
            <Field label="Dirección" value={contact.address} />
            <Field label="Estado"    value={contact.isActive ? "Activo" : "Inactivo"} />
            {contact.notes && (
              <div className="sm:col-span-2">
                <dt className="text-xs font-medium text-slate-500 dark:text-slate-400">Notas</dt>
                <dd className="mt-0.5 text-sm text-slate-900 dark:text-slate-100 whitespace-pre-wrap">{contact.notes}</dd>
              </div>
            )}
          </dl>
        )}
      </div>

      {/* Invoices summary */}
      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <div className="flex items-center justify-between px-6 py-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Facturas</h3>
            <p className="text-sm text-slate-500 dark:text-slate-400">
              {contact.invoiceCount === 0
                ? "Sin facturas registradas."
                : `${contact.invoiceCount} factura${contact.invoiceCount !== 1 ? "s" : ""} registrada${contact.invoiceCount !== 1 ? "s" : ""}.`}
            </p>
          </div>
          {contact.invoiceCount > 0 && (
            <Link
              href={`/dashboard/invoices?search=${encodeURIComponent(contact.name)}`}
              className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-700"
            >
              Ver facturas
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}

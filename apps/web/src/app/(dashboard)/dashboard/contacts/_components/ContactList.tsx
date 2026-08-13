"use client";

import { useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { apiClient, ApiError } from "@/lib/api-client";
import type { Contact, ContactType, CreateContactRequest } from "@accounting/types";

const inputClass = "block w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100";

const TYPE_LABEL: Record<ContactType, string> = {
  Customer: "Cliente",
  Vendor:   "Proveedor",
  Both:     "Ambos",
};
const TYPE_COLOR: Record<ContactType, string> = {
  Customer: "bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300",
  Vendor:   "bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300",
  Both:     "bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300",
};

interface Props { contacts: Contact[]; orgId: string; token: string; canEdit: boolean; totalPages: number; currentPage: number; }

function CreateContactModal({ orgId, token, onClose }: { orgId: string; token: string; onClose: () => void }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [type, setType]     = useState<ContactType>("Customer");
  const [name, setName]     = useState("");
  const [email, setEmail]   = useState("");
  const [phone, setPhone]   = useState("");
  const [address, setAddress] = useState("");
  const [error, setError]   = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim()) { setError("El nombre es requerido."); return; }
    startTransition(async () => {
      try {
        await apiClient.contacts.create(orgId, {
          type, name: name.trim(),
          email: email.trim() || undefined, phone: phone.trim() || undefined,
          address: address.trim() || undefined,
        } as CreateContactRequest, token);
        toast.success("Contacto creado.");
        onClose();
        router.refresh();
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Error al crear el contacto.");
      }
    });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <form onSubmit={handleSubmit} className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800 space-y-4">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-100">Nuevo contacto</h2>
        {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">{error}</p>}
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Tipo</label>
          <select value={type} onChange={e => setType(e.target.value as ContactType)} className={inputClass}>
            <option value="Customer">Cliente</option>
            <option value="Vendor">Proveedor</option>
            <option value="Both">Ambos</option>
          </select>
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Nombre <span className="text-red-500">*</span></label>
          <input value={name} onChange={e => setName(e.target.value)} className={inputClass} />
        </div>
        <div className="grid grid-cols-2 gap-3">
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
        <div className="flex gap-3 pt-2">
          <button type="submit" disabled={isPending} className="flex-1 rounded-lg bg-indigo-600 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
            {isPending ? "Guardando…" : "Crear"}
          </button>
          <button type="button" onClick={onClose} className="flex-1 rounded-lg border border-slate-300 py-2 text-sm text-slate-700 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300">
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

export function ContactList({ contacts, orgId, token, canEdit, totalPages, currentPage }: Props) {
  const router = useRouter();
  const [showCreate, setShowCreate]           = useState(false);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [isDeleting, startDeleteTransition]   = useTransition();

  function handleDelete(id: string) {
    startDeleteTransition(async () => {
      try {
        await apiClient.contacts.delete(orgId, id, token);
        toast.success("Contacto eliminado.");
        setConfirmDeleteId(null);
        router.refresh();
      } catch (err) {
        toast.error(err instanceof ApiError ? err.message : "Error al eliminar.");
        setConfirmDeleteId(null);
      }
    });
  }

  return (
    <>
      {showCreate && <CreateContactModal orgId={orgId} token={token} onClose={() => setShowCreate(false)} />}
      <div className="flex justify-end">
        {canEdit && (
          <button onClick={() => setShowCreate(true)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            + Nuevo contacto
          </button>
        )}
      </div>
      {contacts.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500">No hay contactos.</p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
          <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
            <thead className="bg-slate-50 dark:bg-slate-700/50">
              <tr>
                {["Nombre", "Tipo", "Email", "Teléfono", "Facturas", ...(canEdit ? ["Acciones"] : [])].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30">
              {contacts.map(c => (
                <tr key={c.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                  <td className="px-4 py-3">
                    <Link href={`/dashboard/contacts/${c.id}`} className="text-sm font-medium text-slate-900 hover:text-indigo-600 dark:text-slate-100 dark:hover:text-indigo-400">
                      {c.name}
                    </Link>
                    {c.address && <p className="text-xs text-slate-400 truncate max-w-xs">{c.address}</p>}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${TYPE_COLOR[c.type]}`}>
                      {TYPE_LABEL[c.type]}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-sm text-slate-500">{c.email ?? "—"}</td>
                  <td className="px-4 py-3 text-sm text-slate-500">{c.phone ?? "—"}</td>
                  <td className="px-4 py-3 text-sm text-slate-500">{c.invoiceCount}</td>
                  {canEdit && (
                    <td className="px-4 py-3">
                      {confirmDeleteId === c.id ? (
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-slate-600 dark:text-slate-400">¿Eliminar?</span>
                          <button
                            onClick={() => handleDelete(c.id)}
                            disabled={isDeleting}
                            className="rounded px-2 py-1 text-xs font-semibold text-white bg-red-600 hover:bg-red-700 disabled:opacity-50"
                          >
                            Sí
                          </button>
                          <button
                            onClick={() => setConfirmDeleteId(null)}
                            disabled={isDeleting}
                            className="rounded px-2 py-1 text-xs text-slate-600 border border-slate-300 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300"
                          >
                            No
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => setConfirmDeleteId(c.id)}
                          disabled={c.invoiceCount > 0}
                          title={c.invoiceCount > 0 ? "No se puede eliminar: tiene facturas asociadas" : undefined}
                          className="rounded px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-40 dark:text-red-400 dark:hover:bg-red-900/30"
                        >
                          Eliminar
                        </button>
                      )}
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {totalPages > 1 && (
        <div className="flex items-center justify-between pt-1">
          <a
            href={`?page=${currentPage - 1}`}
            className={`rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-700 ${currentPage <= 1 ? "pointer-events-none opacity-40" : ""}`}
          >
            Anterior
          </a>
          <span className="text-xs text-slate-400">{currentPage} / {totalPages}</span>
          <a
            href={`?page=${currentPage + 1}`}
            className={`rounded-lg border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-400 dark:hover:bg-slate-700 ${currentPage >= totalPages ? "pointer-events-none opacity-40" : ""}`}
          >
            Siguiente
          </a>
        </div>
      )}
    </>
  );
}

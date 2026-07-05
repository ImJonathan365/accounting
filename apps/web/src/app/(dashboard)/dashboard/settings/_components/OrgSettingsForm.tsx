"use client";

import { useState } from "react";
import { toast } from "sonner";
import { updateOrgSettingsAction } from "@/lib/actions";
import type { OrgSettings } from "@accounting/types";
import { ReportTheme } from "@accounting/types";

const THEMES: { value: ReportTheme; label: string; description: string; color: string }[] = [
  { value: ReportTheme.Minimal,      label: "Minimalista", description: "Neutro y elegante",       color: "#475569" },
  { value: ReportTheme.Professional, label: "Profesional", description: "Azul corporativo clásico", color: "#4f46e5" },
  { value: ReportTheme.Corporate,    label: "Corporativo",  description: "Verde formal",             color: "#14532d" },
];

interface Props {
  initialSettings: OrgSettings | null;
  canEdit: boolean;
}

export function OrgSettingsForm({ initialSettings, canEdit }: Props) {
  const [form, setForm] = useState({
    companyName:    initialSettings?.companyName    ?? "",
    logoUrl:        initialSettings?.logoUrl        ?? "",
    address:        initialSettings?.address        ?? "",
    taxId:          initialSettings?.taxId          ?? "",
    phone:          initialSettings?.phone          ?? "",
    email:          initialSettings?.email          ?? "",
    currencySymbol: initialSettings?.currencySymbol ?? "$",
    theme:          initialSettings?.theme          ?? ReportTheme.Professional,
  });
  const [saving, setSaving] = useState(false);

  const set = (field: keyof typeof form) =>
    (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm(prev => ({ ...prev, [field]: e.target.value }));

  const fieldCls = canEdit ? inputCls : inputReadOnly;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!canEdit) return;
    setSaving(true);
    try {
      await updateOrgSettingsAction({
        companyName:    form.companyName,
        logoUrl:        form.logoUrl   || null,
        address:        form.address   || null,
        taxId:          form.taxId     || null,
        phone:          form.phone     || null,
        email:          form.email     || null,
        currencySymbol: form.currencySymbol,
        theme:          form.theme,
      });
      toast.success("Configuración guardada.");
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : "Error al guardar la configuración.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      {/* Company info */}
      <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h3 className="mb-4 font-semibold text-slate-900 dark:text-slate-100">Información de la empresa</h3>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Nombre de la empresa *">
            <input value={form.companyName} onChange={set("companyName")} required maxLength={200}
              disabled={!canEdit} className={fieldCls} placeholder="Comercial Gómez S.A." />
          </Field>
          <Field label="NIT / RUC / Cédula">
            <input value={form.taxId} onChange={set("taxId")} maxLength={50}
              disabled={!canEdit} className={fieldCls} placeholder="900.123.456-7" />
          </Field>
          <Field label="Dirección">
            <input value={form.address} onChange={set("address")} maxLength={300}
              disabled={!canEdit} className={fieldCls} placeholder="Calle 45 #12, Bogotá" />
          </Field>
          <Field label="Teléfono">
            <input value={form.phone} onChange={set("phone")} maxLength={50}
              disabled={!canEdit} className={fieldCls} placeholder="+57 300 000 0000" />
          </Field>
          <Field label="Email de contacto">
            <input value={form.email} onChange={set("email")} type="email" maxLength={200}
              disabled={!canEdit} className={fieldCls} placeholder="info@empresa.com" />
          </Field>
          <Field label="URL del logo">
            <input value={form.logoUrl} onChange={set("logoUrl")} maxLength={500}
              disabled={!canEdit} className={fieldCls} placeholder="https://..." />
          </Field>
        </div>
      </section>

      {/* Export settings */}
      <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h3 className="mb-4 font-semibold text-slate-900 dark:text-slate-100">Opciones de exportación</h3>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Símbolo de moneda *">
            <input value={form.currencySymbol} onChange={set("currencySymbol")} required maxLength={10}
              disabled={!canEdit} className={fieldCls} placeholder="$" />
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">Ej: $, €, S/, Q, ₡</p>
          </Field>
        </div>

        <div className="mt-6">
          <p className="mb-3 text-sm font-medium text-slate-700 dark:text-slate-300">Tema visual de reportes</p>
          <div className="grid gap-3 sm:grid-cols-3">
            {THEMES.map(t => (
              <button
                key={t.value}
                type="button"
                disabled={!canEdit}
                onClick={() => canEdit && setForm(prev => ({ ...prev, theme: t.value }))}
                className={[
                  "flex items-center gap-3 rounded-lg border p-4 text-left transition-all",
                  !canEdit ? "cursor-default opacity-70" : "",
                  form.theme === t.value
                    ? "border-indigo-500 bg-indigo-50 dark:bg-indigo-950/30"
                    : canEdit ? "border-slate-200 hover:border-slate-300 dark:border-slate-700 dark:hover:border-slate-600"
                              : "border-slate-200 dark:border-slate-700",
                ].join(" ")}
              >
                <div className="h-8 w-8 shrink-0 rounded-md" style={{ background: t.color }} />
                <div>
                  <p className="text-sm font-medium text-slate-900 dark:text-slate-100">{t.label}</p>
                  <p className="text-xs text-slate-500 dark:text-slate-400">{t.description}</p>
                </div>
                {form.theme === t.value && (
                  <div className="ml-auto h-4 w-4 rounded-full border-2 border-indigo-500 bg-indigo-500" />
                )}
              </button>
            ))}
          </div>
        </div>
      </section>

      {/* Actions — only shown for owners */}
      {canEdit && (
        <div className="flex justify-end">
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-indigo-600 px-5 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors"
          >
            {saving ? "Guardando…" : "Guardar cambios"}
          </button>
        </div>
      )}
    </form>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
        {label}
      </label>
      {children}
    </div>
  );
}

const inputCls =
  "w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100 dark:placeholder-slate-500";

const inputReadOnly =
  "w-full rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600 cursor-default dark:border-slate-700 dark:bg-slate-800/50 dark:text-slate-400";

import { redirect, notFound } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient, ApiError } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import { InvoiceActions } from "./_components/InvoiceActions";

export const dynamic = "force-dynamic";

const STATUS_STYLE: Record<string, string> = {
  Draft:         "bg-slate-100 text-slate-600",
  Issued:        "bg-blue-100 text-blue-700",
  PartiallyPaid: "bg-amber-100 text-amber-700",
  Paid:          "bg-emerald-100 text-emerald-700",
  Void:          "bg-red-100 text-red-600",
};

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

export default async function InvoiceDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const { id } = await params;
  const canEdit = role === "owner" || role === "admin";

  try {
    const [inv, accounts] = await Promise.all([
      apiClient.invoices.getById(orgId, id, token),
      apiClient.accounts.list(orgId, token),
    ]);

    const paymentAccounts = accounts.filter(a => a.isPostable && a.type === "Asset");

    return (
      <div className="space-y-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
                Factura {inv.number}
              </h1>
              <span className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${STATUS_STYLE[inv.status]}`}>
                {inv.statusLabel}
              </span>
              <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600 dark:bg-slate-700 dark:text-slate-300">
                {inv.type === "Receivable" ? "Por cobrar" : "Por pagar"}
              </span>
            </div>
            <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{inv.contactName}</p>
          </div>
          {canEdit && <InvoiceActions invoice={inv} orgId={orgId} token={token} paymentAccounts={paymentAccounts} />}
        </div>

        <div className="grid gap-4 sm:grid-cols-4">
          {[
            { label: "Total", value: fmt(inv.total) },
            { label: "Pagado", value: fmt(inv.paid) },
            { label: "Pendiente", value: fmt(inv.balance) },
            { label: "Vence", value: inv.dueDate },
          ].map(({ label, value }) => (
            <div key={label} className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-800">
              <p className="text-xs text-slate-500 dark:text-slate-400">{label}</p>
              <p className="mt-1 text-xl font-semibold text-slate-900 dark:text-slate-100 tabular-nums">{value}</p>
            </div>
          ))}
        </div>

        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
          <div className="bg-slate-50 px-6 py-3 dark:bg-slate-800/60">
            <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Líneas</h2>
          </div>
          <table className="w-full text-sm">
            <thead className="border-b border-slate-100 dark:border-slate-700">
              <tr className="text-left text-xs text-slate-400">
                <th className="px-6 py-2 font-medium">Descripción</th>
                <th className="px-6 py-2 font-medium">Cuenta</th>
                <th className="px-6 py-2 text-right font-medium">Cant.</th>
                <th className="px-6 py-2 text-right font-medium">Precio</th>
                <th className="px-6 py-2 text-right font-medium">Subtotal</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-50 dark:divide-slate-700/50">
              {inv.lines.map(l => (
                <tr key={l.id}>
                  <td className="px-6 py-3 text-slate-700 dark:text-slate-300">{l.description}</td>
                  <td className="px-6 py-3 text-xs text-slate-500">{l.accountCode} — {l.accountName}</td>
                  <td className="px-6 py-3 text-right tabular-nums">{l.quantity}</td>
                  <td className="px-6 py-3 text-right tabular-nums">{fmt(l.unitPrice)}</td>
                  <td className="px-6 py-3 text-right font-medium tabular-nums">{fmt(l.subtotal)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/60">
                <td colSpan={4} className="px-6 py-3 text-right font-semibold text-slate-900 dark:text-slate-100">Total</td>
                <td className="px-6 py-3 text-right font-bold tabular-nums text-slate-900 dark:text-slate-100">{fmt(inv.total)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        {inv.payments.length > 0 && (
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
            <div className="bg-slate-50 px-6 py-3 dark:bg-slate-800/60">
              <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Pagos registrados</h2>
            </div>
            <table className="w-full text-sm">
              <tbody className="divide-y divide-slate-50 dark:divide-slate-700/50">
                {inv.payments.map(p => (
                  <tr key={p.id}>
                    <td className="px-6 py-3 text-slate-700 dark:text-slate-300">{p.date}</td>
                    <td className="px-6 py-3 text-slate-500">{p.paymentAccountName}</td>
                    <td className="px-6 py-3 text-slate-500 text-xs">{p.notes}</td>
                    <td className="px-6 py-3 text-right font-semibold tabular-nums text-emerald-600 dark:text-emerald-400">{fmt(p.amount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    );
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) notFound();
    return <PageError message="No se pudo cargar la factura." />;
  }
}

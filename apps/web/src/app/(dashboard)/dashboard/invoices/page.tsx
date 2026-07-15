import Link from "next/link";
import { redirect } from "next/navigation";
import { getServerToken, getCurrentOrgId, getCurrentUserRole } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { PageError } from "@/components/PageError";
import type { Invoice, InvoiceType, InvoiceStatus } from "@accounting/types";

export const dynamic = "force-dynamic";

const PAGE_SIZE = 25;

const STATUS_STYLE: Record<string, string> = {
  Draft:         "bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-300",
  Issued:        "bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300",
  PartiallyPaid: "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  Paid:          "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  Void:          "bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-300",
};

function fmt(n: number) {
  return n.toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

const STATUS_OPTIONS: { label: string; value: InvoiceStatus | "" }[] = [
  { label: "Todos los estados", value: "" },
  { label: "Borrador",          value: "Draft" },
  { label: "Emitida",           value: "Issued" },
  { label: "Parcial",           value: "PartiallyPaid" },
  { label: "Pagada",            value: "Paid" },
  { label: "Anulada",           value: "Void" },
];

export default async function InvoicesPage({
  searchParams,
}: {
  searchParams: Promise<{ type?: string; status?: string; search?: string; from?: string; to?: string; page?: string }>;
}) {
  const [token, orgId, role] = await Promise.all([
    getServerToken(), getCurrentOrgId(), getCurrentUserRole(),
  ]);
  if (!token || !orgId) redirect("/login");

  const sp      = await searchParams;
  const type    = (sp.type as InvoiceType) || undefined;
  const status  = (sp.status as InvoiceStatus) || undefined;
  const search  = sp.search || undefined;
  const from    = sp.from   || undefined;
  const to      = sp.to     || undefined;
  const page    = Math.max(1, parseInt(sp.page ?? "1", 10) || 1);
  const canEdit = role === "owner" || role === "admin";

  let invoices: Invoice[] = [];
  let total = 0;
  let totalPages = 1;

  try {
    const result = await apiClient.invoices.list(orgId, token, type, status, search, from, to, page, PAGE_SIZE);
    invoices   = result.items;
    total      = result.total;
    totalPages = result.totalPages;
  } catch {
    return <PageError message="No se pudo cargar las facturas." />;
  }

  const tabs = [
    { label: "Todas",        value: undefined   },
    { label: "Por cobrar",   value: "Receivable" },
    { label: "Por pagar",    value: "Payable"    },
  ];

  function buildUrl(overrides: Record<string, string | undefined>, resetPage = true) {
    const params = new URLSearchParams();
    const vals = { type, status, search, from, to, page: resetPage ? undefined : String(page), ...overrides };
    Object.entries(vals).forEach(([k, v]) => { if (v) params.set(k, v); });
    const qs = params.toString();
    return `/dashboard/invoices${qs ? `?${qs}` : ""}`;
  }

  function pageUrl(p: number) {
    return buildUrl({ page: String(p) }, false);
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">Facturas</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Cuentas por cobrar y por pagar.
          </p>
        </div>
        {canEdit && (
          <Link href="/dashboard/invoices/new" className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
            + Nueva factura
          </Link>
        )}
      </div>

      {/* Type tabs */}
      <div className="flex gap-1 rounded-xl border border-slate-200 bg-slate-50 p-1 dark:border-slate-700 dark:bg-slate-800/50 w-fit">
        {tabs.map((tab) => (
          <Link
            key={tab.label}
            href={buildUrl({ type: tab.value, page: undefined })}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
              type === tab.value
                ? "bg-white text-slate-900 shadow-sm dark:bg-slate-700 dark:text-slate-100"
                : "text-slate-500 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100"
            }`}
          >
            {tab.label}
          </Link>
        ))}
      </div>

      {/* Filters */}
      <form method="GET" action="/dashboard/invoices" className="flex flex-wrap gap-2 items-center">
        {type   && <input type="hidden" name="type"   value={type} />}
        <input
          name="search"
          defaultValue={search ?? ""}
          placeholder="Número o contacto…"
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
        />
        <select
          name="status"
          defaultValue={status ?? ""}
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
        >
          {STATUS_OPTIONS.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        <input
          type="date"
          name="from"
          defaultValue={from ?? ""}
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
        />
        <span className="text-slate-400 text-sm">–</span>
        <input
          type="date"
          name="to"
          defaultValue={to ?? ""}
          className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
        />
        <button type="submit" className="rounded-lg bg-slate-800 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-700 dark:bg-slate-600 dark:hover:bg-slate-500">
          Filtrar
        </button>
        {(search || status || from || to) && (
          <Link href={buildUrl({ search: undefined, status: undefined, from: undefined, to: undefined })} className="text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200">
            Limpiar filtros
          </Link>
        )}
      </form>

      {invoices.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-white py-16 text-center dark:border-slate-700 dark:bg-slate-800">
          <p className="text-sm text-slate-500">No hay facturas.</p>
          {canEdit && (
            <Link href="/dashboard/invoices/new" className="mt-3 inline-block rounded-lg bg-indigo-600 px-4 py-2 text-xs font-semibold text-white hover:bg-indigo-700">
              + Crear primera factura
            </Link>
          )}
        </div>
      ) : (
        <>
          <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
            <table className="min-w-full divide-y divide-slate-100 dark:divide-slate-700/50">
              <thead className="bg-slate-50 dark:bg-slate-700/50">
                <tr>
                  {["#", "Contacto", "Fecha", "Vence", "Total", "Pendiente", "Estado", ""].map(h => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-400">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50 dark:divide-slate-700/30">
                {invoices.map((inv) => {
                  const overdue = inv.status !== "Paid" && inv.status !== "Void" && inv.dueDate < new Date().toISOString().slice(0,10);
                  return (
                    <tr key={inv.id} className="hover:bg-slate-50 dark:hover:bg-slate-700/30 transition-colors">
                      <td className="px-4 py-3">
                        <span className="font-mono text-sm text-slate-700 dark:text-slate-300">{inv.number}</span>
                        <span className="ml-2 rounded-full bg-slate-100 px-1.5 py-0.5 text-xs text-slate-500 dark:bg-slate-700 dark:text-slate-400">
                          {inv.type === "Receivable" ? "CxC" : "CxP"}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">{inv.contactName}</td>
                      <td className="px-4 py-3 text-sm text-slate-500">{inv.date}</td>
                      <td className={`px-4 py-3 text-sm ${overdue ? "font-semibold text-red-600 dark:text-red-400" : "text-slate-500"}`}>{inv.dueDate}</td>
                      <td className="px-4 py-3 text-right font-mono text-sm text-slate-900 dark:text-slate-100">{fmt(inv.total)}</td>
                      <td className={`px-4 py-3 text-right font-mono text-sm ${inv.balance > 0 ? "text-amber-600 dark:text-amber-400" : "text-slate-400"}`}>{fmt(inv.balance)}</td>
                      <td className="px-4 py-3">
                        <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLE[inv.status]}`}>
                          {inv.statusLabel}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <Link href={`/dashboard/invoices/${inv.id}`} className="text-xs text-indigo-600 hover:underline dark:text-indigo-400">Ver →</Link>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between text-sm text-slate-500 dark:text-slate-400">
              <span>{total} facturas · página {page} de {totalPages}</span>
              <div className="flex gap-2">
                {page > 1 && (
                  <Link href={pageUrl(page - 1)} className="rounded-lg border border-slate-200 px-3 py-1.5 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800">
                    ← Anterior
                  </Link>
                )}
                {page < totalPages && (
                  <Link href={pageUrl(page + 1)} className="rounded-lg border border-slate-200 px-3 py-1.5 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800">
                    Siguiente →
                  </Link>
                )}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

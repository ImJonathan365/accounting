"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";

interface NavItem { href: string; label: string; }

interface NavGroup {
  label: string;
  items: NavItem[];
}

function useGroups(userRole?: string): Array<{ label: string; href?: string; exact?: boolean; items?: NavItem[] }> {
  const isAdmin = userRole === "owner" || userRole === "admin";
  const isOwner = userRole === "owner";

  return [
    { label: "Inicio", href: "/dashboard", exact: true },
    {
      label: "Contabilidad",
      items: [
        { href: "/dashboard/accounts",          label: "Cuentas" },
        { href: "/dashboard/journal",           label: "Diario" },
        ...(isAdmin ? [{ href: "/dashboard/opening-balances", label: "Saldos iniciales" }] : []),
        ...(isAdmin ? [{ href: "/dashboard/periods",          label: "Períodos" }] : []),
        ...(isOwner ? [{ href: "/dashboard/year-end",         label: "Cierre anual" }] : []),
      ],
    },
    {
      label: "Facturación",
      items: [
        { href: "/dashboard/contacts",  label: "Contactos" },
        { href: "/dashboard/invoices",  label: "Facturas" },
        { href: "/dashboard/products",  label: "Productos" },
        { href: "/dashboard/tax-rates", label: "Impuestos" },
      ],
    },
    {
      label: "Finanzas",
      items: [
        { href: "/dashboard/bank",    label: "Banco" },
        { href: "/dashboard/budgets", label: "Presupuesto" },
        { href: "/dashboard/reports", label: "Reportes" },
      ],
    },
    {
      label: "Organización",
      items: [
        { href: "/dashboard/team",  label: "Equipo" },
        ...(isOwner ? [{ href: "/dashboard/audit", label: "Auditoría" }] : []),
      ],
    },
  ];
}

function DropdownMenu({ label, items }: NavGroup) {
  const pathname  = usePathname();
  const [open, setOpen] = useState(false);
  const ref       = useRef<HTMLDivElement>(null);
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const isGroupActive = items.some(i => pathname.startsWith(i.href));

  function handleMouseEnter() {
    if (closeTimer.current) clearTimeout(closeTimer.current);
    setOpen(true);
  }
  function handleMouseLeave() {
    closeTimer.current = setTimeout(() => setOpen(false), 120);
  }

  useEffect(() => {
    setOpen(false);
  }, [pathname]);

  useEffect(() => () => { if (closeTimer.current) clearTimeout(closeTimer.current); }, []);

  return (
    <div
      ref={ref}
      className="relative"
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <button
        className={`flex items-center gap-1 rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
          isGroupActive
            ? "bg-indigo-50 text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-300"
            : "text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-700 dark:hover:text-slate-100"
        }`}
      >
        {label}
        <svg
          className={`h-3.5 w-3.5 transition-transform duration-150 ${open ? "rotate-180" : ""}`}
          fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {open && (
        <div className="absolute left-0 top-full z-20 mt-1 min-w-40 overflow-hidden rounded-xl border border-slate-200 bg-white py-1 shadow-lg dark:border-slate-700 dark:bg-slate-800">
          {items.map(({ href, label }) => {
            const active = pathname.startsWith(href);
            return (
              <Link
                key={href}
                href={href}
                className={`block px-4 py-2 text-sm transition-colors ${
                  active
                    ? "bg-indigo-50 font-medium text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-300"
                    : "text-slate-700 hover:bg-slate-50 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-slate-700/60 dark:hover:text-slate-100"
                }`}
              >
                {label}
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}

export function NavLinks({ userRole }: { userRole?: string }) {
  const pathname = usePathname();
  const groups   = useGroups(userRole);

  return (
    <nav className="hidden items-center gap-0.5 sm:flex">
      {groups.map((g) => {
        if (g.href) {
          const active = g.exact ? pathname === g.href : pathname.startsWith(g.href!);
          return (
            <Link
              key={g.href}
              href={g.href}
              className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
                active
                  ? "bg-indigo-50 text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-300"
                  : "text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-700 dark:hover:text-slate-100"
              }`}
            >
              {g.label}
            </Link>
          );
        }
        return <DropdownMenu key={g.label} label={g.label} items={g.items!} />;
      })}
    </nav>
  );
}

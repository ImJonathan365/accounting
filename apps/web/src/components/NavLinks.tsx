"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const BASE_LINKS = [
  { href: "/dashboard",          label: "Inicio",    exact: true  },
  { href: "/dashboard/accounts", label: "Cuentas",   exact: false },
  { href: "/dashboard/journal",  label: "Diario",    exact: false },
  { href: "/dashboard/reports",  label: "Reportes",  exact: false },
  { href: "/dashboard/team",     label: "Equipo",    exact: false },
];

export function NavLinks({ userRole }: { userRole?: string }) {
  const pathname = usePathname();

  const links = [
    ...BASE_LINKS,
    ...(userRole === "owner"
      ? [{ href: "/dashboard/audit", label: "Auditoría", exact: false }]
      : []),
  ];

  return (
    <nav className="hidden items-center gap-1 sm:flex">
      {links.map(({ href, label, exact }) => {
        const isActive = exact ? pathname === href : pathname.startsWith(href);
        return (
          <Link
            key={href}
            href={href}
            className={`rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
              isActive
                ? "bg-indigo-50 text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-300"
                : "text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-slate-700 dark:hover:text-slate-100"
            }`}
          >
            {label}
          </Link>
        );
      })}
    </nav>
  );
}

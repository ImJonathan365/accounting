import Link from "next/link";
import { getDisplayName } from "@/lib/auth";
import { ThemeToggle } from "@/components/ThemeToggle";
import { UserMenu } from "@/components/UserMenu";
import { NavLinks } from "@/components/NavLinks";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const displayName = await getDisplayName();
  const initials = displayName
    ? displayName.split(" ").map((n) => n[0]).join("").slice(0, 2).toUpperCase()
    : "?";

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 backdrop-blur-sm dark:border-slate-700 dark:bg-slate-800/90">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:px-6">
          <div className="flex items-center gap-6">
            <Link href="/dashboard" className="flex items-center gap-2">
              <div className="flex h-7 w-7 items-center justify-center rounded-md bg-indigo-600 text-xs font-bold text-white">A</div>
              <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">Accounting</span>
            </Link>
            <NavLinks />
          </div>

          <div className="flex items-center gap-2">
            <ThemeToggle />
            <div className="h-5 w-px bg-slate-200 dark:bg-slate-700" />
            <UserMenu displayName={displayName} initials={initials} />
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">{children}</main>
    </div>
  );
}

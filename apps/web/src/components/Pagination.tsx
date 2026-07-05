import Link from "next/link";

interface Props {
  page: number;
  totalPages: number;
  total: number;
  pageSize: number;
  buildHref: (page: number) => string;
}

export function Pagination({ page, totalPages, total, pageSize, buildHref }: Props) {
  if (totalPages <= 1) return null;

  const from = (page - 1) * pageSize + 1;
  const to   = Math.min(page * pageSize, total);

  const pages: (number | "...")[] = [];
  if (totalPages <= 7) {
    for (let i = 1; i <= totalPages; i++) pages.push(i);
  } else {
    pages.push(1);
    if (page > 3) pages.push("...");
    for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) pages.push(i);
    if (page < totalPages - 2) pages.push("...");
    pages.push(totalPages);
  }

  const linkBase = "flex h-8 min-w-8 items-center justify-center rounded-lg px-2 text-sm font-medium transition-colors";
  const active   = `${linkBase} bg-indigo-600 text-white`;
  const inactive = `${linkBase} text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700`;
  const disabled = `${linkBase} cursor-not-allowed text-slate-300 dark:text-slate-600`;

  return (
    <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
      <p className="text-xs text-slate-500 dark:text-slate-400">
        Mostrando {from}–{to} de {total} asiento{total !== 1 ? "s" : ""}
      </p>

      <div className="flex items-center gap-1">
        {page > 1 ? (
          <Link href={buildHref(page - 1)} className={inactive} aria-label="Anterior">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
          </Link>
        ) : (
          <span className={disabled} aria-label="Anterior">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
          </span>
        )}

        {pages.map((p, i) =>
          p === "..." ? (
            <span key={`ellipsis-${i}`} className="px-1 text-slate-400">…</span>
          ) : (
            <Link
              key={p}
              href={buildHref(p)}
              className={p === page ? active : inactive}
              aria-current={p === page ? "page" : undefined}
            >
              {p}
            </Link>
          )
        )}

        {page < totalPages ? (
          <Link href={buildHref(page + 1)} className={inactive} aria-label="Siguiente">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
            </svg>
          </Link>
        ) : (
          <span className={disabled} aria-label="Siguiente">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
            </svg>
          </span>
        )}
      </div>
    </div>
  );
}

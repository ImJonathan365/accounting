"use client";

interface Props {
  message?: string;
}

export function PageError({ message = "No se pudo cargar la información. Intenta de nuevo." }: Props) {
  return (
    <div className="flex min-h-[40vh] flex-col items-center justify-center gap-3 text-center">
      <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-50 dark:bg-red-900/20">
        <svg className="h-6 w-6 text-red-500 dark:text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
        </svg>
      </div>
      <p className="text-sm font-medium text-slate-700 dark:text-slate-300">{message}</p>
      <button
        onClick={() => window.location.reload()}
        className="text-sm text-indigo-600 underline-offset-2 hover:underline dark:text-indigo-400"
      >
        Reintentar
      </button>
    </div>
  );
}

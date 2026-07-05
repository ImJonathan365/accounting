"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { postJournalEntryAction } from "@/lib/actions";

export function PostButton({ entryId }: { entryId: string }) {
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  async function handlePost() {
    setLoading(true);
    try {
      await postJournalEntryAction(entryId);
      router.refresh();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Error al registrar el asiento.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <button
      onClick={handlePost}
      disabled={loading}
      className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 transition-colors"
    >
      {loading && (
        <span className="inline-block h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />
      )}
      {loading ? "Registrando…" : "Registrar asiento"}
    </button>
  );
}

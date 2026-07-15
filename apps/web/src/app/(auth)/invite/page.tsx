import { getServerToken } from "@/lib/auth";
import { apiClient } from "@/lib/api-client";
import { InviteClient } from "./_components/InviteClient";
import type { InvitationInfo } from "@accounting/types";

interface Props {
  searchParams: { token?: string };
}

export default async function InvitePage({ searchParams }: Props) {
  const rawToken       = searchParams.token ?? "";
  const isAuthenticated = !!(await getServerToken());

  let initialInfo: InvitationInfo | null = null;
  let fetchError = false;

  if (rawToken) {
    try {
      initialInfo = await apiClient.invitations.getInfo(rawToken);
    } catch {
      fetchError = true;
    }
  }

  return (
    <InviteClient
      rawToken={rawToken}
      isAuthenticated={isAuthenticated}
      initialInfo={initialInfo}
      fetchError={fetchError}
    />
  );
}

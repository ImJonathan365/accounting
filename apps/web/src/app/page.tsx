import { redirect } from "next/navigation";
import { getServerToken } from "@/lib/auth";

export const dynamic = "force-dynamic";

export default async function RootPage() {
  const token = await getServerToken();
  redirect(token ? "/dashboard" : "/login");

}

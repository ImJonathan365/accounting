import { NextRequest, NextResponse } from "next/server";
import { cookies } from "next/headers";
import { apiBaseUrl } from "@/lib/env";

export async function GET(req: NextRequest) {
  const cookieStore = await cookies();
  const token = cookieStore.get("auth_token")?.value;
  if (!token) return new NextResponse(null, { status: 401 });

  const backendPath = req.nextUrl.searchParams.get("path");
  if (!backendPath || !backendPath.startsWith("/api/organizations/")) {
    return new NextResponse(null, { status: 400 });
  }

  const backendRes = await fetch(`${apiBaseUrl}${backendPath}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!backendRes.ok) {
    return new NextResponse(null, { status: backendRes.status });
  }

  const headers = new Headers();
  const ct = backendRes.headers.get("content-type");
  const cd = backendRes.headers.get("content-disposition");
  if (ct) headers.set("Content-Type", ct);
  if (cd) headers.set("Content-Disposition", cd);

  return new NextResponse(backendRes.body, { headers });
}

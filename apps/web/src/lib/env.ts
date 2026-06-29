import { z } from "zod";

const schema = z.object({
  NEXT_PUBLIC_API_URL: z.string().url(),
  API_INTERNAL_URL: z.string().url().optional(),
});

export const env = schema.parse({
  NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL,
  API_INTERNAL_URL: process.env.API_INTERNAL_URL ?? process.env.NEXT_PUBLIC_API_URL,
});

export const apiBaseUrl =
  typeof window === "undefined"
    ? (env.API_INTERNAL_URL ?? env.NEXT_PUBLIC_API_URL)
    : env.NEXT_PUBLIC_API_URL;

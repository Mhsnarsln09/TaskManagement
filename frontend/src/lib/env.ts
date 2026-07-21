import { z } from "zod";

// NEXT_PUBLIC_* değişkenleri build sırasında satır içine gömülür; bu yüzden
// process.env erişimi doğrudan yapılır, dinamik anahtar kullanılamaz.
const schema = z.object({
  NEXT_PUBLIC_API_BASE_URL: z
    .string()
    .url()
    .default("http://localhost:8080")
    .transform((url) => url.replace(/\/+$/, "")),
});

export const env = schema.parse({
  NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL,
});

export const apiBaseUrl = env.NEXT_PUBLIC_API_BASE_URL;
export const notificationsHubUrl = `${apiBaseUrl}/hubs/notifications`;

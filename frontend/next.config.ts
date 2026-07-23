import type { NextConfig } from "next";

// F10-08: Tarayıcı güvenlik tabanı. CSP + güvenlik başlıkları tüm yanıtlara eklenir.
//
// connect-src, API origin'ini (farklı port: 8080) ve SignalR websocket'ini (ws/wss)
// içermek zorundadır; aksi halde CSP tüm API çağrılarını ve gerçek zamanlı bildirimleri
// engellerdi. Origin build zamanında NEXT_PUBLIC_API_BASE_URL'den türetilir.
const apiBaseUrl = (
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080"
).replace(/\/+$/, "");

let apiOrigin = "http://localhost:8080";
try {
  apiOrigin = new URL(apiBaseUrl).origin;
} catch {
  // Geçersiz URL: güvenli varsayılan kalır.
}
// http -> ws, https -> wss (SignalR taşıması).
const apiWsOrigin = apiOrigin.replace(/^http/, "ws");

const isDev = process.env.NODE_ENV !== "production";

// Not: script-src/style-src 'unsafe-inline' bilinçli bir MVP ödünüdür — Next.js App
// Router bootstrap inline script'i ve Tailwind/shadcn inline style'ları enjekte eder.
// Nonce + 'strict-dynamic' tabanlı sıkı CSP (dinamik render gerektirir) MVP sonrası
// sertleştirme adımıdır (bkz. README güvenlik notu). 'upgrade-insecure-requests'
// eklenmez: yerel/compose API'si http olduğu için tüm API çağrılarını bozardı.
const contentSecurityPolicy = [
  "default-src 'self'",
  `script-src 'self' 'unsafe-inline'${isDev ? " 'unsafe-eval'" : ""}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob:",
  "font-src 'self'",
  `connect-src 'self' ${apiOrigin} ${apiWsOrigin}`,
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "frame-ancestors 'none'",
].join("; ");

const securityHeaders = [
  { key: "Content-Security-Policy", value: contentSecurityPolicy },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  { key: "X-Frame-Options", value: "DENY" },
  {
    key: "Permissions-Policy",
    value: "camera=(), microphone=(), geolocation=(), interest-cohort=()",
  },
];

const nextConfig: NextConfig = {
  async headers() {
    return [
      {
        source: "/:path*",
        headers: securityHeaders,
      },
    ];
  },
};

export default nextConfig;

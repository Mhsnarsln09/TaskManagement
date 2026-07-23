# TaskManagement Frontend

TaskManagement web istemcisi: Next.js App Router, TypeScript, Tailwind CSS,
shadcn/ui ve Lucide icons. Ürün kapsamı, entegrasyon sözleşmesi ve görevler
[docs](docs/README.md) altındadır; tasarım-sözleşme kararları için
[docs/DESIGN-DECISIONS.md](docs/DESIGN-DECISIONS.md).

## Gereksinimler

- Node.js 22+
- Çalışan backend API (`http://localhost:8080`) — bkz. `../backend`
- Docker ile tüm stack için `../backend/compose.yml`

## Kurulum ve geliştirme

```bash
npm install
cp .env.example .env.local   # gerekirse NEXT_PUBLIC_API_BASE_URL değiştirin
npm run dev                  # http://localhost:3000
```

Backend'i yerelde çalıştırmak için (Docker ile Postgres + Redis):

```bash
cd ../backend
docker compose up -d postgres redis
dotnet run --project TaskManagement.Api   # .env'deki bağlantı bilgileriyle
```

Frontend'i de Docker'dan kaldırmak için aynı compose dosyasında `frontend`
servisini hedefleyin:

```bash
cd ../backend
cp .env.example .env
docker compose up -d --build frontend
```

Bu komut frontend'i başlatırken API, Postgres ve Redis servislerini de
zincirleme başlatır.

## Komutlar

| Komut | Açıklama |
| --- | --- |
| `npm run dev` | Geliştirme sunucusu |
| `npm run build` | Production build |
| `npm run lint` | ESLint |
| `npm run typecheck` | `tsc --noEmit` |
| `npm test` | Vitest (unit/component) |
| `npm run test:e2e` | Playwright E2E (API mock'lu, backend gerektirmez) |
| `npm run format` | Prettier |
| `npm run openapi` | Canlı API'den şemayı çekip tipleri yeniden üretir |

## Testler

- **Unit/component:** Vitest + Testing Library (`src/**/*.test.ts(x)`).
- **E2E:** Playwright (`e2e/`). API, `e2e/fixtures/api-mock.ts` içindeki route
  mock'larıyla taklit edilir; sözleşme şekli `src/lib/api/types.ts` tiplerine
  bağlıdır, böylece backend sözleşmesi değişince fixture'lar derlemede kırılır.
  Masaüstü spec'leri `chromium`, mobil varyantlar `e2e/mobile.spec.ts` içinde
  `mobile` projesinde çalışır. Testler kendi sunucusunu **3100** portunda
  başlatır; 3000'deki geliştirme sunucusu açıkken de sorunsuz çalışır. İlk
  çalıştırmadan önce: `npx playwright install chromium`.

## OpenAPI istemcisi

Tipler `src/lib/api/schema.d.ts` dosyasına `openapi-typescript` ile üretilir ve
**elle düzenlenmez**. Backend sözleşmesi değiştiğinde API'yi çalıştırıp:

```bash
npm run openapi
```

`scripts/normalize-openapi.mjs`, .NET üreticisinin sayısal alanlar için
yayınladığı `integer|string` birleşimlerini tip üretiminden önce sadeleştirir.

## Mimari özeti

- `src/lib/api` — üretilmiş tipler, `apiFetch` (Problem Details + tekil refresh
  kuyruğu), endpoint modülleri.
- `src/lib/auth` — token store (localStorage) ve `AuthProvider`/rota koruması.
- `src/lib/realtime` — SignalR bildirim hub'ı (reconnect sonrası liste yenilenir).
- `src/lib/user-directory.ts` — GUID→ad eşlemesi (DESIGN-DECISIONS.md §1).
- `src/components/ui` — shadcn primitifleri; `src/components/shared` — ortak
  kompozitler (ProblemDetailsAlert, StatusBadge, UserDisplay…).
- `src/features/*` — özellik bileşenleri; `src/app` rotaları yalnız kompozisyon.

Dağıtım adımları için [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Güvenlik notu

**Token saklama (XSS riski ve geçiş planı — F10-08):** MVP'de access/refresh token'lar
`localStorage`'ta tutulur; API token'ı JSON gövdesinde döndürür (bkz.
docs/FRONTEND-INTEGRATION.md). `localStorage` JavaScript'ten okunabilir olduğu için bir
XSS açığı token'ların çalınmasına yol açabilir. Bu risk, CSP tabanı (aşağıda) ve girdi
kaçışıyla azaltılır ama tamamen kaldırılmaz. **Geçiş planı:** production sertleştirmesinde
refresh token'ın `HttpOnly` + `Secure` + `SameSite` cookie'ye taşınması (BFF veya backend
cookie sözleşmesi) hedeflenir; access token kısa ömürlü bellek-içi tutulur. Bu, backend
cookie sözleşmesi netleştiğinde yapılacak ayrı bir iştir.

**CSP ve güvenlik başlıkları (F10-08):** `next.config.ts` tüm yanıtlara Content-Security-Policy,
`X-Content-Type-Options: nosniff`, `Referrer-Policy`, `X-Frame-Options: DENY` ve
`Permissions-Policy` ekler. `connect-src`, API origin'ini ve SignalR websocket'ini
(`NEXT_PUBLIC_API_BASE_URL`'den türetilir) kapsar. `script-src`/`style-src` şu an
`'unsafe-inline'` içerir (Next.js bootstrap script'i + Tailwind/shadcn inline style'ları);
nonce + `'strict-dynamic'` tabanlı sıkı CSP (dinamik render gerektirir) post-MVP
sertleştirme adımıdır.

Gizli bilgiler `NEXT_PUBLIC_*` değişkenlerine yazılmaz.

import { defineConfig, devices } from "@playwright/test";

// Bu suite hızlı bir UI davranış testidir, SÖZLEŞME KANITI DEĞİLDİR (F10-09): API'yi
// route mock'larıyla taklit eder (e2e/fixtures/api-mock.ts), böylece CI'da backend,
// Postgres ve Redis çalıştırmadan UI akışları (loading/empty/error/permission, refresh
// koordinasyonu, rota koruması) hızlıca korunur. Gerçek backend sözleşme kanıtı ayrı
// bir suite'tedir: `npm run test:e2e:fullstack` (playwright.fullstack.config.ts) +
// backend integration testleri + üretilen OpenAPI tipleri.
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? "line" : "list",
  use: {
    // Geliştirme sunucusu 3000'i kullanır; E2E kendi portunda çalışır ki açık bir
    // dev/preview sunucusu yanlışlıkla yeniden kullanılmasın (o sunucuda aşağıdaki
    // API adresi ayarlı olmadığı için istekler mock yerine gerçek backend'e gider).
    baseURL: "http://localhost:3100",
    trace: "on-first-retry",
  },
  // Masaüstü ve mobil farklı yerleşimler render eder (tablo ↔ kart satırları,
  // sabit sidebar ↔ çekmece). Her spec kendi kırılımında çalışır.
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
      testIgnore: "**/mobile.spec.ts",
    },
    {
      name: "mobile",
      use: { ...devices["Pixel 7"] },
      testMatch: "**/mobile.spec.ts",
    },
  ],
  webServer: {
    // Production build, dev overlay'i ve HMR gürültüsünü devre dışı bırakır.
    command: "npm run build && npm run start -- --port 3100",
    url: "http://localhost:3100",
    reuseExistingServer: false,
    timeout: 180_000,
    env: {
      // API tabanı aynı origin'e alınır: tarayıcı CORS preflight göndermez,
      // böylece route mock'ları her isteği yakalayabilir. Gerçek dağıtımda
      // bu değer backend'in kendi adresidir (bkz. docs/DEPLOYMENT.md).
      NEXT_PUBLIC_API_BASE_URL: "http://localhost:3100/__api",
    },
  },
});

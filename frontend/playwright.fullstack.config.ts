import { defineConfig } from "@playwright/test";

// F10-09: Gerçek backend kullanan full-stack kalite kapısı.
//
// Route-mock suite'inden (playwright.config.ts) ayrıdır ve onun yerine geçmez: mock
// suite hızlı UI davranış testidir, bu suite ise frontend'in beklediği sözleşmeyi
// ÇALIŞAN backend'e karşı kanıtlar (auth, logout iptali, proje silme izolasyonu, görev
// yetki matrisi, yorem sayfalaması). Testler Playwright `request` context'i ile gerçek
// API'yi sürer; her test kendi kullanıcılarını benzersiz adlarla oluşturur, böylece
// paylaşılan veritabanına bağımlılık yoktur.
//
// Ön koşul: Görev 10 backend'i ayakta olmalı. Adres FULLSTACK_API_URL ile verilir
// (varsayılan http://localhost:8080). Yerel/compose Postgres yeterlidir.
const apiURL = process.env.FULLSTACK_API_URL ?? "http://localhost:8080";

export default defineConfig({
  testDir: "./e2e-fullstack",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? "line" : "list",
  use: {
    baseURL: apiURL,
    extraHTTPHeaders: {
      Accept: "application/json",
    },
  },
});

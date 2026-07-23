import { test, expect, signIn, PROJECT_ID } from "./fixtures/api-mock";

test.beforeEach(async ({ page }) => {
  await signIn(page);
});

test.describe("Proje listesi", () => {
  test("kullanıcının projelerini sözleşmedeki alanlarla listeler", async ({ page }) => {
    await page.goto("/projects");

    await expect(page.getByRole("heading", { name: "Projeler" })).toBeVisible();
    await expect(page.getByText("2 proje")).toBeVisible();
    await expect(page.getByText("Web Sitesi Yenileme").first()).toBeVisible();
    await expect(page.getByText("Mobil Uygulama v2").first()).toBeVisible();
    // Açıklaması olmayan proje için dürüst boş metin (uydurma sayaç/ikon yok).
    await expect(page.getByText("Açıklama yok").first()).toBeVisible();
  });

  test("boş durum ilk proje oluşturmaya yönlendirir", async ({ page, api }) => {
    api.override("GET /api/projects", (route) =>
      route.fulfill({ status: 200, contentType: "application/json", body: "[]" }),
    );
    await page.goto("/projects");

    await expect(page.getByText("Henüz projeniz yok")).toBeVisible();
    await expect(page.getByText("İlk projenizi oluşturarak başlayın.")).toBeVisible();
  });

  test("hata durumunda yeniden deneme listeyi tazeler", async ({ page, api }) => {
    api.fail("GET /api/projects", 500, "Beklenmeyen bir hata oluştu.");
    await page.goto("/projects");

    await expect(page.getByText("Projeler yüklenemedi.")).toBeVisible();

    // Yeniden dene: bu kez varsayılan (başarılı) yanıt dönsün.
    api.override("GET /api/projects", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            id: PROJECT_ID,
            name: "Web Sitesi Yenileme",
            description: null,
            ownerUserId: "2c9e4b7a-1111-4c3f-9a2b-000000008a12",
            createdAtUtc: "2026-06-03T09:00:00+00:00",
            updatedAtUtc: null,
          },
        ]),
      }),
    );
    await page.getByRole("button", { name: "Tekrar dene" }).click();

    await expect(page.getByText("Web Sitesi Yenileme").first()).toBeVisible();
  });

  test("proje oluşturma diyaloğu başarı toast'ı gösterir", async ({ page }) => {
    await page.goto("/projects");

    await page.getByRole("button", { name: "Yeni proje" }).first().click();
    await page.getByLabel("Proje adı").fill("Pazarlama Kampanyası Q3");
    await page.getByRole("button", { name: "Oluştur" }).click();

    await expect(page.getByText("Proje oluşturuldu.")).toBeVisible();
  });

  test("proje adı boşken doğrulama hatası verir", async ({ page }) => {
    await page.goto("/projects");

    await page.getByRole("button", { name: "Yeni proje" }).first().click();
    await page.getByRole("button", { name: "Oluştur" }).click();

    await expect(page.getByText("Proje adı zorunludur.")).toBeVisible();
  });
});

test.describe("Proje genel bakış", () => {
  test("altı metrik ve tamamlanma yüzdesi gösterilir", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}`);

    await expect(page.getByRole("heading", { name: "Web Sitesi Yenileme" })).toBeVisible();
    await expect(page.getByText("%29")).toBeVisible();

    for (const label of [
      "Toplam",
      "Yapılacak",
      "Devam Eden",
      "Tamamlanan",
      "İptal",
      "Geciken",
    ]) {
      await expect(page.getByText(label, { exact: true })).toBeVisible();
    }

    // Aktivite akışı endpoint'i yok: uydurma bölüm render edilmemeli.
    await expect(page.getByText(/aktivite|son hareketler/i)).toHaveCount(0);
  });

  test("erişilemeyen proje için 404 durumu gösterilir", async ({ page, api }) => {
    api.fail(`GET /api/projects/${PROJECT_ID}`, 404, "Project was not found.");
    await page.goto(`/projects/${PROJECT_ID}`);

    await expect(page.getByText("Bu projeye erişiminiz yok")).toBeVisible();
    await expect(page.getByRole("link", { name: "Projelerime dön" })).toBeVisible();
  });
});

test.describe("Üyeler", () => {
  test("üye listesi adları gösterir; e-posta ve rol sızdırmaz", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/members`);

    await expect(page.getByRole("heading", { name: "Üyeler" })).toBeVisible();
    await expect(page.getByText("2 üye")).toBeVisible();
    await expect(page.getByText("Ayşe Demir").first()).toBeVisible();
    await expect(page.getByText("@aysedemir").first()).toBeVisible();
    // Üye yanıtı e-posta veya rol taşımaz; ekranda da olmamalı.
    await expect(page.getByText("ayse@ornek.com")).toHaveCount(0);
    await expect(page.getByText("ProjectManager")).toHaveCount(0);
  });

  test("üye ekleme kişiyi isimle aratıp seçtirir", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/members`);
    await page.getByRole("button", { name: "Üye ekle" }).click();

    // Seçim yapılmadan gönderilemez.
    await expect(page.getByRole("button", { name: "Ekle", exact: true })).toBeDisabled();

    await page.getByRole("combobox", { name: "Kişi" }).click();
    await page.getByLabel("Kişi ara").fill("zeynep");

    const option = page.getByRole("option", { name: /Zeynep Arslan/ });
    await expect(option).toBeVisible();
    await option.click();

    await expect(page.getByRole("combobox", { name: "Kişi" })).toContainText("Zeynep Arslan");
    await page.getByRole("button", { name: "Ekle", exact: true }).click();

    await expect(page.getByText("Üye eklendi.")).toBeVisible();
  });

  test("zaten üye olan kişi seçim listesinde çıkmaz", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/members`);
    await page.getByRole("button", { name: "Üye ekle" }).click();

    await page.getByRole("combobox", { name: "Kişi" }).click();
    await page.getByLabel("Kişi ara").fill("ayse");

    // Ayşe Demir zaten üye → seçilebilir sonuç yok.
    await expect(page.getByText(/ile eşleşen kişi yok/)).toBeVisible();
  });

  test("yinelenen üye 409 çakışması bağlama özgü gösterilir", async ({ page, api }) => {
    api.fail(
      `POST /api/projects/${PROJECT_ID}/members`,
      409,
      "Aynı kişiyi ikinci kez ekleyemezsiniz.",
    );
    await page.goto(`/projects/${PROJECT_ID}/members`);

    await page.getByRole("button", { name: "Üye ekle" }).click();
    await page.getByRole("combobox", { name: "Kişi" }).click();
    await page.getByLabel("Kişi ara").fill("zeynep");
    await page.getByRole("option", { name: /Zeynep Arslan/ }).click();
    await page.getByRole("button", { name: "Ekle", exact: true }).click();

    await expect(page.getByText("Üye eklenemedi.")).toBeVisible();
    await expect(page.getByText("Aynı kişiyi ikinci kez ekleyemezsiniz.")).toBeVisible();
    await expect(page.getByText("(409)")).toBeVisible();
  });
});

test.describe("Proje ayarları", () => {
  test("silme onayı proje adı birebir yazılana kadar devre dışıdır", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/settings`);

    await page.getByRole("button", { name: "Projeyi sil" }).click();

    // F10-04: "kalıcı silme" dili kaldırıldı; onay düğmesi "Projeyi kaldır".
    const confirm = page.getByRole("button", { name: "Projeyi kaldır" });
    await expect(confirm).toBeDisabled();

    await page.getByLabel("Onaylamak için proje adını yazın").fill("Web Sitesi");
    await expect(confirm).toBeDisabled();

    await page.getByLabel("Onaylamak için proje adını yazın").fill("Web Sitesi Yenileme");
    await expect(confirm).toBeEnabled();

    await confirm.click();
    await expect(page.getByText("Proje silindi.")).toBeVisible();
    await expect(page).toHaveURL(/\/projects$/);
  });
});

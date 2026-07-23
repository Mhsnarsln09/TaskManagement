import { test, expect, signIn, PROJECT_ID } from "./fixtures/api-mock";

const TASKS_URL = `/projects/${PROJECT_ID}/tasks`;

test.beforeEach(async ({ page }) => {
  await signIn(page);
});

test.describe("Görev listesi", () => {
  test("görevleri durum, öncelik, son tarih ve overdue ile gösterir", async ({ page }) => {
    await page.goto(TASKS_URL);

    await expect(page.getByRole("heading", { name: "Görevler" })).toBeVisible();
    await expect(page.getByText("Ödeme sayfası hata düzeltmesi").first()).toBeVisible();
    await expect(page.getByText("Devam Ediyor").first()).toBeVisible();
    await expect(page.getByText("Kritik").first()).toBeVisible();
    await expect(page.getByText("Gecikti").first()).toBeVisible();
    // Atanmamış görev dürüst şekilde işaretlenir.
    await expect(page.getByText("Atanmamış").first()).toBeVisible();
    await expect(page.getByText("4 görevden 1–4 gösteriliyor")).toBeVisible();
  });

  test("metin arama kutusu yoktur (endpoint mevcut değil)", async ({ page }) => {
    await page.goto(TASKS_URL);

    await expect(page.getByRole("searchbox")).toHaveCount(0);
    await expect(page.getByPlaceholder(/ara/i)).toHaveCount(0);
  });

  test("durum filtresi sunucuya query parametresi olarak gider", async ({ page, api }) => {
    await page.goto(TASKS_URL);

    await page.getByLabel("Durum filtresi").click();
    await page.getByRole("option", { name: "Tamamlandı" }).click();

    await expect(page).toHaveURL(/status=Completed/);
    await expect(page.getByText("Footer bağlantılarını düzelt").first()).toBeVisible();
    await expect(page.getByText("Ödeme sayfası hata düzeltmesi")).toHaveCount(0);
    expect(api.callCount(`GET /api/projects/${PROJECT_ID}/tasks`)).toBeGreaterThan(1);
  });

  test("filtreyle eşleşen görev yoksa temizleme eylemi sunulur", async ({ page }) => {
    await page.goto(`${TASKS_URL}?status=Cancelled`);

    await expect(page.getByText("Filtreyle eşleşen görev yok")).toBeVisible();
    await page.getByRole("button", { name: "Filtreleri temizle" }).click();

    await expect(page).not.toHaveURL(/status=/);
    await expect(page.getByText("Ödeme sayfası hata düzeltmesi").first()).toBeVisible();
  });

  test("başlık kolonundan sıralama yönü değiştirilir", async ({ page }) => {
    await page.goto(TASKS_URL);

    await page.getByRole("button", { name: "Başlık" }).click();
    await expect(page).toHaveURL(/sortBy=title&sortDirection=asc/);

    await page.getByRole("button", { name: "Başlık" }).click();
    await expect(page).toHaveURL(/sortBy=title(?!&sortDirection=asc)/);
  });

  test("boş projede ilk görevi oluşturma yönlendirmesi çıkar", async ({ page, api }) => {
    api.override(`GET /api/projects/${PROJECT_ID}/tasks`, (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 1 }),
      }),
    );
    await page.goto(TASKS_URL);

    await expect(page.getByText("Bu projede henüz görev yok")).toBeVisible();
  });

  test("429 yanıtı geri sayımla gösterilir ve yeniden dene devre dışı kalır", async ({
    page,
    api,
  }) => {
    api.fail(`GET /api/projects/${PROJECT_ID}/tasks`, 429, "Çok fazla istek.");
    await page.goto(TASKS_URL);

    await expect(page.getByText("Görevler yüklenemedi.")).toBeVisible();
    await expect(page.getByText(/sn sonra tekrar deneyebilirsiniz/)).toBeVisible();
    await expect(page.getByRole("button", { name: "Tekrar dene" })).toBeDisabled();
  });
});

test.describe("Görev formu", () => {
  test("oluşturma formunda durum alanı yoktur (backend Todo atar)", async ({ page }) => {
    await page.goto(TASKS_URL);
    await page.getByRole("button", { name: "Yeni görev" }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Görev, Yapılacak durumunda oluşturulur.")).toBeVisible();
    await expect(dialog.getByLabel("Durum")).toHaveCount(0);
    await expect(dialog.getByLabel("Öncelik")).toBeVisible();
  });

  test("başlık zorunludur", async ({ page }) => {
    await page.goto(TASKS_URL);
    await page.getByRole("button", { name: "Yeni görev" }).first().click();
    await page.getByRole("button", { name: "Oluştur" }).click();

    await expect(page.getByText("Başlık zorunludur.")).toBeVisible();
  });

  test("görev oluşturma başarı toast'ı gösterir", async ({ page }) => {
    await page.goto(TASKS_URL);
    await page.getByRole("button", { name: "Yeni görev" }).first().click();

    await page.getByLabel("Başlık").fill("Erişilebilirlik denetimi");
    await page.getByRole("button", { name: "Oluştur" }).click();

    await expect(page.getByText("Görev oluşturuldu.")).toBeVisible();
  });

  test("kirli form kapatılırken onay ister", async ({ page }) => {
    await page.goto(TASKS_URL);
    await page.getByRole("button", { name: "Yeni görev" }).first().click();
    await page.getByLabel("Başlık").fill("Yarım kalan görev");

    await page.getByRole("button", { name: "Vazgeç" }).click();

    await expect(page.getByText("Kaydedilmemiş değişiklikler var")).toBeVisible();
    await page.getByRole("button", { name: "Düzenlemeye dön" }).click();
    await expect(page.getByLabel("Başlık")).toHaveValue("Yarım kalan görev");
  });

  test("düzenlemede 409 çakışması son sürümü yükleme eylemi sunar", async ({ page, api }) => {
    api.fail(
      `PUT /api/projects/${PROJECT_ID}/tasks/11111111-0000-4c3f-9a2b-000000000001`,
      409,
      "Son sürümü yükleyip değişikliklerinizi yeniden uygulayın.",
    );
    await page.goto(`${TASKS_URL}?task=11111111-0000-4c3f-9a2b-000000000001`);

    await page.getByRole("button", { name: "Düzenle" }).click();
    await page.getByLabel("Başlık").fill("Güncellenmiş başlık");
    await page.getByRole("button", { name: "Kaydet" }).click();

    await expect(
      page.getByText("Bu görev siz düzenlerken başkası tarafından güncellendi."),
    ).toBeVisible();
    // F10-06: girdiyi kaybetmeden güncel temele geçiş eylemi.
    await expect(page.getByRole("button", { name: "Güncel sürüme geç" })).toBeVisible();
  });
});

test.describe("Görev detayı", () => {
  test("meta veriler, yorum ve ek bölümleri sırayla gösterilir", async ({ page }) => {
    await page.goto(`${TASKS_URL}?task=11111111-0000-4c3f-9a2b-000000000001`);

    const sheet = page.getByRole("dialog");
    await expect(sheet.getByText("Ödeme sayfası hata düzeltmesi")).toBeVisible();
    await expect(sheet.getByText("Son tarih")).toBeVisible();
    await expect(sheet.getByText("Atanan")).toBeVisible();
    await expect(sheet.getByText("Yorumlar")).toBeVisible();
    await expect(sheet.getByText("Ekler")).toBeVisible();
    await expect(sheet.getByText("Henüz ek yok — dosya yükleyin.")).toBeVisible();
  });

  test("yorum eklenince liste tazelenir", async ({ page }) => {
    await page.goto(`${TASKS_URL}?task=11111111-0000-4c3f-9a2b-000000000001`);

    await page.getByPlaceholder("Yorum yazın…").fill("Staging ortamında doğrulandı.");
    await page.getByRole("button", { name: "Yorumu gönder" }).click();

    // Yeni yorum sunucudan yeniden çekilir; composer temizlenir.
    await expect(page.getByPlaceholder("Yorum yazın…")).toHaveValue("");
  });

  test("proje sahibine görev eylemleri (düzenle/sil) menüsü gösterilir", async ({ page }) => {
    // B10-02 matrisi: tam görev yönetimi (düzenle/yeniden ata/sil) proje sahibi veya
    // Admin'e aittir; sistem ProjectManager rolü tek başına yetki vermez. Oturum
    // kullanıcısı (ayse) PROJECT'in sahibidir, bu yüzden eylem menüsünü görür.
    await page.goto(`${TASKS_URL}?task=11111111-0000-4c3f-9a2b-000000000001`);
    await expect(page.getByRole("button", { name: "Diğer eylemler" })).toBeVisible();
  });
});

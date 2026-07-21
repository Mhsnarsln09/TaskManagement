import { test, expect, signIn, USERS, PROJECT_ID } from "./fixtures/api-mock";

// Mobil varyantlar (tasarım §02/§13): sidebar yerine çekmece, tablo yerine kart
// satırları, popover yerine tam sayfa bildirimler. Yalnız "mobile" projesinde
// çalışır (playwright.config.ts).

test.describe("Mobil kabuk", () => {
  test("gezinim çekmecesi tüm ana bağlantıları korur", async ({ page }) => {
    await signIn(page);
    await page.goto(`/projects/${PROJECT_ID}`);

    // Masaüstü sidebar mobilde gizlidir.
    await expect(page.getByRole("link", { name: "Genel Bakış" })).toBeHidden();

    await page.getByRole("button", { name: "Menüyü aç" }).click();

    const drawer = page.getByRole("dialog");
    await expect(drawer.getByRole("link", { name: "Genel Bakış" })).toBeVisible();
    await expect(drawer.getByRole("link", { name: "Görevler" })).toBeVisible();
    await expect(drawer.getByRole("link", { name: "Üyeler" })).toBeVisible();

    await drawer.getByRole("link", { name: "Görevler" }).click();
    await expect(page).toHaveURL(new RegExp(`/projects/${PROJECT_ID}/tasks`));
  });

  test("çekmecedeki dokunma hedefleri en az 44px yüksekliktedir", async ({ page }) => {
    await signIn(page);
    await page.goto(`/projects/${PROJECT_ID}`);
    await page.getByRole("button", { name: "Menüyü aç" }).click();

    const link = page.getByRole("dialog").getByRole("link", { name: "Görevler" });
    const box = await link.boundingBox();
    expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);
  });

  test("SuperAdmin yönetim bağlantısı çekmecede görünür", async ({ page }) => {
    await signIn(page, USERS.superAdmin);
    await page.goto("/projects");

    await page.getByRole("button", { name: "Menüyü aç" }).click();
    await expect(
      page.getByRole("dialog").getByRole("link", { name: "Kullanıcılar" }),
    ).toBeVisible();
  });
});

test.describe("Mobil listeler", () => {
  test.beforeEach(async ({ page }) => {
    await signIn(page);
  });

  test("proje listesi kart satırlarıyla gösterilir", async ({ page }) => {
    await page.goto("/projects");

    // Masaüstü tablosu mobilde render edilmez.
    await expect(page.getByRole("table")).toBeHidden();
    const card = page.getByRole("listitem").filter({ hasText: "Web Sitesi Yenileme" });
    await expect(card).toBeVisible();
  });

  test("görev listesi yapılandırılmış satırlar ve rozetlerle gösterilir", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/tasks`);

    await expect(page.getByRole("table")).toBeHidden();
    const row = page.getByRole("listitem").filter({ hasText: "Ödeme sayfası hata düzeltmesi" });
    await expect(row).toBeVisible();
    await expect(row.getByText("Devam Ediyor")).toBeVisible();
    await expect(row.getByText("Kritik")).toBeVisible();
    await expect(row.getByText("Gecikti")).toBeVisible();
  });

  test("mobil satırdan görev detayı tam genişlikte açılır", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/tasks`);

    await page
      .getByRole("listitem")
      .filter({ hasText: "Ödeme sayfası hata düzeltmesi" })
      .click();

    const sheet = page.getByRole("dialog");
    await expect(sheet.getByText("Yorumlar")).toBeVisible();

    const viewport = page.viewportSize();
    const box = await sheet.boundingBox();
    // Dar ekranda panel neredeyse tüm genişliği kaplar.
    expect(box?.width ?? 0).toBeGreaterThan((viewport?.width ?? 0) * 0.85);
  });

  test("üye listesi kart satırlarında ad ve katılım tarihi gösterir", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/members`);

    await expect(page.getByRole("table")).toBeHidden();
    // Gizli masaüstü sidebar'ında da ad geçtiği için üye kartına kapsamlanır.
    const card = page.getByRole("listitem").filter({ hasText: "Katılım:" }).first();
    await expect(card).toBeVisible();
    await expect(card.getByText("Ayşe Demir")).toBeVisible();
  });

  test("bildirimler mobilde tam sayfa rotada listelenir", async ({ page }) => {
    await page.goto("/notifications");

    await expect(page.getByRole("heading", { name: "Bildirimler" })).toBeVisible();
    await expect(
      page.getByText('Size "Ödeme sayfası hata düzeltmesi" görevi atandı.'),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Tümünü okundu işaretle" })).toBeVisible();
  });

  test("sayfa yatay taşma yapmaz", async ({ page }) => {
    await page.goto(`/projects/${PROJECT_ID}/tasks`);

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
    );
    expect(overflow).toBeLessThanOrEqual(1);
  });
});

test.describe("Mobil yönetim", () => {
  test("kullanıcı yönetimi kart satırları ve rol diyaloğu çalışır", async ({ page }) => {
    await signIn(page, USERS.superAdmin);
    await page.goto("/admin/users");

    await expect(page.getByRole("table")).toBeHidden();
    await page.getByRole("button", { name: "Roller" }).first().click();

    await expect(page.getByText("Oturum sonlandırılır")).toBeVisible();
    await expect(page.getByRole("checkbox", { name: "SuperAdmin" })).toBeVisible();
  });
});

test.describe("Mobil auth", () => {
  test("giriş formu tek sütun ve kullanılabilir", async ({ page }) => {
    await page.goto("/login");

    await page.getByLabel("Kullanıcı adı veya e-posta").fill("aysedemir");
    await page.getByLabel("Parola", { exact: true }).fill("Parola123");
    await page.getByRole("button", { name: "Oturum aç" }).click();

    await expect(page).toHaveURL(/\/projects$/);
  });
});

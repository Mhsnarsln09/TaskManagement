import { test, expect, signIn, USERS } from "./fixtures/api-mock";

test.describe("Kimlik doğrulama", () => {
  test("kayıt formu sözleşmedeki alanları gösterir; rol seçici yoktur", async ({ page }) => {
    await page.goto("/register");

    await expect(page.getByLabel(/görünen ad/i)).toBeVisible();
    await expect(page.getByLabel("Kullanıcı adı", { exact: true })).toBeVisible();
    await expect(page.getByLabel("E-posta")).toBeVisible();
    await expect(page.getByLabel("Parola", { exact: true })).toBeVisible();

    // Sözleşmede olmayan kontroller: rol, parola onayı, şirket, sosyal giriş.
    await expect(page.getByRole("combobox")).toHaveCount(0);
    await expect(page.getByText(/parola.*tekrar|şirket|google ile/i)).toHaveCount(0);
  });

  test("parola kuralları satır içi doğrulanır", async ({ page }) => {
    await page.goto("/register");

    await page.getByLabel("Kullanıcı adı", { exact: true }).fill("aysedemir");
    await page.getByLabel("E-posta").fill("ayse@ornek.com");
    await page.getByLabel("Parola", { exact: true }).fill("kucukharf1");
    await page.getByRole("button", { name: "Hesap oluştur" }).click();

    await expect(page.getByText("En az bir büyük harf içermelidir.")).toBeVisible();
  });

  test("geçersiz kimlik bilgisi 401 hatasını form üstünde gösterir", async ({ page, api }) => {
    api.fail("POST /api/auth/login", 401, "Kullanıcı adı/e-posta veya parola hatalı.");
    await page.goto("/login");

    await page.getByLabel("Kullanıcı adı veya e-posta").fill("aysedemir");
    await page.getByLabel("Parola", { exact: true }).fill("YanlisParola1");
    await page.getByRole("button", { name: "Oturum aç" }).click();

    await expect(page.getByText("Giriş başarısız.")).toBeVisible();
    await expect(
      page.getByText("Kullanıcı adı/e-posta veya parola hatalı."),
    ).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test("başarılı giriş proje listesine yönlendirir", async ({ page }) => {
    await page.goto("/login");

    await page.getByLabel("Kullanıcı adı veya e-posta").fill("aysedemir");
    await page.getByLabel("Parola", { exact: true }).fill("Parola123");
    await page.getByRole("button", { name: "Oturum aç" }).click();

    await expect(page).toHaveURL(/\/projects$/);
    await expect(page.getByRole("heading", { name: "Projeler" })).toBeVisible();
  });

  test("korumalı rota oturumsuz açılırsa dönüş URL'siyle girişe yönlendirir", async ({ page }) => {
    await page.goto("/projects");

    await expect(page).toHaveURL(/\/login\?returnUrl=%2Fprojects/);
  });

  test("giriş sonrası dönüş URL'sine geri dönülür", async ({ page }) => {
    await page.goto("/notifications");
    await expect(page).toHaveURL(/returnUrl=%2Fnotifications/);

    await page.getByLabel("Kullanıcı adı veya e-posta").fill("aysedemir");
    await page.getByLabel("Parola", { exact: true }).fill("Parola123");
    await page.getByRole("button", { name: "Oturum aç" }).click();

    await expect(page).toHaveURL(/\/notifications$/);
  });

  test("oturum süresi doldu bandı gösterilir", async ({ page }) => {
    await page.goto("/login?expired=1");

    await expect(page.getByText("Oturum süreniz doldu.")).toBeVisible();
  });

  test("çıkış tokenları temizler ve girişe döner", async ({ page }) => {
    await signIn(page);
    await page.goto("/projects");

    await page.getByRole("button", { name: "Hesap menüsü" }).first().click();
    await page.getByRole("menuitem", { name: "Çıkış yap" }).click();

    await expect(page).toHaveURL(/\/login/);
    const session = await page.evaluate(() =>
      window.localStorage.getItem("taskmanagement.session"),
    );
    expect(session).toBeNull();
  });

  test("hesap menüsü kimlik özetini ve rolleri gösterir", async ({ page }) => {
    await signIn(page);
    await page.goto("/projects");

    await page.getByRole("button", { name: "Hesap menüsü" }).first().click();

    await expect(page.getByText(USERS.ayse.email, { exact: false })).toBeVisible();
    await expect(page.getByText("ProjectManager", { exact: true })).toBeVisible();
  });
});

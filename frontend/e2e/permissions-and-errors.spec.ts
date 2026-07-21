import { test, expect, signIn, USERS, PROJECT_ID } from "./fixtures/api-mock";

test.describe("Yetkiye bağlı görünürlük", () => {
  test("SuperAdmin sidebar'da Yönetim bölümünü görür", async ({ page }) => {
    await signIn(page, USERS.superAdmin);
    await page.goto("/projects");

    await expect(page.getByRole("link", { name: "Kullanıcılar" })).toBeVisible();
    await expect(page.getByText("Yönetim")).toBeVisible();
  });

  test("SuperAdmin olmayan kullanıcıda Yönetim bölümü hiç render edilmez", async ({ page }) => {
    await signIn(page, USERS.ayse);
    await page.goto("/projects");

    await expect(page.getByRole("link", { name: "Kullanıcılar" })).toHaveCount(0);
    await expect(page.getByText("Yönetim")).toHaveCount(0);
  });

  test("SuperAdmin olmayan kullanıcı /admin/users rotasında 404 görür", async ({ page }) => {
    await signIn(page, USERS.ayse);
    await page.goto("/admin/users");

    await expect(page.getByText("Sayfa bulunamadı")).toBeVisible();
    // Kaynak sızdırmamak için "yetkiniz yok" yerine 404 tercih edilir.
    await expect(page.getByText("Erişim yetkiniz yok")).toHaveCount(0);
  });

  test("proje sahibi olmayan üye ayarlar sayfasında 403 görür", async ({ page, api }) => {
    await signIn(page, USERS.ayse);
    api.override(`GET /api/projects/${PROJECT_ID}`, (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          id: PROJECT_ID,
          name: "Web Sitesi Yenileme",
          description: null,
          // Sahip başkası → yönetim eylemleri gizlenir.
          ownerUserId: USERS.superAdmin.id,
          createdAtUtc: "2026-06-03T09:00:00+00:00",
          updatedAtUtc: null,
        }),
      }),
    );

    await page.goto(`/projects/${PROJECT_ID}/settings`);

    await expect(page.getByText("Erişim yetkiniz yok")).toBeVisible();
  });

  test("sahip olmayan üye için üye yönetimi eylemleri gizlenir", async ({ page, api }) => {
    await signIn(page, USERS.ayse);
    api.override(`GET /api/projects/${PROJECT_ID}`, (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          id: PROJECT_ID,
          name: "Web Sitesi Yenileme",
          description: null,
          ownerUserId: USERS.superAdmin.id,
          createdAtUtc: "2026-06-03T09:00:00+00:00",
          updatedAtUtc: null,
        }),
      }),
    );

    await page.goto(`/projects/${PROJECT_ID}/members`);

    await expect(page.getByText("Üyeleri yalnızca proje sahibi yönetebilir.")).toBeVisible();
    await expect(page.getByRole("button", { name: "Üye ekle" })).toHaveCount(0);
  });
});

test.describe("SuperAdmin rol yönetimi", () => {
  test.beforeEach(async ({ page }) => {
    await signIn(page, USERS.superAdmin);
  });

  test("arama sunucuya search parametresiyle gider", async ({ page }) => {
    await page.goto("/admin/users");

    await expect(page.getByText("Sistem rolleri · 2 kullanıcı")).toBeVisible();
    await page.getByLabel("Kullanıcı ara").fill("mehmet");

    await expect(page.getByText('"mehmet" ile eşleşen kullanıcı yok')).toBeVisible();
    await page.getByRole("button", { name: "Aramayı temizle" }).click();
    await expect(page.getByText("Sistem rolleri · 2 kullanıcı")).toBeVisible();
  });

  test("rol diyaloğu oturum sonlandırma uyarısı gösterir", async ({ page }) => {
    await page.goto("/admin/users");

    await page.getByRole("button", { name: "Rolleri yönet" }).first().click();

    await expect(page.getByText("Oturum sonlandırılır")).toBeVisible();
    await expect(
      page.getByText(/mevcut oturumu sonlandırılır; yeniden giriş yapması gerekir/),
    ).toBeVisible();
  });

  test("son SuperAdmin kaldırılamaz (409) mesajı gösterilir", async ({ page, api }) => {
    api.fail(
      `PUT /api/admin/users/${USERS.superAdmin.id}/roles`,
      409,
      "Önce başka bir kullanıcıya SuperAdmin rolü verin.",
    );
    await page.goto("/admin/users");

    // İkinci satır SuperAdmin kullanıcısı.
    await page.getByRole("button", { name: "Rolleri yönet" }).nth(1).click();
    await page.getByRole("checkbox", { name: "SuperAdmin" }).click();
    await page.getByRole("checkbox", { name: "Member" }).click();
    await page.getByRole("button", { name: "Kaydet" }).click();

    await expect(page.getByText("Önce başka bir kullanıcıya SuperAdmin rolü verin.")).toBeVisible();
    await expect(page.getByText("(409)")).toBeVisible();
  });

  test("hiç rol seçilmezse kaydet devre dışıdır", async ({ page }) => {
    await page.goto("/admin/users");

    await page.getByRole("button", { name: "Rolleri yönet" }).nth(1).click();
    await page.getByRole("checkbox", { name: "SuperAdmin" }).click();

    await expect(page.getByText("En az bir rol seçilmelidir.")).toBeVisible();
    await expect(page.getByRole("button", { name: "Kaydet" })).toBeDisabled();
  });
});

test.describe("Oturum ve hata işleme", () => {
  test("eşzamanlı 401 yanıtları tek refresh isteği üretir", async ({ page, api }) => {
    await signIn(page);

    // İlk turda korumalı uçlar 401 döner; refresh sonrası 200.
    let refreshed = false;
    api.override("POST /api/auth/refresh", async (route) => {
      refreshed = true;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accessToken: "renewed-token",
          expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
          refreshToken: "renewed-refresh",
          refreshTokenExpiresAtUtc: new Date(Date.now() + 86_400_000).toISOString(),
          user: USERS.ayse,
        }),
      });
    });
    api.override("GET /api/projects", (route) => {
      if (!refreshed) {
        return route.fulfill({
          status: 401,
          contentType: "application/problem+json",
          body: JSON.stringify({ status: 401, title: "Unauthorized" }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([]),
      });
    });
    api.override("GET /api/notifications", (route) => {
      if (!refreshed) {
        return route.fulfill({
          status: 401,
          contentType: "application/problem+json",
          body: JSON.stringify({ status: 401, title: "Unauthorized" }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 1 }),
      });
    });

    await page.goto("/projects");

    await expect(page.getByText("Henüz projeniz yok")).toBeVisible();
    // Paralel 401'ler tek refresh isteğini paylaşır.
    expect(api.callCount("POST /api/auth/refresh")).toBe(1);
    const session = await page.evaluate(() =>
      JSON.parse(window.localStorage.getItem("taskmanagement.session") ?? "{}"),
    );
    expect(session.accessToken).toBe("renewed-token");
  });

  test("refresh başarısızsa oturum-süresi-doldu yönlendirmesi yapılır", async ({ page, api }) => {
    await signIn(page);
    api.fail("GET /api/projects", 401, "Unauthorized.");
    api.fail("GET /api/notifications", 401, "Unauthorized.");
    api.fail("POST /api/auth/refresh", 401, "Refresh token is invalid.");

    await page.goto("/projects");

    await expect(page).toHaveURL(/\/login\?expired=1/);
    await expect(page.getByText("Oturum süreniz doldu.")).toBeVisible();
    const session = await page.evaluate(() =>
      window.localStorage.getItem("taskmanagement.session"),
    );
    expect(session).toBeNull();
  });

  test("ağ hatası anlaşılır şekilde gösterilir", async ({ page, api }) => {
    await signIn(page);
    api.override("GET /api/projects", (route) => route.abort("connectionrefused"));

    await page.goto("/projects");

    await expect(page.getByText("Projeler yüklenemedi.")).toBeVisible();
    await expect(page.getByText("Sunucuya ulaşılamıyor.")).toBeVisible();
  });

  test("bilinmeyen rota 404 sayfası gösterir", async ({ page }) => {
    await signIn(page);
    await page.goto("/boyle-bir-sayfa-yok");

    await expect(page.getByText("Sayfa bulunamadı")).toBeVisible();
  });
});

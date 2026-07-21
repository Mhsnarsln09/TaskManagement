import { test as base, expect, type Page, type Route } from "@playwright/test";
import {
  ADMIN_USERS,
  DIRECTORY,
  MEMBERS,
  NOTIFICATIONS,
  OTHER_PROJECT,
  PROJECT,
  PROJECT_ID,
  STATISTICS,
  TASKS,
  USERS,
  authResponse,
  paged,
  problem,
} from "./data";

// E2E'de API tabanı aynı origin altındadır (playwright.config.ts webServer.env),
// böylece CORS preflight devreye girmez ve her istek route mock'una düşer.
const API_PREFIX = "/__api";

type Handler = (route: Route, url: URL) => Promise<void> | void;

/** Test içinden eklenen geçersiz kılmalar; ilk eşleşen kazanır. */
export interface ApiMock {
  /** `METHOD /api/yol` kalıbı için özel yanıt tanımlar (varsayılanı ezer). */
  override(pattern: string, handler: Handler): void;
  /** Kısayol: Problem Details yanıtı döndürür. */
  fail(pattern: string, status: number, detail: string, extra?: Record<string, unknown>): void;
  /** İstek sayacı: aynı uç noktaya kaç çağrı gittiğini doğrulamak için. */
  callCount(pattern: string): number;
}

function json(route: Route, body: unknown, status = 200) {
  return route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function noContent(route: Route) {
  return route.fulfill({ status: 204 });
}

function problemJson(route: Route, body: unknown, status: number, headers?: Record<string, string>) {
  return route.fulfill({
    status,
    contentType: "application/problem+json",
    headers,
    body: JSON.stringify(body),
  });
}

export const test = base.extend<{ api: ApiMock }>({
  // auto: testler `api` parametresini almasa da mock kurulur; aksi halde
  // Playwright fixture'ı tembel oluşturur ve istekler gerçek ağa çıkar.
  api: [async ({ page }, use) => {
    const overrides = new Map<string, Handler>();
    const counts = new Map<string, number>();

    const api: ApiMock = {
      override: (pattern, handler) => overrides.set(pattern, handler),
      fail: (pattern, status, detail, extra) =>
        overrides.set(pattern, (route) =>
          problemJson(
            route,
            { ...problem(status, `HTTP ${status}`, detail), ...extra },
            status,
            status === 429 ? { "Retry-After": "3" } : undefined,
          ),
        ),
      callCount: (pattern) => counts.get(pattern) ?? 0,
    };

    // SignalR hub'ı testlerde bağlanmaz; bildirim listesi yine API'den gelir.
    await page.route(
      (url) => url.pathname.startsWith(`${API_PREFIX}/hubs/`),
      (route) => route.abort(),
    );

    await page.route((url) => url.pathname.startsWith(`${API_PREFIX}/api/`), async (route) => {
      const url = new URL(route.request().url());
      // Sözleşme yolları prefix'siz eşleşir: /__api/api/projects → /api/projects
      const path = url.pathname.slice(API_PREFIX.length);
      const key = `${route.request().method()} ${path}`;
      counts.set(key, (counts.get(key) ?? 0) + 1);

      const override = overrides.get(key);
      if (override) return override(route, url);

      return defaultHandler(route, url, key, path);
    });

    await use(api);
  }, { auto: true }],
});

async function defaultHandler(route: Route, url: URL, key: string, path: string) {

  // --- Auth ---
  if (key === "POST /api/auth/login") {
    const body = route.request().postDataJSON() as { userNameOrEmail: string };
    const user = body.userNameOrEmail.includes("super") ? USERS.superAdmin : USERS.ayse;
    return json(route, authResponse(user));
  }
  if (key === "POST /api/auth/register") {
    return json(route, authResponse(USERS.ayse), 201);
  }
  if (key === "POST /api/auth/refresh") {
    return json(route, authResponse(USERS.ayse));
  }

  // --- Kullanıcı dizini ---
  if (key === "GET /api/users") {
    const search = url.searchParams.get("search")?.toLocaleLowerCase("tr") ?? "";
    return json(
      route,
      DIRECTORY.filter((user) =>
        `${user.userName} ${user.displayName ?? ""}`
          .toLocaleLowerCase("tr")
          .includes(search),
      ),
    );
  }

  // --- Projeler ---
  if (key === "GET /api/projects") return json(route, [PROJECT, OTHER_PROJECT]);
  if (key === "POST /api/projects") {
    const body = route.request().postDataJSON() as { name: string; description: string | null };
    return json(route, { ...PROJECT, id: "new-project-id", ...body }, 201);
  }
  if (key === `GET /api/projects/${PROJECT_ID}`) return json(route, PROJECT);
  if (key === `PUT /api/projects/${PROJECT_ID}`) {
    const body = route.request().postDataJSON() as { name: string; description: string | null };
    return json(route, { ...PROJECT, ...body, updatedAtUtc: new Date().toISOString() });
  }
  if (key === `DELETE /api/projects/${PROJECT_ID}`) return noContent(route);
  if (key === `GET /api/projects/${PROJECT_ID}/statistics`) return json(route, STATISTICS);
  if (key === `GET /api/projects/${PROJECT_ID}/members`) return json(route, MEMBERS);
  if (key === `POST /api/projects/${PROJECT_ID}/members`) {
    const body = route.request().postDataJSON() as { userId: string };
    return json(
      route,
      {
        userId: body.userId,
        user: DIRECTORY.find((user) => user.id === body.userId) ?? null,
        joinedAtUtc: new Date().toISOString(),
      },
      201,
    );
  }
  if (path.startsWith(`/api/projects/${PROJECT_ID}/members/`)) {
    return noContent(route);
  }

  // --- Görevler ---
  if (key === `GET /api/projects/${PROJECT_ID}/tasks`) {
    const status = url.searchParams.get("status");
    const priority = url.searchParams.get("priority");
    const sortBy = url.searchParams.get("sortBy");
    const direction = url.searchParams.get("sortDirection") === "asc" ? 1 : -1;

    let items = [...TASKS];
    if (status) items = items.filter((item) => item.status === status);
    if (priority) items = items.filter((item) => item.priority === priority);
    if (sortBy === "title") {
      items.sort((a, b) => a.title.localeCompare(b.title, "tr") * direction);
    }
    return json(route, paged(items, 1, 10, items.length));
  }
  if (key === `POST /api/projects/${PROJECT_ID}/tasks`) {
    const body = route.request().postDataJSON() as Record<string, unknown>;
    return json(
      route,
      {
        ...TASKS[1],
        id: "created-task-id",
        ...body,
        isOverdue: false,
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: null,
      },
      201,
    );
  }
  const taskMatch = /^\/api\/projects\/[^/]+\/tasks\/([^/]+)$/.exec(path);
  if (taskMatch) {
    if (route.request().method() === "DELETE") return noContent(route);
    const found = TASKS.find((item) => item.id === taskMatch[1]) ?? TASKS[0];
    if (route.request().method() === "PUT") {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      return json(route, { ...found, ...body, updatedAtUtc: new Date().toISOString() });
    }
    return json(route, found);
  }

  // --- Yorumlar ve ekler ---
  if (path.endsWith("/comments")) {
    if (route.request().method() === "POST") {
      const body = route.request().postDataJSON() as { content: string };
      return json(
        route,
        {
          id: "created-comment-id",
          taskItemId: TASKS[0].id,
          content: body.content,
          author: {
            id: USERS.ayse.id,
            userName: USERS.ayse.userName,
            displayName: USERS.ayse.displayName,
          },
          createdAtUtc: new Date().toISOString(),
        },
        201,
      );
    }
    return json(route, paged([], 1, 20, 0));
  }
  if (path.endsWith("/attachments")) return json(route, []);

  // --- Bildirimler ---
  if (key === "GET /api/notifications") return json(route, paged(NOTIFICATIONS, 1, 20, 2));
  if (path.startsWith("/api/notifications/")) return noContent(route);

  // --- Yönetim ---
  if (key === "GET /api/admin/users") {
    const search = url.searchParams.get("search")?.toLocaleLowerCase("tr") ?? "";
    const items = search
      ? ADMIN_USERS.filter((user) =>
          `${user.userName} ${user.email} ${user.displayName ?? ""}`
            .toLocaleLowerCase("tr")
            .includes(search),
        )
      : ADMIN_USERS;
    return json(route, paged(items, 1, 10, items.length));
  }
  const rolesMatch = /^\/api\/admin\/users\/([^/]+)\/roles$/.exec(path);
  if (rolesMatch) {
    const body = route.request().postDataJSON() as { roles: string[] };
    const user = ADMIN_USERS.find((item) => item.id === rolesMatch[1]) ?? ADMIN_USERS[0];
    return json(route, { ...user, roles: body.roles });
  }

  return problemJson(route, problem(404, "Not Found", `Mock yok: ${key}`), 404);
}

/**
 * Oturumu localStorage'a yazar; giriş formundan geçmeden korumalı sayfalar açılır.
 * Anahtar ve gövde src/lib/auth/token-store.ts ile aynıdır.
 */
export async function signIn(
  page: Page,
  user: (typeof USERS)[keyof typeof USERS] = USERS.ayse,
) {
  const response = authResponse(user);
  await page.addInitScript(
    ([key, value]) => window.localStorage.setItem(key as string, value as string),
    [
      "taskmanagement.session",
      JSON.stringify({
        accessToken: response.accessToken,
        accessTokenExpiresAtUtc: response.expiresAtUtc,
        refreshToken: response.refreshToken,
        refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc,
        user: response.user,
      }),
    ],
  );
}

export { expect, USERS, PROJECT_ID };

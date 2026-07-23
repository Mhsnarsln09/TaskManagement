import { test, expect } from "@playwright/test";
import {
  addComment,
  addMember,
  bearer,
  createProject,
  createTask,
  registerUser,
} from "./api-helpers";

// F10-09: real-backend contract gate. These run against a live Görev 10 backend and
// prove the exact contract the frontend depends on (auth, logout revoke, soft-delete
// isolation, task authority matrix, comment paging). The route-mock suite stays as a
// fast UI test and is not treated as contract proof.

test.describe("Auth ve logout (gerçek backend)", () => {
  test("kayıt token döndürür ve korumalı uca erişir", async ({ request }) => {
    const user = await registerUser(request);

    const projects = await request.get("/api/projects", bearer(user.token));
    expect(projects.status()).toBe(200);
  });

  test("logout refresh token ailesini iptal eder (B10-03)", async ({ request }) => {
    const user = await registerUser(request);

    const logout = await request.post("/api/auth/logout", {
      data: { refreshToken: user.refreshToken },
    });
    expect(logout.status()).toBe(204);

    // İptal edilmiş refresh token yeni access token üretemez.
    const refresh = await request.post("/api/auth/refresh", {
      data: { refreshToken: user.refreshToken },
    });
    expect(refresh.status()).toBe(401);

    // Idempotent: ikinci logout yine 204.
    const again = await request.post("/api/auth/logout", {
      data: { refreshToken: user.refreshToken },
    });
    expect(again.status()).toBe(204);
  });
});

test.describe("Soft-delete izolasyonu (gerçek backend)", () => {
  test("silinmiş projenin alt kaynakları eski üyeye kapanır (B10-01)", async ({ request }) => {
    const owner = await registerUser(request);
    const projectId = await createProject(request, owner.token);
    const task = await createTask(request, owner.token, projectId);
    await addComment(request, owner.token, projectId, task.id, "before delete");

    const del = await request.delete(`/api/projects/${projectId}`, bearer(owner.token));
    expect(del.status()).toBe(204);

    for (const path of [
      `/api/projects/${projectId}`,
      `/api/projects/${projectId}/tasks`,
      `/api/projects/${projectId}/tasks/${task.id}`,
      `/api/projects/${projectId}/statistics`,
      `/api/projects/${projectId}/tasks/${task.id}/comments`,
    ]) {
      const response = await request.get(path, bearer(owner.token));
      expect(response.status(), `${path} should 404 after delete`).toBe(404);
    }
  });
});

test.describe("Görev yetki matrisi (gerçek backend)", () => {
  test("owner/assignee/member işlemleri B10-02 matrisiyle uyumludur", async ({ request }) => {
    const owner = await registerUser(request);
    const member = await registerUser(request);
    const projectId = await createProject(request, owner.token);
    await addMember(request, owner.token, projectId, member.userId);

    // Owner atanmış görev oluşturur (assignee = member).
    const task = await createTask(request, owner.token, projectId, member.userId);

    // Atanmış üye yalnız kendi görevinin DURUMUNU değiştirebilir.
    const statusChange = await request.put(
      `/api/projects/${projectId}/tasks/${task.id}`,
      {
        ...bearer(member.token),
        data: {
          title: "Task",
          description: null,
          status: "InProgress",
          priority: "Medium",
          dueDate: null,
          assigneeUserId: member.userId,
          version: task.version,
        },
      },
    );
    expect(statusChange.status(), "assignee status change → 200").toBe(200);

    // Atanmış üye başka bir alanı (başlık) değiştiremez → 403.
    const renameByAssignee = await request.put(
      `/api/projects/${projectId}/tasks/${task.id}`,
      {
        ...bearer(member.token),
        data: {
          title: "Renamed by assignee",
          description: null,
          status: "InProgress",
          priority: "Medium",
          dueDate: null,
          assigneeUserId: member.userId,
        },
      },
    );
    expect(renameByAssignee.status(), "assignee field edit → 403").toBe(403);

    // Üye görev silemez → 403.
    const memberDelete = await request.delete(
      `/api/projects/${projectId}/tasks/${task.id}`,
      bearer(member.token),
    );
    expect(memberDelete.status(), "member delete → 403").toBe(403);

    // Owner (sistem ProjectManager rolü olmadan) görevi silebilir → 204.
    const ownerDelete = await request.delete(
      `/api/projects/${projectId}/tasks/${task.id}`,
      bearer(owner.token),
    );
    expect(ownerDelete.status(), "owner delete → 204").toBe(204);
  });
});

test.describe("Optimistic concurrency (gerçek backend)", () => {
  test("bayat sürümle güncelleme 409 döner ve veriyi ezmez (B10-06)", async ({ request }) => {
    const owner = await registerUser(request);
    const projectId = await createProject(request, owner.token);
    const task = await createTask(request, owner.token, projectId);

    // İlk güncelleme sürümü ilerletir.
    const first = await request.put(`/api/projects/${projectId}/tasks/${task.id}`, {
      ...bearer(owner.token),
      data: {
        title: "First wins",
        description: null,
        status: "InProgress",
        priority: "Medium",
        dueDate: null,
        assigneeUserId: null,
        version: task.version,
      },
    });
    expect(first.status()).toBe(200);

    // Aynı (artık bayat) sürümle ikinci güncelleme reddedilir.
    const stale = await request.put(`/api/projects/${projectId}/tasks/${task.id}`, {
      ...bearer(owner.token),
      data: {
        title: "Second overwrites",
        description: null,
        status: "InProgress",
        priority: "Medium",
        dueDate: null,
        assigneeUserId: null,
        version: task.version,
      },
    });
    expect(stale.status(), "stale update → 409").toBe(409);

    // Kazanan düzenleme korunur.
    const current = await request.get(`/api/projects/${projectId}/tasks/${task.id}`, bearer(owner.token));
    expect((await current.json()).title).toBe("First wins");
  });
});

test.describe("Yorum sayfalaması (gerçek backend)", () => {
  test("ilk sayfa en yeni yorumları döndürür (B10-04)", async ({ request }) => {
    const owner = await registerUser(request);
    const projectId = await createProject(request, owner.token);
    const task = await createTask(request, owner.token, projectId);

    for (let index = 0; index < 21; index++) {
      await addComment(request, owner.token, projectId, task.id, `comment-${String(index).padStart(3, "0")}`);
    }

    const page1 = await request.get(
      `/api/projects/${projectId}/tasks/${task.id}/comments?page=1&pageSize=20`,
      bearer(owner.token),
    );
    expect(page1.status()).toBe(200);
    const first = await page1.json();
    expect(first.totalCount).toBe(21);
    // En yeni ilk: page-020 en üstte, page-001 en altta (page-000 ikinci sayfaya kayar).
    expect(first.items[0].content).toBe("comment-020");
    expect(first.items[19].content).toBe("comment-001");

    const page2 = await request.get(
      `/api/projects/${projectId}/tasks/${task.id}/comments?page=2&pageSize=20`,
      bearer(owner.token),
    );
    const second = await page2.json();
    expect(second.items).toHaveLength(1);
    expect(second.items[0].content).toBe("comment-000");
  });
});

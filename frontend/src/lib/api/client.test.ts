import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiFetch, setSessionExpiredHandler } from "./client";
import { ApiError, NetworkError } from "./problem";
import { tokenStore } from "@/lib/auth/token-store";
import type { AuthResponse } from "./types";

const user = {
  id: "11111111-1111-1111-1111-111111111111",
  email: "ayse@ornek.com",
  userName: "aysedemir",
  displayName: "Ayşe Demir",
  roles: ["Member"],
};

function authResponse(suffix: string): AuthResponse {
  return {
    accessToken: `access-${suffix}`,
    expiresAtUtc: new Date(Date.now() + 3600_000).toISOString(),
    refreshToken: `refresh-${suffix}`,
    refreshTokenExpiresAtUtc: new Date(Date.now() + 86_400_000).toISOString(),
    user,
  };
}

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function problemResponse(status: number, detail?: string): Response {
  return new Response(
    JSON.stringify({ status, title: `HTTP ${status}`, detail }),
    { status, headers: { "Content-Type": "application/problem+json" } },
  );
}

describe("apiFetch", () => {
  beforeEach(() => {
    tokenStore.setFromAuthResponse(authResponse("initial"));
  });

  afterEach(() => {
    tokenStore.clear();
    setSessionExpiredHandler(null);
    vi.restoreAllMocks();
  });

  it("Authorization başlığını ekler ve JSON yanıtı döndürür", async () => {
    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValue(jsonResponse([{ id: "p1" }]));

    const result = await apiFetch<{ id: string }[]>("/api/projects");

    expect(result).toEqual([{ id: "p1" }]);
    const [, init] = fetchMock.mock.calls[0]!;
    expect(new Headers(init?.headers).get("Authorization")).toBe(
      "Bearer access-initial",
    );
  });

  it("eşzamanlı 401 yanıtları tek refresh isteği paylaşır", async () => {
    let refreshCalls = 0;
    let protectedCalls = 0;

    vi.spyOn(globalThis, "fetch").mockImplementation(async (input, init) => {
      const url = String(input);
      if (url.endsWith("/api/auth/refresh")) {
        refreshCalls += 1;
        await new Promise((resolve) => setTimeout(resolve, 10));
        return jsonResponse(authResponse("renewed"));
      }
      protectedCalls += 1;
      const token = new Headers(init?.headers).get("Authorization");
      if (token === "Bearer access-renewed") return jsonResponse({ ok: true });
      return problemResponse(401);
    });

    const [a, b, c] = await Promise.all([
      apiFetch<{ ok: boolean }>("/api/projects"),
      apiFetch<{ ok: boolean }>("/api/notifications"),
      apiFetch<{ ok: boolean }>("/api/projects/x/tasks"),
    ]);

    expect(refreshCalls).toBe(1);
    expect(a.ok && b.ok && c.ok).toBe(true);
    // 3 başarısız + 3 tekrar = 6 korumalı çağrı
    expect(protectedCalls).toBe(6);
    // Yeni tokenlar atomik yazıldı.
    expect(tokenStore.get()?.refreshToken).toBe("refresh-renewed");
  });

  it("başka sekme zaten yenilediyse ikinci kez refresh etmez", async () => {
    // Sekmeler arası koordinasyon: bu sekme 401 alırken başka bir sekme token'ı
    // yenilemiş olsun. Kilit alındıktan sonra access token değişmiş görülür ve
    // /api/auth/refresh hiç çağrılmadan yeni token ile istek yinelenir.
    let refreshCalls = 0;
    let firstProtected = true;

    vi.spyOn(globalThis, "fetch").mockImplementation(async (input, init) => {
      const url = String(input);
      if (url.endsWith("/api/auth/refresh")) {
        refreshCalls += 1;
        return jsonResponse(authResponse("renewed"));
      }
      const token = new Headers(init?.headers).get("Authorization");
      if (token === "Bearer access-renewed") return jsonResponse({ ok: true });
      if (firstProtected) {
        firstProtected = false;
        // Diğer sekmenin yenilemesini simüle et.
        tokenStore.setFromAuthResponse(authResponse("renewed"));
        return problemResponse(401);
      }
      return problemResponse(401);
    });

    const result = await apiFetch<{ ok: boolean }>("/api/projects");

    expect(result.ok).toBe(true);
    expect(refreshCalls).toBe(0);
    expect(tokenStore.get()?.accessToken).toBe("access-renewed");
  });

  it("refresh başarısızsa oturumu temizler ve expired handler'ı çağırır", async () => {
    const onExpired = vi.fn();
    setSessionExpiredHandler(onExpired);

    vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
      const url = String(input);
      if (url.endsWith("/api/auth/refresh")) return problemResponse(401);
      return problemResponse(401);
    });

    await expect(apiFetch("/api/projects")).rejects.toBeInstanceOf(ApiError);
    expect(onExpired).toHaveBeenCalledTimes(1);
    expect(tokenStore.get()).toBeNull();
  });

  it("Problem Details gövdesini ApiError olarak fırlatır", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      problemResponse(409, "Son SuperAdmin kaldırılamaz."),
    );

    const error: ApiError = await apiFetch("/api/admin/users/x/roles", {
      method: "PUT",
      body: { roles: [] },
    }).then(
      () => {
        throw new Error("İsteğin başarısız olması bekleniyordu.");
      },
      (cause) => cause as ApiError,
    );

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(409);
    expect(error.problem.detail).toBe("Son SuperAdmin kaldırılamaz.");
  });

  it("ağ hatasını NetworkError'a çevirir", async () => {
    vi.spyOn(globalThis, "fetch").mockRejectedValue(new TypeError("failed"));

    await expect(apiFetch("/api/projects")).rejects.toBeInstanceOf(NetworkError);
  });
});

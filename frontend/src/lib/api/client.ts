import { apiBaseUrl } from "@/lib/env";
import { ApiError, NetworkError, type ProblemDetails } from "./problem";
import { tokenStore } from "@/lib/auth/token-store";
import type { AuthResponse } from "./types";

// Tek doğruluk kaynağı: FRONTEND-INTEGRATION.md "Authentication akışı".
// Aynı anda gelen 401'ler tek refresh isteğini paylaşır; refresh başarısızsa
// oturum temizlenir ve kayıtlı dinleyici (auth-context) girişe yönlendirir.

let refreshPromise: Promise<boolean> | null = null;
let onSessionExpired: (() => void) | null = null;

export function setSessionExpiredHandler(handler: (() => void) | null) {
  onSessionExpired = handler;
}

async function parseProblem(response: Response): Promise<ProblemDetails> {
  try {
    const text = await response.text();
    if (text) return JSON.parse(text) as ProblemDetails;
  } catch {
    // gövde JSON değilse aşağıdaki fallback kullanılır
  }
  return { status: response.status, title: `HTTP ${response.status}` };
}

function retryAfterSeconds(response: Response): number | null {
  const header = response.headers.get("Retry-After");
  if (!header) return null;
  const seconds = Number(header);
  return Number.isFinite(seconds) && seconds >= 0 ? seconds : null;
}

/** Tekil refresh kuyruğu: eşzamanlı çağrılar aynı promise'i bekler. */
async function refreshSession(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const session = tokenStore.get();
    if (!session) return false;
    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: session.refreshToken }),
      });
      if (!response.ok) return false;
      const auth = (await response.json()) as AuthResponse;
      // Yeni access+refresh token atomik olarak eskilerinin yerini alır.
      tokenStore.setFromAuthResponse(auth);
      return true;
    } catch {
      return false;
    } finally {
      // Promise çözülmeden null'lamak yeni kuyruk açılmasına izin verirdi;
      // burada sıralama garanti: önce sonuç, sonra sıfırlama.
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

export interface ApiFetchOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  /** true ise Authorization başlığı eklenmez (login/register/refresh). */
  anonymous?: boolean;
  /** FormData gönderirken Content-Type otomatik bırakılır. */
  rawBody?: BodyInit;
}

async function doFetch(path: string, options: ApiFetchOptions): Promise<Response> {
  const { body, anonymous, rawBody, headers, ...rest } = options;
  const requestHeaders = new Headers(headers);
  if (!anonymous) {
    const session = tokenStore.get();
    if (session) {
      requestHeaders.set("Authorization", `Bearer ${session.accessToken}`);
    }
  }
  let requestBody: BodyInit | undefined = rawBody;
  if (body !== undefined) {
    requestHeaders.set("Content-Type", "application/json");
    requestBody = JSON.stringify(body);
  }
  try {
    return await fetch(`${apiBaseUrl}${path}`, {
      ...rest,
      headers: requestHeaders,
      body: requestBody,
    });
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === "AbortError") throw cause;
    throw new NetworkError(cause);
  }
}

/**
 * JSON API çağrısı. 401'de tekil refresh dener ve isteği bir kez yineler;
 * refresh başarısızsa oturumu kapatır ve oturum-süresi-doldu akışını tetikler.
 */
export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const response = await fetchWithAuthRetry(path, options);

  if (!response.ok) {
    throw new ApiError(
      response.status,
      await parseProblem(response),
      retryAfterSeconds(response),
    );
  }
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/** Blob indiren varyant (ek dosyası içerikleri). */
export async function apiFetchBlob(
  path: string,
  options: ApiFetchOptions = {},
): Promise<{ blob: Blob; fileName: string | null }> {
  const response = await fetchWithAuthRetry(path, options);
  if (!response.ok) {
    throw new ApiError(
      response.status,
      await parseProblem(response),
      retryAfterSeconds(response),
    );
  }
  const disposition = response.headers.get("Content-Disposition");
  let fileName: string | null = null;
  if (disposition) {
    const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
    const plain = /filename="?([^";]+)"?/i.exec(disposition);
    fileName = utf8 ? decodeURIComponent(utf8[1]) : (plain?.[1] ?? null);
  }
  return { blob: await response.blob(), fileName };
}

async function fetchWithAuthRetry(
  path: string,
  options: ApiFetchOptions,
): Promise<Response> {
  let response = await doFetch(path, options);

  if (response.status === 401 && !options.anonymous && tokenStore.get()) {
    const refreshed = await refreshSession();
    if (refreshed) {
      response = await doFetch(path, options);
    } else {
      tokenStore.clear("expired");
      onSessionExpired?.();
    }
  }
  return response;
}

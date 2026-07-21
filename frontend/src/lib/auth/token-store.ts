import type { AuthResponse, UserResponse } from "@/lib/api/types";

// MVP kararı (DESIGN-DECISIONS.md §9): tokenlar localStorage'ta tutulur; XSS
// riski kabul edilmiştir, production için BFF/HttpOnly cookie post-MVP konusudur.
// Sekmeler arası tutarlılık için storage event'i dinlenir (auth-context içinde).

const STORAGE_KEY = "taskmanagement.session";

export interface Session {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserResponse;
}

type Listener = () => void;

let cached: Session | null | undefined;
let expired = false;
const listeners = new Set<Listener>();

function read(): Session | null {
  if (typeof window === "undefined") return null;
  if (cached !== undefined) return cached;
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    cached = raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    cached = null;
  }
  return cached;
}

function notify() {
  for (const listener of listeners) listener();
}

export const tokenStore = {
  get(): Session | null {
    return read();
  },

  /** Login/register/refresh yanıtını atomik olarak yazar. */
  setFromAuthResponse(response: AuthResponse) {
    const session: Session = {
      accessToken: response.accessToken,
      accessTokenExpiresAtUtc: response.expiresAtUtc,
      refreshToken: response.refreshToken,
      refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc,
      user: response.user,
    };
    cached = session;
    expired = false;
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    notify();
  },

  /**
   * Oturumu temizler. `reason: "expired"` ile temizlenen oturumda korumalı rota
   * koruması da girişe "oturum süresi doldu" bildirimiyle yönlendirir; aksi
   * halde iki yönlendirme yarışır ve kullanıcı sebebini göremez.
   */
  clear(reason?: "expired") {
    cached = null;
    expired = reason === "expired";
    if (typeof window !== "undefined") {
      window.localStorage.removeItem(STORAGE_KEY);
    }
    notify();
  },

  /** Son temizlemenin oturum süresi dolmasından kaynaklanıp kaynaklanmadığı. */
  wasExpired(): boolean {
    return expired;
  },

  /** storage event'i sonrası önbelleği tazeler (diğer sekme yazdıysa). */
  invalidateCache() {
    cached = undefined;
    notify();
  },

  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
};

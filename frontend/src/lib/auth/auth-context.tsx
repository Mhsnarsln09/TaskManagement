"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useSyncExternalStore,
} from "react";
import { usePathname, useRouter } from "next/navigation";
import { authApi } from "@/lib/api/endpoints";
import { setSessionExpiredHandler } from "@/lib/api/client";
import { tokenStore, type Session } from "./token-store";
import { userDirectory } from "@/lib/user-directory";
import type {
  ApplicationRole,
  LoginRequest,
  RegisterRequest,
  UserResponse,
} from "@/lib/api/types";

interface AuthContextValue {
  /** İlk render'da localStorage okunana kadar undefined kalmaz; SSR'da null. */
  user: UserResponse | null;
  isAuthenticated: boolean;
  hasRole: (role: ApplicationRole) => boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function useSession(): Session | null {
  return useSyncExternalStore(
    tokenStore.subscribe,
    () => tokenStore.get(),
    () => null,
  );
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const session = useSession();
  const router = useRouter();

  // Oturum süresi doldu: token temizlendi → girişe yönlendir, dönüş URL'si korunur.
  useEffect(() => {
    setSessionExpiredHandler(() => {
      const returnUrl = window.location.pathname + window.location.search;
      router.replace(
        `/login?expired=1&returnUrl=${encodeURIComponent(returnUrl)}`,
      );
    });
    return () => setSessionExpiredHandler(null);
  }, [router]);

  // Diğer sekmede login/logout olursa bu sekme de senkronlanır.
  useEffect(() => {
    const onStorage = (event: StorageEvent) => {
      if (event.key === "taskmanagement.session") tokenStore.invalidateCache();
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  // Oturum kullanıcısı kullanıcı dizinini besler (DESIGN-DECISIONS.md §1).
  useEffect(() => {
    if (session?.user) userDirectory.upsert(session.user);
  }, [session?.user]);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await authApi.login(request);
    tokenStore.setFromAuthResponse(response);
  }, []);

  const register = useCallback(async (request: RegisterRequest) => {
    const response = await authApi.register(request);
    // Kayıt yanıtı token içerir → otomatik giriş (DESIGN-DECISIONS.md §14).
    tokenStore.setFromAuthResponse(response);
  }, []);

  const logout = useCallback(async () => {
    // Sunucu taraflı çıkış: refresh token ailesini iptal et (B10-03). Sonuçtan
    // bağımsız olarak yerel oturumu temizle — ağ hatası çıkışı engellememeli.
    const refreshToken = tokenStore.get()?.refreshToken;
    if (refreshToken) {
      try {
        await authApi.logout({ refreshToken });
      } catch {
        // İdempotent uç; başarısızlıkta bile yerel temizlik yapılır.
      }
    }
    // localStorage.removeItem diğer sekmelerde storage event'i tetikler; onlar da
    // oturumu boş görüp girişe yönlenir (çıkışın sekmeler arası yayılması).
    tokenStore.clear();
    userDirectory.clear();
    router.replace("/login");
  }, [router]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      isAuthenticated: session !== null,
      hasRole: (role) => session?.user.roles.includes(role) ?? false,
      login,
      register,
      logout,
    }),
    [session, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth, AuthProvider içinde kullanılmalıdır.");
  return context;
}

const emptySubscribe = () => () => {};

/** Hydration tamamlanana kadar false; localStorage ancak sonrasında güvenilirdir. */
function useHydrated(): boolean {
  return useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false,
  );
}

/**
 * Korumalı rota kancası: oturum yoksa girişe yönlendirir.
 * localStorage yalnız istemcide okunabildiği için guard istemcidedir; hydration
 * bitmeden yönlendirme yapılmaz (ilk render'da sunucu snapshot'ı null döner).
 */
export function useRequireAuth() {
  const { isAuthenticated } = useAuth();
  const hydrated = useHydrated();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (hydrated && !isAuthenticated) {
      // Oturum süresi dolduysa sebebi taşı; yoksa sade yönlendirme yeterli.
      const expired = tokenStore.wasExpired() ? "expired=1&" : "";
      router.replace(`/login?${expired}returnUrl=${encodeURIComponent(pathname)}`);
    }
  }, [hydrated, isAuthenticated, router, pathname]);

  return hydrated && isAuthenticated;
}

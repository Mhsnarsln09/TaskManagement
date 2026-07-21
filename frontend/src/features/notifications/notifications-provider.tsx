"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { notificationsApi } from "@/lib/api/endpoints";
import type { NotificationResponse } from "@/lib/api/types";
import { useNotificationsHub, type HubStatus } from "@/lib/realtime/notifications-hub";
import { useAuth } from "@/lib/auth/auth-context";

// SignalR kalıcı veri kaynağı değildir: canlı geliş yalnız toast + cache'e ekleme
// yapar; ilk bağlantı ve reconnect sonrası liste her zaman API'den yenilenir.

interface NotificationsContextValue {
  hubStatus: HubStatus;
  /** Yüklenen sayfalardaki okunmamışlar üzerinden rozet sayısı. */
  markRead: (id: string) => Promise<void>;
  markManyRead: (ids: string[]) => Promise<{ failed: number }>;
}

const NotificationsContext = createContext<NotificationsContextValue | null>(null);

export const NOTIFICATIONS_QUERY_KEY = ["notifications"] as const;

export function NotificationsProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  // Aynı bildirim hem hub'dan hem yenilenen listeden gelirse iki kez toast atmayı önler.
  const [seenToastIds] = useState(() => new Set<string>());

  const invalidate = useCallback(() => {
    void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_QUERY_KEY });
  }, [queryClient]);

  const onNotification = useCallback(
    (notification: NotificationResponse) => {
      if (!seenToastIds.has(notification.id)) {
        seenToastIds.add(notification.id);
        // Karar §2: aktör/proje dekorasyonu ve gezinme yok; yalnız mesaj.
        toast.info("Yeni bildirim", { description: notification.message });
      }
      invalidate();
    },
    [invalidate, seenToastIds],
  );

  const hubStatus = useNotificationsHub({
    enabled: isAuthenticated,
    onNotification,
    onSynced: invalidate,
  });

  const markRead = useCallback(
    async (id: string) => {
      await notificationsApi.markRead(id);
      invalidate();
    },
    [invalidate],
  );

  // Toplu endpoint yok (karar §3): yüklenmiş okunmamışlara paralel PUT.
  const markManyRead = useCallback(
    async (ids: string[]) => {
      const results = await Promise.allSettled(
        ids.map((id) => notificationsApi.markRead(id)),
      );
      invalidate();
      const failed = results.filter((result) => result.status === "rejected").length;
      return { failed };
    },
    [invalidate],
  );

  const value = useMemo(
    () => ({ hubStatus, markRead, markManyRead }),
    [hubStatus, markRead, markManyRead],
  );

  return (
    <NotificationsContext.Provider value={value}>
      {children}
    </NotificationsContext.Provider>
  );
}

export function useNotifications(): NotificationsContextValue {
  const context = useContext(NotificationsContext);
  if (!context) {
    throw new Error("useNotifications, NotificationsProvider içinde kullanılmalıdır.");
  }
  return context;
}

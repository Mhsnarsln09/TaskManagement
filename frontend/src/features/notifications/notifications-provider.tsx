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
import { notificationTitle } from "./notification-text";

// SignalR kalıcı veri kaynağı değildir: canlı geliş yalnız toast + cache'e ekleme
// yapar; ilk bağlantı ve reconnect sonrası liste her zaman API'den yenilenir.

interface NotificationsContextValue {
  hubStatus: HubStatus;
  markRead: (id: string) => Promise<void>;
  /** Sunucu taraflı: tek istekte tüm bildirimleri okundu yapar (B10-07). */
  markAllRead: () => Promise<void>;
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
        // F10-05: başlık yapılandırılmış `type`'tan Türkçeleştirilir; backend `message`
        // ikincil ayrıntı olarak gösterilir. Karar §2 uyarınca gezinme eylemi yok.
        toast.info(notificationTitle(notification.type), {
          description: notification.message,
        });
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

  // B10-07: tek sunucu-taraflı endpoint bütün okunmamışları (yalnız yüklü sayfayı değil)
  // okundu yapar; idempotenttir.
  const markAllRead = useCallback(async () => {
    await notificationsApi.markAllRead();
    invalidate();
  }, [invalidate]);

  const value = useMemo(
    () => ({ hubStatus, markRead, markAllRead }),
    [hubStatus, markRead, markAllRead],
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

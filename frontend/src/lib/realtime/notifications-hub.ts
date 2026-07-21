"use client";

import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { useEffect, useRef, useState } from "react";
import { notificationsHubUrl } from "@/lib/env";
import { tokenStore } from "@/lib/auth/token-store";
import type { NotificationResponse } from "@/lib/api/types";

export type HubStatus = "connecting" | "connected" | "reconnecting" | "disconnected";

interface UseNotificationsHubOptions {
  enabled: boolean;
  onNotification: (notification: NotificationResponse) => void;
  /** İlk bağlantı ve reconnect sonrası liste yenilenir (SignalR kalıcı kaynak değildir). */
  onSynced: () => void;
}

export function useNotificationsHub({
  enabled,
  onNotification,
  onSynced,
}: UseNotificationsHubOptions): HubStatus {
  // Bağlantı yaşam döngüsü asenkron callback'lerde durumu günceller; efekt
  // gövdesinde senkron setState yoktur. enabled=false görünümü türetilir.
  const [status, setStatus] = useState<HubStatus>("connecting");
  const callbacksRef = useRef({ onNotification, onSynced });

  useEffect(() => {
    callbacksRef.current = { onNotification, onSynced };
  }, [onNotification, onSynced]);

  useEffect(() => {
    if (!enabled) return;

    let disposed = false;
    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(notificationsHubUrl, {
        accessTokenFactory: () => tokenStore.get()?.accessToken ?? "",
      })
      .withAutomaticReconnect()
      // React StrictMode'un çift efekt çalıştırması ilk start'ı iptal eder;
      // SignalR bunu console.error'a yazar. Durum zaten UI'da izlenir.
      .configureLogging(LogLevel.Critical)
      .build();

    connection.on("notificationReceived", (notification: NotificationResponse) => {
      callbacksRef.current.onNotification(notification);
    });
    connection.onreconnecting(() => {
      if (!disposed) setStatus("reconnecting");
    });
    connection.onreconnected(() => {
      if (disposed) return;
      setStatus("connected");
      callbacksRef.current.onSynced();
    });
    connection.onclose(() => {
      if (!disposed) setStatus("disconnected");
    });

    connection
      .start()
      .then(() => {
        if (disposed) return;
        setStatus("connected");
        callbacksRef.current.onSynced();
      })
      .catch(() => {
        if (!disposed) setStatus("disconnected");
      });

    return () => {
      disposed = true;
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, [enabled]);

  return enabled ? status : "disconnected";
}

"use client";

import { cn } from "@/lib/utils";
import { formatRelative } from "@/lib/dates";
import type { NotificationResponse } from "@/lib/api/types";

// Karar §2: yalnız message + göreli zaman; okunmamış sol vurgu noktası.
export function NotificationItem({
  notification,
  onMarkRead,
  className,
}: {
  notification: NotificationResponse;
  onMarkRead: (id: string) => void;
  className?: string;
}) {
  const unread = !notification.isRead;
  return (
    <button
      type="button"
      onClick={() => {
        if (unread) onMarkRead(notification.id);
      }}
      aria-label={
        unread ? "Bildirimi okundu işaretle" : "Bildirim (okundu)"
      }
      className={cn(
        "flex w-full items-start gap-2.5 rounded-md px-3 py-2.5 text-left transition-colors",
        unread ? "bg-accent/40 hover:bg-accent/70" : "hover:bg-muted",
        className,
      )}
    >
      <span
        aria-hidden
        className={cn(
          "mt-1.5 size-2 shrink-0 rounded-full",
          unread ? "bg-primary" : "bg-transparent",
        )}
      />
      <span className="min-w-0 flex-1">
        <span
          className={cn(
            "block text-sm",
            unread ? "font-medium" : "text-muted-foreground",
          )}
        >
          {notification.message}
        </span>
        <span className="mt-0.5 block text-xs text-muted-foreground">
          {formatRelative(notification.createdAtUtc)}
        </span>
      </span>
    </button>
  );
}

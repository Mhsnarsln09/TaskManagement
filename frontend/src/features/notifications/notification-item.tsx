"use client";

import Link from "next/link";
import { cn } from "@/lib/utils";
import { formatRelative } from "@/lib/dates";
import type { NotificationResponse } from "@/lib/api/types";
import { notificationHref, notificationTitle } from "./notification-text";

// F10-05: birincil metin `type`'tan Türkçeleştirilir; backend `message` ikincil
// ayrıntı olarak gösterilir. projectId/taskItemId varsa satır ilgili göreve linktir
// ve tıklanınca okundu işaretlenir.
export function NotificationItem({
  notification,
  onMarkRead,
  onNavigate,
  className,
}: {
  notification: NotificationResponse;
  onMarkRead: (id: string) => void;
  onNavigate?: () => void;
  className?: string;
}) {
  const unread = !notification.isRead;
  const href = notificationHref(notification);

  const body = (
    <>
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
          {notificationTitle(notification.type)}
        </span>
        {notification.message ? (
          <span className="mt-0.5 block truncate text-xs text-muted-foreground">
            {notification.message}
          </span>
        ) : null}
        <span className="mt-0.5 block text-xs text-muted-foreground">
          {formatRelative(notification.createdAtUtc)}
        </span>
      </span>
    </>
  );

  const classes = cn(
    "flex w-full items-start gap-2.5 rounded-md px-3 py-2.5 text-left transition-colors",
    unread ? "bg-accent/40 hover:bg-accent/70" : "hover:bg-muted",
    className,
  );

  function handleActivate() {
    if (unread) onMarkRead(notification.id);
    onNavigate?.();
  }

  // İlgili göreve gidilebiliyorsa link; değilse yalnız okundu işaretleyen düğme.
  if (href) {
    return (
      <Link
        href={href}
        onClick={handleActivate}
        aria-label={`${notificationTitle(notification.type)} — göreve git`}
        className={classes}
      >
        {body}
      </Link>
    );
  }

  return (
    <button
      type="button"
      onClick={handleActivate}
      aria-label={unread ? "Bildirimi okundu işaretle" : "Bildirim (okundu)"}
      className={classes}
    >
      {body}
    </button>
  );
}

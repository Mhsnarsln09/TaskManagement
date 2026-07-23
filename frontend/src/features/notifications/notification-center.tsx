"use client";

import { useQuery } from "@tanstack/react-query";
import { Bell, BellOff, Loader2 } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { toast } from "sonner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Skeleton } from "@/components/ui/skeleton";
import { notificationsApi } from "@/lib/api/endpoints";
import {
  NOTIFICATIONS_QUERY_KEY,
  useNotifications,
} from "./notifications-provider";
import { NotificationItem } from "./notification-item";

// Zil + popover (tasarım §08). Mobilde tam görünüm /notifications rotasındadır.
export function NotificationCenter() {
  const [open, setOpen] = useState(false);
  const [markingAll, setMarkingAll] = useState(false);
  const { markRead, markAllRead } = useNotifications();

  const { data, isPending } = useQuery({
    queryKey: [...NOTIFICATIONS_QUERY_KEY, "popover"],
    queryFn: ({ signal }) => notificationsApi.list(1, 10, signal),
  });

  // Rozet, ilk 10 kayıttan değil sunucudaki gerçek toplam okunmamış sayısından beslenir.
  const unreadCountQuery = useQuery({
    queryKey: [...NOTIFICATIONS_QUERY_KEY, "unread-count"],
    queryFn: ({ signal }) => notificationsApi.unreadCount(signal),
  });
  const unreadCount = unreadCountQuery.data?.unreadCount ?? 0;

  async function onMarkAll() {
    setMarkingAll(true);
    try {
      await markAllRead();
    } catch {
      toast.error("Bildirimler okundu işaretlenemedi.");
    } finally {
      setMarkingAll(false);
    }
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={
            unreadCount > 0 ? `Bildirimler, ${unreadCount} yeni` : "Bildirimler"
          }
          className="relative"
        >
          <Bell aria-hidden className="size-4" />
          {unreadCount > 0 ? (
            <span
              aria-hidden
              className="absolute right-1.5 top-1.5 flex size-2 rounded-full bg-destructive"
            />
          ) : null}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-90 p-0">
        <div className="flex items-center justify-between gap-2 border-b px-3 py-2.5">
          <span className="flex items-center gap-2 text-sm font-semibold">
            Bildirimler
            {unreadCount > 0 ? (
              <Badge className="px-1.5 py-0 text-[10px]">{unreadCount} yeni</Badge>
            ) : null}
          </span>
          {unreadCount > 0 ? (
            <Button
              type="button"
              variant="ghost"
              size="xs"
              onClick={onMarkAll}
              disabled={markingAll}
            >
              {markingAll ? (
                <Loader2 aria-hidden className="size-3 animate-spin" />
              ) : null}
              Tümünü okundu işaretle
            </Button>
          ) : null}
        </div>
        <div className="max-h-96 overflow-y-auto p-1.5">
          {isPending ? (
            <div className="space-y-2 p-2" aria-hidden>
              <Skeleton className="h-10" />
              <Skeleton className="h-10" />
              <Skeleton className="h-10" />
            </div>
          ) : data && data.items.length > 0 ? (
            data.items.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onMarkRead={(id) => void markRead(id)}
                onNavigate={() => setOpen(false)}
              />
            ))
          ) : (
            <div className="flex flex-col items-center gap-1.5 px-4 py-10 text-center">
              <BellOff aria-hidden className="size-6 text-muted-foreground/60" />
              <p className="text-sm font-medium">Bildiriminiz yok</p>
              <p className="text-xs text-muted-foreground">
                Görev atamaları, durum değişiklikleri ve son tarih hatırlatmaları
                burada görünür.
              </p>
            </div>
          )}
        </div>
        <div className="border-t p-1.5">
          <Button asChild type="button" variant="ghost" size="sm" className="w-full">
            <Link href="/notifications" onClick={() => setOpen(false)}>
              Tümünü görüntüle
            </Link>
          </Button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

"use client";

import { useInfiniteQuery } from "@tanstack/react-query";
import { BellOff, Loader2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/shared/page-header";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { EmptyState } from "@/components/shared/states";
import { notificationsApi } from "@/lib/api/endpoints";
import { totalPagesOf } from "@/lib/api/types";
import {
  NOTIFICATIONS_QUERY_KEY,
  useNotifications,
} from "./notifications-provider";
import { NotificationItem } from "./notification-item";

// Tasarım §08/§13: mobilde popover yerine tam sayfa; aynı NotificationItem.
const PAGE_SIZE = 20;

export function NotificationsPage() {
  const { markRead, markManyRead } = useNotifications();
  const [markingAll, setMarkingAll] = useState(false);

  const query = useInfiniteQuery({
    queryKey: [...NOTIFICATIONS_QUERY_KEY, "page"],
    queryFn: ({ pageParam, signal }) =>
      notificationsApi.list(pageParam, PAGE_SIZE, signal),
    initialPageParam: 1,
    getNextPageParam: (lastPage) =>
      lastPage.page < totalPagesOf(lastPage) ? lastPage.page + 1 : undefined,
  });

  const items = query.data?.pages.flatMap((page) => page.items) ?? [];
  const unread = items.filter((item) => !item.isRead);

  async function onMarkAll() {
    setMarkingAll(true);
    try {
      const { failed } = await markManyRead(unread.map((item) => item.id));
      if (failed > 0) toast.error(`${failed} bildirim okundu işaretlenemedi.`);
    } finally {
      setMarkingAll(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-2xl space-y-4">
      <PageHeader
        title="Bildirimler"
        description={
          unread.length > 0 ? `${unread.length} okunmamış bildirim` : undefined
        }
        actions={
          unread.length > 0 ? (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onMarkAll}
              disabled={markingAll}
            >
              {markingAll ? (
                <Loader2 aria-hidden className="size-3.5 animate-spin" />
              ) : null}
              Tümünü okundu işaretle
            </Button>
          ) : undefined
        }
      />

      {query.isPending ? (
        <div className="space-y-2" aria-hidden>
          {Array.from({ length: 5 }).map((_, index) => (
            <Skeleton key={index} className="h-14" />
          ))}
        </div>
      ) : query.error ? (
        <ProblemDetailsAlert
          error={query.error}
          title="Bildirimler yüklenemedi."
          onRetry={() => void query.refetch()}
        />
      ) : items.length === 0 ? (
        <EmptyState
          icon={BellOff}
          title="Bildiriminiz yok"
          description="Görev atamaları ve yorumlar burada görünür."
        />
      ) : (
        <>
          <div className="divide-y rounded-lg border bg-card">
            {items.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onMarkRead={(id) => void markRead(id)}
                className="rounded-none first:rounded-t-lg last:rounded-b-lg"
              />
            ))}
          </div>
          {query.hasNextPage ? (
            <Button
              type="button"
              variant="outline"
              className="w-full"
              disabled={query.isFetchingNextPage}
              onClick={() => void query.fetchNextPage()}
            >
              {query.isFetchingNextPage ? (
                <Loader2 aria-hidden className="size-4 animate-spin" />
              ) : null}
              Daha fazla yükle
            </Button>
          ) : null}
        </>
      )}
    </div>
  );
}

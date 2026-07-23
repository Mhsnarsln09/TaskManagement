"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Send } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { UserDisplay } from "@/components/shared/user-display";
import { commentsApi } from "@/lib/api/endpoints";
import { problemMessage } from "@/lib/api/problem";
import type { CommentResponse } from "@/lib/api/types";
import { formatDateTime } from "@/lib/dates";
import { userDirectory } from "@/lib/user-directory";

// Tasarım §06: sayfalı yorumlar, "daha eski yorumları yükle" + composer.
// Yorum yazarları UserSummaryResponse ile gelir ve kullanıcı dizinini besler.

const PAGE_SIZE = 20;

export function CommentsSection({
  projectId,
  taskId,
}: {
  projectId: string;
  taskId: string;
}) {
  const queryClient = useQueryClient();
  const [pagesToShow, setPagesToShow] = useState(1);
  const [content, setContent] = useState("");

  const firstPage = useQuery({
    queryKey: ["project", projectId, "task", taskId, "comments", 1],
    queryFn: ({ signal }) => commentsApi.list(projectId, taskId, 1, PAGE_SIZE, signal),
  });

  // Yorum yazarlarını dizine yaz (DESIGN-DECISIONS.md §1).
  useEffect(() => {
    if (firstPage.data) {
      userDirectory.upsertMany(firstPage.data.items.map((comment) => comment.author));
    }
  }, [firstPage.data]);

  const olderPages = useQuery({
    queryKey: ["project", projectId, "task", taskId, "comments", "older", pagesToShow],
    queryFn: async ({ signal }) => {
      const pages: CommentResponse[] = [];
      for (let page = 2; page <= pagesToShow; page += 1) {
        const result = await commentsApi.list(projectId, taskId, page, PAGE_SIZE, signal);
        pages.push(...result.items);
        userDirectory.upsertMany(result.items.map((comment) => comment.author));
      }
      return pages;
    },
    enabled: pagesToShow > 1,
  });

  const mutation = useMutation({
    mutationFn: (text: string) =>
      commentsApi.create(projectId, taskId, { content: text }),
    onSuccess: () => {
      setContent("");
      setPagesToShow(1);
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "task", taskId, "comments"],
      });
    },
    onError: (cause) => {
      toast.error("Yorum eklenemedi.", { description: problemMessage(cause) });
    },
  });

  const data = firstPage.data;
  const totalCount = data?.totalCount ?? 0;
  const olderComments = olderPages.data ?? [];
  // Backend "en yeni ilk sayfa" döndürür (B10-04): page 1 en yeni, sonraki sayfalar
  // daha eski. Yüklenmiş sayfaları newest-first sırada birleştirip (page 1 önce, sonra
  // daha eski sayfalar) tersine çeviririz; sonuç eski → yeni kronolojik akıştır ve
  // sayfa sınırlarında ne tekrar ne boşluk oluşur (tasarım §06, F10-01).
  const visible = [...(data?.items ?? []), ...olderComments].reverse();
  const remaining = totalCount - visible.length;

  return (
    <section aria-label="Yorumlar" className="space-y-3">
      <h3 className="text-sm font-semibold">
        Yorumlar{" "}
        <span className="font-normal text-muted-foreground">({totalCount})</span>
      </h3>

      {firstPage.isPending ? (
        <div className="space-y-2" aria-hidden>
          <Skeleton className="h-14" />
          <Skeleton className="h-14" />
        </div>
      ) : firstPage.error ? (
        <ProblemDetailsAlert
          error={firstPage.error}
          title="Yorumlar yüklenemedi."
          onRetry={() => void firstPage.refetch()}
        />
      ) : (
        <>
          {remaining > 0 ? (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="w-full text-muted-foreground"
              disabled={olderPages.isFetching}
              onClick={() => setPagesToShow((current) => current + 1)}
            >
              {olderPages.isFetching ? (
                <Loader2 aria-hidden className="size-3.5 animate-spin" />
              ) : null}
              ↑ Daha eski yorumları yükle ({remaining})
            </Button>
          ) : null}
          {visible.length === 0 ? (
            <p className="rounded-md border border-dashed px-3 py-6 text-center text-sm text-muted-foreground">
              Henüz yorum yok. İlk yorumu siz yazın.
            </p>
          ) : (
            <ul className="space-y-3">
              {visible.map((comment) => (
                <li key={comment.id} className="rounded-md border bg-card px-3 py-2.5">
                  <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs">
                    <UserDisplay
                      userId={comment.author.id}
                      className="font-semibold text-foreground"
                    />
                    <span className="text-muted-foreground">
                      {formatDateTime(comment.createdAtUtc)}
                    </span>
                  </div>
                  <p className="mt-1 whitespace-pre-wrap text-sm">{comment.content}</p>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      <form
        onSubmit={(event) => {
          event.preventDefault();
          const trimmed = content.trim();
          if (trimmed) mutation.mutate(trimmed);
        }}
        className="flex items-end gap-2"
      >
        <label htmlFor="comment-composer" className="sr-only">
          Yorum yaz
        </label>
        <Textarea
          id="comment-composer"
          rows={2}
          maxLength={2000}
          placeholder="Yorum yazın…"
          value={content}
          onChange={(event) => setContent(event.target.value)}
          disabled={mutation.isPending}
          className="flex-1 resize-none"
        />
        <Button
          type="submit"
          size="icon"
          aria-label="Yorumu gönder"
          disabled={mutation.isPending || content.trim().length === 0}
        >
          {mutation.isPending ? (
            <Loader2 aria-hidden className="size-4 animate-spin" />
          ) : (
            <Send aria-hidden className="size-4" />
          )}
        </Button>
      </form>
    </section>
  );
}

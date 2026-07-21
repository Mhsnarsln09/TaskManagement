"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FileText, Loader2, Paperclip, Upload } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { UserDisplay } from "@/components/shared/user-display";
import { attachmentsApi } from "@/lib/api/endpoints";
import { problemMessage } from "@/lib/api/problem";
import { formatDate } from "@/lib/dates";
import {
  ALLOWED_UPLOAD_EXTENSIONS,
  UPLOAD_RULES_TEXT,
  formatFileSize,
  validateUpload,
} from "@/lib/upload";
import { userDirectory } from "@/lib/user-directory";

// Tasarım §06/§13. Karar §4: 5 MB + png/jpg/jpeg/gif/pdf/txt/csv (backend aynası).
// Liste sayfasızdır (karar §10). İndirme bearer token gerektirdiği için blob ile yapılır.

export function AttachmentsSection({
  projectId,
  taskId,
}: {
  projectId: string;
  taskId: string;
}) {
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const listQuery = useQuery({
    queryKey: ["project", projectId, "task", taskId, "attachments"],
    queryFn: ({ signal }) => attachmentsApi.list(projectId, taskId, signal),
  });

  useEffect(() => {
    if (listQuery.data) {
      userDirectory.upsertMany(
        listQuery.data.map((attachment) => attachment.uploadedBy),
      );
    }
  }, [listQuery.data]);

  const uploadMutation = useMutation({
    mutationFn: (file: File) => attachmentsApi.upload(projectId, taskId, file),
    onSuccess: () => {
      setUploadError(null);
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "task", taskId, "attachments"],
      });
      toast.success("Dosya yüklendi.");
    },
    onError: (cause) => {
      setUploadError(problemMessage(cause));
    },
  });

  function onFileSelected(file: File | undefined) {
    if (!file) return;
    const validationError = validateUpload(file);
    if (validationError) {
      setUploadError(validationError);
      return;
    }
    setUploadError(null);
    uploadMutation.mutate(file);
  }

  async function onDownload(attachmentId: string, fallbackName: string) {
    setDownloadingId(attachmentId);
    try {
      const { blob, fileName } = await attachmentsApi.download(
        projectId,
        taskId,
        attachmentId,
      );
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName ?? fallbackName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (cause) {
      toast.error("Dosya indirilemedi.", { description: problemMessage(cause) });
    } finally {
      setDownloadingId(null);
    }
  }

  const attachments = listQuery.data ?? [];

  return (
    <section aria-label="Ekler" className="space-y-3">
      <div className="flex items-center justify-between gap-2">
        <h3 className="text-sm font-semibold">
          Ekler{" "}
          <span className="font-normal text-muted-foreground">
            ({attachments.length})
          </span>
        </h3>
        <input
          ref={inputRef}
          type="file"
          accept={ALLOWED_UPLOAD_EXTENSIONS.join(",")}
          className="sr-only"
          aria-label="Dosya seç"
          onChange={(event) => {
            onFileSelected(event.target.files?.[0]);
            event.target.value = "";
          }}
        />
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={uploadMutation.isPending}
          onClick={() => inputRef.current?.click()}
        >
          {uploadMutation.isPending ? (
            <>
              <Loader2 aria-hidden className="size-3.5 animate-spin" />
              Yükleniyor…
            </>
          ) : (
            <>
              <Upload aria-hidden className="size-3.5" />
              Dosya yükle
            </>
          )}
        </Button>
      </div>

      {uploadError ? (
        <p role="alert" className="text-sm text-destructive">
          {uploadError} {UPLOAD_RULES_TEXT}
        </p>
      ) : null}

      {listQuery.isPending ? (
        <div className="space-y-2" aria-hidden>
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
        </div>
      ) : listQuery.error ? (
        <ProblemDetailsAlert
          error={listQuery.error}
          title="Ekler yüklenemedi."
          onRetry={() => void listQuery.refetch()}
        />
      ) : attachments.length === 0 ? (
        <p className="flex items-center justify-center gap-2 rounded-md border border-dashed px-3 py-6 text-sm text-muted-foreground">
          <Paperclip aria-hidden className="size-4" />
          Henüz ek yok — dosya yükleyin.
        </p>
      ) : (
        <ul className="space-y-2">
          {attachments.map((attachment) => (
            <li
              key={attachment.id}
              className="flex items-center gap-3 rounded-md border bg-card px-3 py-2.5"
            >
              <FileText aria-hidden className="size-4 shrink-0 text-muted-foreground" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{attachment.fileName}</p>
                <p className="flex flex-wrap items-center gap-x-1.5 text-xs text-muted-foreground">
                  {formatFileSize(attachment.sizeInBytes)} ·{" "}
                  {formatDate(attachment.createdAtUtc)} ·{" "}
                  <UserDisplay userId={attachment.uploadedBy.id} showYouBadge={false} />
                </p>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={`${attachment.fileName} dosyasını indir`}
                disabled={downloadingId === attachment.id}
                onClick={() => void onDownload(attachment.id, attachment.fileName)}
              >
                {downloadingId === attachment.id ? (
                  <Loader2 aria-hidden className="size-4 animate-spin" />
                ) : (
                  <Download aria-hidden className="size-4" />
                )}
              </Button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

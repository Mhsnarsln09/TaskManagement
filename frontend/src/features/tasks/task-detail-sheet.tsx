"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Separator } from "@/components/ui/separator";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Skeleton } from "@/components/ui/skeleton";
import {
  OverdueBadge,
  PriorityBadge,
  StatusBadge,
} from "@/components/shared/badges";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { UserDisplay } from "@/components/shared/user-display";
import { projectsApi, tasksApi } from "@/lib/api/endpoints";
import { problemMessage } from "@/lib/api/problem";
import { canDeleteTasks } from "@/lib/permissions";
import { useAuth } from "@/lib/auth/auth-context";
import { formatDateOnly, formatDateTime } from "@/lib/dates";
import { CommentsSection } from "./comments-section";
import { AttachmentsSection } from "./attachments-section";
import { TaskFormDialog } from "./task-form-dialog";

// Tasarım §06: masaüstünde sağ Sheet (480px), mobilde tama yakın genişlik.
// Meta önce, sonra yorumlar ve ekler. Düzenleme her üyeye, silme yetkiye bağlı.

export function TaskDetailSheet({
  projectId,
  taskId,
  onClose,
}: {
  projectId: string;
  taskId: string;
  onClose: () => void;
}) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const taskQuery = useQuery({
    queryKey: ["project", projectId, "task", taskId],
    queryFn: ({ signal }) => tasksApi.get(projectId, taskId, signal),
  });

  const projectQuery = useQuery({
    queryKey: ["project", projectId],
    queryFn: ({ signal }) => projectsApi.get(projectId, signal),
  });

  const deleteMutation = useMutation({
    mutationFn: () => tasksApi.remove(projectId, taskId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["project", projectId, "tasks"] });
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "statistics"],
      });
      setDeleteOpen(false);
      toast.success("Görev silindi.");
      onClose();
    },
    onError: (cause) => {
      toast.error("Görev silinemedi.", { description: problemMessage(cause) });
    },
  });

  const task = taskQuery.data;
  const project = projectQuery.data;
  const allowDelete = project ? canDeleteTasks(user, project) : false;

  return (
    <>
      <Sheet
        open
        onOpenChange={(open) => {
          if (!open) onClose();
        }}
      >
        <SheetContent
          side="right"
          // Sheet primitifinin varsayılanı data-[side]:w-3/4; mobilde tasarım
          // tam sayfaya yakın panel ister, bu yüzden aynı varyantla ezilir.
          className="w-full gap-0 overflow-y-auto p-0 data-[side=right]:w-full sm:max-w-lg"
        >
          {taskQuery.isPending ? (
            <div className="space-y-3 p-5" aria-busy>
              <Skeleton className="h-7 w-3/4" />
              <Skeleton className="h-5 w-1/2" />
              <Skeleton className="h-24" />
            </div>
          ) : taskQuery.error ? (
            <div className="p-5 pt-10">
              <SheetTitle className="sr-only">Görev yüklenemedi</SheetTitle>
              <ProblemDetailsAlert
                error={taskQuery.error}
                title="Görev yüklenemedi."
                onRetry={() => void taskQuery.refetch()}
              />
            </div>
          ) : task ? (
            <>
              <SheetHeader className="gap-2 border-b p-5 pr-12">
                <SheetTitle className="text-base leading-snug">
                  {task.title}
                </SheetTitle>
                <SheetDescription className="sr-only">
                  Görev detayı
                </SheetDescription>
                <div className="flex flex-wrap items-center gap-1.5">
                  <StatusBadge status={task.status} />
                  <PriorityBadge priority={task.priority} />
                  {task.isOverdue ? <OverdueBadge /> : null}
                  <span className="flex-1" />
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setEditOpen(true)}
                  >
                    <Pencil aria-hidden className="size-3.5" />
                    Düzenle
                  </Button>
                  {allowDelete ? (
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon-sm"
                          aria-label="Diğer eylemler"
                        >
                          <MoreHorizontal aria-hidden className="size-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem
                          variant="destructive"
                          onSelect={() => setDeleteOpen(true)}
                        >
                          <Trash2 aria-hidden className="size-4" />
                          Sil
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  ) : null}
                </div>
              </SheetHeader>

              <div className="space-y-5 p-5">
                {task.description?.trim() ? (
                  <p className="whitespace-pre-wrap text-sm">{task.description}</p>
                ) : (
                  <p className="text-sm text-muted-foreground">Açıklama yok.</p>
                )}

                <dl className="grid grid-cols-2 gap-x-4 gap-y-3 text-sm">
                  <div>
                    <dt className="text-xs font-medium text-muted-foreground">
                      Son tarih
                    </dt>
                    <dd className="mt-0.5 flex items-center gap-1.5">
                      {task.dueDate ? formatDateOnly(task.dueDate) : "—"}
                      {task.isOverdue ? (
                        <span className="text-xs font-medium text-destructive">
                          · Gecikti
                        </span>
                      ) : null}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-xs font-medium text-muted-foreground">Atanan</dt>
                    <dd className="mt-0.5">
                      <UserDisplay userId={task.assigneeUserId} />
                    </dd>
                  </div>
                  <div>
                    <dt className="text-xs font-medium text-muted-foreground">
                      Oluşturulma
                    </dt>
                    <dd className="mt-0.5">{formatDateTime(task.createdAtUtc)}</dd>
                  </div>
                  <div>
                    <dt className="text-xs font-medium text-muted-foreground">
                      Güncelleme
                    </dt>
                    <dd className="mt-0.5">
                      {task.updatedAtUtc ? formatDateTime(task.updatedAtUtc) : "—"}
                    </dd>
                  </div>
                </dl>

                <Separator />
                <CommentsSection projectId={projectId} taskId={taskId} />
                <Separator />
                <AttachmentsSection projectId={projectId} taskId={taskId} />
              </div>
            </>
          ) : null}
        </SheetContent>
      </Sheet>

      {task ? (
        <TaskFormDialog
          open={editOpen}
          onOpenChange={setEditOpen}
          projectId={projectId}
          task={task}
        />
      ) : null}

      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Görev silinsin mi?"
        description="Görevle birlikte yorumları ve ekleri de kalıcı olarak silinir. Bu işlem geri alınamaz."
        confirmLabel="Sil"
        destructive
        pending={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
      />
    </>
  );
}

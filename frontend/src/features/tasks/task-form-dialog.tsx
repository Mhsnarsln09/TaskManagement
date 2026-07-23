"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CalendarIcon, Loader2, X } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { UserDisplay } from "@/components/shared/user-display";
import {
  STATUS_LABELS,
  PRIORITY_LABELS,
} from "@/components/shared/badges";
import { tasksApi } from "@/lib/api/endpoints";
import { useProjectMembers } from "@/features/projects/use-project-members";
import { ApiError, fieldErrors } from "@/lib/api/problem";
import {
  TASK_PRIORITIES,
  WORK_ITEM_STATUSES,
  type TaskResponse,
} from "@/lib/api/types";
import { formatDateOnly, parseDateOnly, toDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";

// Tasarım §05. Karar §6: create'te Durum alanı YOK (backend Todo atar);
// dueDate DateOnly ve bugünden eski olamaz; atanan proje üyeleri arasından seçilir.

const UNASSIGNED = "__unassigned__";

const schema = z.object({
  title: z.string().min(1, "Başlık zorunludur.").max(200, "En fazla 200 karakter."),
  description: z.string().max(4000, "En fazla 4000 karakter.").optional(),
  status: z.enum(WORK_ITEM_STATUSES),
  priority: z.enum(TASK_PRIORITIES),
  dueDate: z.string().nullable(),
  assigneeUserId: z.string(),
});

type FormValues = z.infer<typeof schema>;

interface TaskFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectId: string;
  /** Verilirse düzenleme modu. */
  task?: TaskResponse;
  onSaved?: (task: TaskResponse) => void;
}

function isPastDateOnly(value: string): boolean {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return parseDateOnly(value) < today;
}

export function TaskFormDialog({
  open,
  onOpenChange,
  projectId,
  task,
  onSaved,
}: TaskFormDialogProps) {
  const isEdit = task !== undefined;
  const queryClient = useQueryClient();
  const [error, setError] = useState<unknown>(null);
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  // Optimistic concurrency temeli (B10-06/F10-06): düzenlenen görevin versiyonu.
  // 409'da yeni sürüme geçilirken form alanları KORUNUR, yalnız bu değer tazelenir.
  const [version, setVersion] = useState<string | undefined>(task?.version);
  // 409 sonrası çekilen güncel sunucu sürümü (karşılaştırma için gösterilir).
  const [latestConflict, setLatestConflict] = useState<TaskResponse | null>(null);

  // Üye adları dizine yazılır; "Atanan" seçicisi GUID yerine ad gösterir.
  const membersQuery = useProjectMembers(projectId, open);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: defaultsFor(task),
  });

  useEffect(() => {
    if (open) {
      form.reset(defaultsFor(task));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, task?.id, task?.updatedAtUtc]);

  // Görev/oturum değiştiğinde version ve çakışma durumunu render sırasında tazele
  // (efekt içinde setState yerine React'in önerdiği "prop değişince state'i sıfırla").
  const taskKey = open ? `${task?.id ?? ""}:${task?.updatedAtUtc ?? ""}` : "";
  const [syncedTaskKey, setSyncedTaskKey] = useState(taskKey);
  if (taskKey !== syncedTaskKey) {
    setSyncedTaskKey(taskKey);
    setVersion(task?.version);
    setLatestConflict(null);
  }

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const common = {
        title: values.title.trim(),
        description: values.description?.trim() ? values.description.trim() : null,
        priority: values.priority,
        dueDate: values.dueDate,
        assigneeUserId:
          values.assigneeUserId === UNASSIGNED ? null : values.assigneeUserId,
      };
      return isEdit
        ? tasksApi.update(projectId, task.id, {
            ...common,
            status: values.status,
            version,
          })
        : tasksApi.create(projectId, common);
    },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: ["project", projectId, "tasks"] });
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "statistics"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "overdue-preview"],
      });
      queryClient.setQueryData(["project", projectId, "task", saved.id], saved);
      toast.success(isEdit ? "Değişiklikler kaydedildi." : "Görev oluşturuldu.", {
        description: isEdit ? undefined : "Listeye eklendi.",
      });
      onSaved?.(saved);
      onOpenChange(false);
    },
    onError: (cause) => {
      if (cause instanceof ApiError && cause.status === 400 && cause.problem.errors) {
        let mapped = false;
        for (const field of ["title", "description", "dueDate", "assigneeUserId"] as const) {
          const messages = fieldErrors(cause.problem, field);
          if (messages.length > 0) {
            form.setError(field, { type: "server", message: messages[0] });
            mapped = true;
          }
        }
        if (mapped) return;
      }
      setError(cause);
    },
  });

  // react-hook-form formState'i Proxy ile abonelik kurar: render sırasında
  // okunmazsa callback içindeki değer güncellenmez ve kirli form koruması çalışmaz.
  const isDirty = form.formState.isDirty;

  function requestClose(nextOpen: boolean) {
    if (nextOpen) return onOpenChange(true);
    if (mutation.isPending) return;
    setError(null);
    if (isDirty) {
      setConfirmDiscard(true);
      return;
    }
    onOpenChange(false);
  }

  const is409 = error instanceof ApiError && error.status === 409;

  return (
    <>
      <Dialog open={open} onOpenChange={requestClose}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{isEdit ? "Görevi düzenle" : "Yeni görev"}</DialogTitle>
            <DialogDescription>
              {isEdit
                ? "Görev alanlarını güncelleyin."
                : "Görev, Yapılacak durumunda oluşturulur."}
            </DialogDescription>
          </DialogHeader>
          <Form {...form}>
            <form
              onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
              className="space-y-4"
              noValidate
            >
              {error ? (
                <ProblemDetailsAlert
                  error={error}
                  title={
                    is409
                      ? "Bu görev siz düzenlerken başkası tarafından güncellendi."
                      : undefined
                  }
                  description={
                    is409 && latestConflict
                      ? `Güncel sürümdeki başlık: “${latestConflict.title}” · durum: ${STATUS_LABELS[latestConflict.status]}. Değişiklikleriniz korundu; “Kaydet” ile güncel sürümün üzerine yazın.`
                      : undefined
                  }
                  onRetry={
                    is409 && task
                      ? async () => {
                          // Girdiyi kaybetmeden güncel temele geç: en son sürümü çek,
                          // yalnız version'ı tazele, form alanlarını KORU (F10-06).
                          try {
                            const latest = await tasksApi.get(projectId, task.id);
                            setVersion(latest.version);
                            setLatestConflict(latest);
                            queryClient.setQueryData(
                              ["project", projectId, "task", task.id],
                              latest,
                            );
                            setError(null);
                            toast.info("Güncel sürüme geçildi.", {
                              description:
                                "Değişiklikleriniz korundu; tekrar kaydedebilirsiniz.",
                            });
                          } catch (cause) {
                            setError(cause);
                          }
                        }
                      : undefined
                  }
                  retryLabel="Güncel sürüme geç"
                />
              ) : null}

              <FormField
                control={form.control}
                name="title"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Başlık</FormLabel>
                    <FormControl>
                      <Input autoFocus disabled={mutation.isPending} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>
                      Açıklama{" "}
                      <span className="font-normal text-muted-foreground">
                        (isteğe bağlı)
                      </span>
                    </FormLabel>
                    <FormControl>
                      <Textarea rows={3} disabled={mutation.isPending} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                {isEdit ? (
                  <FormField
                    control={form.control}
                    name="status"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Durum</FormLabel>
                        <Select
                          value={field.value}
                          onValueChange={field.onChange}
                          disabled={mutation.isPending}
                        >
                          <FormControl>
                            <SelectTrigger className="w-full">
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {WORK_ITEM_STATUSES.map((status) => (
                              <SelectItem key={status} value={status}>
                                {STATUS_LABELS[status]}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                ) : null}

                <FormField
                  control={form.control}
                  name="priority"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Öncelik</FormLabel>
                      <Select
                        value={field.value}
                        onValueChange={field.onChange}
                        disabled={mutation.isPending}
                      >
                        <FormControl>
                          <SelectTrigger className="w-full">
                            <SelectValue />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {TASK_PRIORITIES.map((priority) => (
                            <SelectItem key={priority} value={priority}>
                              {PRIORITY_LABELS[priority]}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormField
                  control={form.control}
                  name="dueDate"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>
                        Son tarih{" "}
                        <span className="font-normal text-muted-foreground">(ops.)</span>
                      </FormLabel>
                      <div className="flex items-center gap-1">
                        <Popover>
                          <PopoverTrigger asChild>
                            <FormControl>
                              <Button
                                type="button"
                                variant="outline"
                                disabled={mutation.isPending}
                                className={cn(
                                  "flex-1 justify-start font-normal",
                                  !field.value && "text-muted-foreground",
                                )}
                              >
                                <CalendarIcon aria-hidden className="size-4" />
                                {field.value
                                  ? formatDateOnly(field.value)
                                  : "Tarih seç"}
                              </Button>
                            </FormControl>
                          </PopoverTrigger>
                          <PopoverContent className="w-auto p-0" align="start">
                            <Calendar
                              mode="single"
                              selected={
                                field.value ? parseDateOnly(field.value) : undefined
                              }
                              onSelect={(date) =>
                                field.onChange(date ? toDateOnly(date) : null)
                              }
                              disabled={(date) => {
                                const today = new Date();
                                today.setHours(0, 0, 0, 0);
                                return date < today;
                              }}
                              autoFocus
                            />
                          </PopoverContent>
                        </Popover>
                        {field.value ? (
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon-sm"
                            aria-label="Son tarihi temizle"
                            disabled={mutation.isPending}
                            onClick={() => field.onChange(null)}
                          >
                            <X aria-hidden className="size-3.5" />
                          </Button>
                        ) : null}
                      </div>
                      {field.value && isPastDateOnly(field.value) ? (
                        <FormDescription className="text-warning">
                          Geçmiş tarihli görev bu tarihle kaydedilemez; tarihi
                          güncelleyin veya temizleyin.
                        </FormDescription>
                      ) : null}
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="assigneeUserId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>
                        Atanan{" "}
                        <span className="font-normal text-muted-foreground">(ops.)</span>
                      </FormLabel>
                      <Select
                        value={field.value}
                        onValueChange={field.onChange}
                        disabled={mutation.isPending || membersQuery.isPending}
                      >
                        <FormControl>
                          <SelectTrigger className="w-full">
                            <SelectValue placeholder="Atanmamış" />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value={UNASSIGNED}>Atanmamış</SelectItem>
                          {(membersQuery.data ?? []).map((member) => (
                            <SelectItem key={member.userId} value={member.userId}>
                              <UserDisplay
                                userId={member.userId}
                                showYouBadge={false}
                              />
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormDescription>
                        Proje üyeleri arasından seçilir; userId arka planda gönderilir.
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  disabled={mutation.isPending}
                  onClick={() => requestClose(false)}
                >
                  Vazgeç
                </Button>
                <Button type="submit" disabled={mutation.isPending}>
                  {mutation.isPending ? (
                    <>
                      <Loader2 aria-hidden className="size-4 animate-spin" />
                      {isEdit ? "Kaydediliyor…" : "Oluşturuluyor…"}
                    </>
                  ) : isEdit ? (
                    "Kaydet"
                  ) : (
                    "Oluştur"
                  )}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmDiscard}
        onOpenChange={setConfirmDiscard}
        title="Kaydedilmemiş değişiklikler var"
        description="Formu kapatırsanız yaptığınız değişiklikler kaybolur."
        confirmLabel="Değişiklikleri at"
        cancelLabel="Düzenlemeye dön"
        destructive
        onConfirm={() => {
          setConfirmDiscard(false);
          onOpenChange(false);
        }}
      />
    </>
  );
}

function defaultsFor(task: TaskResponse | undefined): FormValues {
  return {
    title: task?.title ?? "",
    description: task?.description ?? "",
    status: task?.status ?? "Todo",
    priority: task?.priority ?? "Medium",
    dueDate: task?.dueDate ?? null,
    assigneeUserId: task?.assigneeUserId ?? UNASSIGNED,
  };
}

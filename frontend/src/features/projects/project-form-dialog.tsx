"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
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
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { projectsApi } from "@/lib/api/endpoints";
import { ApiError, fieldErrors } from "@/lib/api/problem";
import type { ProjectResponse } from "@/lib/api/types";
import { PROJECTS_QUERY_KEY } from "@/features/shell/sidebar-nav";

const schema = z.object({
  name: z
    .string()
    .min(1, "Proje adı zorunludur.")
    .max(160, "En fazla 160 karakter."),
  description: z.string().max(2000, "En fazla 2000 karakter.").optional(),
});

type FormValues = z.infer<typeof schema>;

interface ProjectFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Verilirse düzenleme modu (tasarım §13: dolu değerlerle aynı diyalog). */
  project?: ProjectResponse;
}

export function ProjectFormDialog({
  open,
  onOpenChange,
  project,
}: ProjectFormDialogProps) {
  const isEdit = project !== undefined;
  const queryClient = useQueryClient();
  const [error, setError] = useState<unknown>(null);
  const [confirmDiscard, setConfirmDiscard] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: project?.name ?? "", description: project?.description ?? "" },
  });

  useEffect(() => {
    if (open) {
      form.reset({
        name: project?.name ?? "",
        description: project?.description ?? "",
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, project?.id]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const body = {
        name: values.name.trim(),
        description: values.description?.trim() ? values.description.trim() : null,
      };
      return isEdit ? projectsApi.update(project.id, body) : projectsApi.create(body);
    },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: PROJECTS_QUERY_KEY });
      void queryClient.invalidateQueries({ queryKey: ["project", saved.id] });
      toast.success(isEdit ? "Değişiklikler kaydedildi." : "Proje oluşturuldu.");
      onOpenChange(false);
    },
    onError: (cause) => {
      if (cause instanceof ApiError && cause.status === 400 && cause.problem.errors) {
        const nameErrors = fieldErrors(cause.problem, "name");
        const descriptionErrors = fieldErrors(cause.problem, "description");
        if (nameErrors.length > 0) {
          form.setError("name", { type: "server", message: nameErrors[0] });
        }
        if (descriptionErrors.length > 0) {
          form.setError("description", { type: "server", message: descriptionErrors[0] });
        }
        if (nameErrors.length > 0 || descriptionErrors.length > 0) return;
      }
      setError(cause);
    },
  });

  // Kirli form koruması (tasarım §05 deseniyle aynı): ESC/dış tıklama/Vazgeç.
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

  return (
    <>
      <Dialog open={open} onOpenChange={requestClose}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{isEdit ? "Projeyi düzenle" : "Yeni proje"}</DialogTitle>
            <DialogDescription>
              {isEdit
                ? "Proje adını ve açıklamasını güncelleyin."
                : "Proje sahibi siz olursunuz."}
            </DialogDescription>
          </DialogHeader>
          <Form {...form}>
            <form
              onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
              className="space-y-4"
              noValidate
            >
              {error ? <ProblemDetailsAlert error={error} /> : null}
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Proje adı</FormLabel>
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
              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => requestClose(false)}
                  disabled={mutation.isPending}
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

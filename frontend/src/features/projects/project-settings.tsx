"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { Button } from "@/components/ui/button";
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
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/shared/page-header";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { FullPageState } from "@/components/shared/states";
import { projectsApi } from "@/lib/api/endpoints";
import { ApiError, fieldErrors, isApiError } from "@/lib/api/problem";
import { canManageProject } from "@/lib/permissions";
import { useAuth } from "@/lib/auth/auth-context";
import { PROJECTS_QUERY_KEY } from "@/features/shell/sidebar-nav";
import { DeleteProjectDialog } from "./delete-project-dialog";

// Tasarım §09: proje bilgileri formu + tehlike bölgesi. Yalnız sahip/Admin.

const schema = z.object({
  name: z.string().min(1, "Proje adı zorunludur.").max(160, "En fazla 160 karakter."),
  description: z.string().max(2000, "En fazla 2000 karakter.").optional(),
});

type FormValues = z.infer<typeof schema>;

export function ProjectSettings({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [error, setError] = useState<unknown>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const projectQuery = useQuery({
    queryKey: ["project", projectId],
    queryFn: ({ signal }) => projectsApi.get(projectId, signal),
  });

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", description: "" },
  });

  useEffect(() => {
    if (projectQuery.data) {
      form.reset({
        name: projectQuery.data.name,
        description: projectQuery.data.description ?? "",
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [projectQuery.data?.id, projectQuery.data?.updatedAtUtc]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      projectsApi.update(projectId, {
        name: values.name.trim(),
        description: values.description?.trim() ? values.description.trim() : null,
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(["project", projectId], saved);
      void queryClient.invalidateQueries({ queryKey: PROJECTS_QUERY_KEY });
      toast.success("Değişiklikler kaydedildi.");
      setError(null);
    },
    onError: (cause) => {
      if (cause instanceof ApiError && cause.status === 400 && cause.problem.errors) {
        const nameErrors = fieldErrors(cause.problem, "name");
        if (nameErrors.length > 0) {
          form.setError("name", { type: "server", message: nameErrors[0] });
          return;
        }
      }
      setError(cause);
    },
  });

  if (projectQuery.isPending) {
    return (
      <div className="mx-auto w-full max-w-2xl space-y-4" aria-busy>
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  if (projectQuery.error) {
    return (
      <FullPageState
        code={isApiError(projectQuery.error, 403) ? "403" : "404"}
        title="Bu sayfaya erişiminiz yok"
        description="Proje ayarlarını yalnızca proje sahibi veya yönetici görüntüleyebilir."
        actionLabel="Projelerime dön"
        actionHref="/projects"
      />
    );
  }

  const project = projectQuery.data;
  if (!project) return null;

  // Eylem bazlı 403: sahip olmayan üye ayar sayfasını hiç görmez (karar §8).
  if (!canManageProject(user, project)) {
    return (
      <FullPageState
        code="403"
        title="Erişim yetkiniz yok"
        description="Proje ayarlarını yalnızca proje sahibi veya yönetici düzenleyebilir."
        actionLabel="Projeye dön"
        actionHref={`/projects/${projectId}`}
      />
    );
  }

  return (
    <div className="mx-auto w-full max-w-2xl space-y-6">
      <PageHeader title="Proje ayarları" description={project.name} />

      <section className="rounded-lg border bg-card p-5">
        <h2 className="mb-4 text-sm font-semibold">Proje bilgileri</h2>
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
                    <Input disabled={mutation.isPending} {...field} />
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
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                disabled={mutation.isPending || !form.formState.isDirty}
                onClick={() =>
                  form.reset({
                    name: project.name,
                    description: project.description ?? "",
                  })
                }
              >
                Vazgeç
              </Button>
              <Button
                type="submit"
                disabled={mutation.isPending || !form.formState.isDirty}
              >
                {mutation.isPending ? (
                  <>
                    <Loader2 aria-hidden className="size-4 animate-spin" />
                    Kaydediliyor…
                  </>
                ) : (
                  "Kaydet"
                )}
              </Button>
            </div>
          </form>
        </Form>
      </section>

      <section className="rounded-lg border border-destructive/30 bg-destructive/5 p-5">
        <h2 className="text-sm font-semibold text-destructive">Tehlike bölgesi</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Projeyi kaldırmak; görevlerini, yorumlarını ve eklerini erişimden çıkarır
          ve listelerde görünmez yapar. Bu işlem arayüzden geri alınamaz.
        </p>
        <Button
          type="button"
          variant="destructive"
          className="mt-3"
          onClick={() => setDeleteOpen(true)}
        >
          Projeyi sil
        </Button>
      </section>

      <DeleteProjectDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        project={project}
      />
    </div>
  );
}

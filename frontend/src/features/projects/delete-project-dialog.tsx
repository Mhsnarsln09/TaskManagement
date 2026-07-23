"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { projectsApi } from "@/lib/api/endpoints";
import { problemMessage } from "@/lib/api/problem";
import type { ProjectResponse } from "@/lib/api/types";
import { PROJECTS_QUERY_KEY } from "@/features/shell/sidebar-nav";

// Tasarım §09: ad yazılarak onaylanan silme. Görev sayısı istatistikten gelir;
// yorum/ek sayısı endpoint'i yok → metin sayısızdır (DESIGN-DECISIONS.md §14).
export function DeleteProjectDialog({
  open,
  onOpenChange,
  project,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  project: ProjectResponse;
}) {
  const [nameInput, setNameInput] = useState("");
  const queryClient = useQueryClient();
  const router = useRouter();

  // Kapanışta temizlenir; diyalog her açılış öncesi kapalı olduğundan
  // açılışta alan her zaman boştur.
  function handleOpenChange(next: boolean) {
    if (!next) setNameInput("");
    onOpenChange(next);
  }

  const { data: statistics } = useQuery({
    queryKey: ["project", project.id, "statistics"],
    queryFn: ({ signal }) => projectsApi.statistics(project.id, signal),
    enabled: open,
  });

  const mutation = useMutation({
    mutationFn: () => projectsApi.remove(project.id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PROJECTS_QUERY_KEY });
      setNameInput("");
      onOpenChange(false);
      toast.success("Proje silindi.", {
        description: "Proje listesine dönüldü.",
      });
      router.push("/projects");
    },
    onError: (cause) => {
      toast.error("Proje silinemedi.", { description: problemMessage(cause) });
    },
  });

  return (
    <ConfirmDialog
      open={open}
      onOpenChange={handleOpenChange}
      title={`"${project.name}" silinsin mi?`}
      description={
        <>
          {statistics
            ? `${statistics.totalTasks} görev ile birlikte yorumlar ve ekler erişimden kaldırılacak. `
            : "Görevler, yorumlar ve ekler erişimden kaldırılacak. "}
          Proje ve içeriği listelerde artık görünmez;{" "}
          <strong>bu işlem arayüzden geri alınamaz</strong>.
        </>
      }
      confirmLabel="Projeyi kaldır"
      destructive
      pending={mutation.isPending}
      confirmDisabled={nameInput !== project.name}
      onConfirm={() => mutation.mutate()}
    >
      <div className="grid gap-2">
        <Label htmlFor="delete-project-name">
          Onaylamak için proje adını yazın
        </Label>
        <Input
          id="delete-project-name"
          value={nameInput}
          onChange={(event) => setNameInput(event.target.value)}
          placeholder={project.name}
          autoComplete="off"
          disabled={mutation.isPending}
        />
      </div>
    </ConfirmDialog>
  );
}

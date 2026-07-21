"use client";

import { useQuery } from "@tanstack/react-query";
import {
  FolderOpen,
  FolderPlus,
  MoreHorizontal,
  Pencil,
  Plus,
  Trash2,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PageHeader } from "@/components/shared/page-header";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { EmptyState, TableSkeleton } from "@/components/shared/states";
import { UserDisplay } from "@/components/shared/user-display";
import { formatDate } from "@/lib/dates";
import { projectsApi } from "@/lib/api/endpoints";
import { canManageProject } from "@/lib/permissions";
import { useAuth } from "@/lib/auth/auth-context";
import type { ProjectResponse } from "@/lib/api/types";
import { PROJECTS_QUERY_KEY } from "@/features/shell/sidebar-nav";
import { ProjectFormDialog } from "./project-form-dialog";
import { DeleteProjectDialog } from "./delete-project-dialog";

export function ProjectList() {
  const { user } = useAuth();
  const router = useRouter();
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<ProjectResponse | null>(null);
  const [deleting, setDeleting] = useState<ProjectResponse | null>(null);

  const { data, isPending, error, refetch } = useQuery({
    queryKey: PROJECTS_QUERY_KEY,
    queryFn: ({ signal }) => projectsApi.list(signal),
  });

  return (
    <div className="mx-auto w-full max-w-5xl space-y-5">
      <PageHeader
        title="Projeler"
        description={
          data
            ? `Sahibi olduğunuz veya üyesi olduğunuz projeler · ${data.length} proje`
            : "Sahibi olduğunuz veya üyesi olduğunuz projeler"
        }
        actions={
          <Button type="button" onClick={() => setCreateOpen(true)}>
            <Plus aria-hidden className="size-4" />
            Yeni proje
          </Button>
        }
      />

      {isPending ? (
        <div className="rounded-lg border p-4">
          <TableSkeleton rows={4} columns={3} />
        </div>
      ) : error ? (
        <ProblemDetailsAlert
          error={error}
          title="Projeler yüklenemedi."
          onRetry={() => void refetch()}
        />
      ) : data && data.length === 0 ? (
        <EmptyState
          icon={FolderPlus}
          title="Henüz projeniz yok"
          description="İlk projenizi oluşturarak başlayın."
          action={
            <Button type="button" onClick={() => setCreateOpen(true)}>
              <Plus aria-hidden className="size-4" />
              Yeni proje
            </Button>
          }
        />
      ) : data ? (
        <>
          {/* Masaüstü tablo */}
          <div className="hidden overflow-hidden rounded-lg border md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Proje</TableHead>
                  <TableHead className="w-44">Sahip</TableHead>
                  <TableHead className="w-32">Oluşturulma</TableHead>
                  <TableHead className="w-12">
                    <span className="sr-only">Eylemler</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((project) => (
                  <TableRow
                    key={project.id}
                    className="cursor-pointer"
                    onClick={() => router.push(`/projects/${project.id}`)}
                  >
                    <TableCell className="max-w-0">
                      <div className="truncate font-medium">
                        <Link
                          href={`/projects/${project.id}`}
                          className="hover:underline"
                          onClick={(event) => event.stopPropagation()}
                        >
                          {project.name}
                        </Link>
                      </div>
                      <div className="truncate text-xs text-muted-foreground">
                        {project.description?.trim() || "Açıklama yok"}
                      </div>
                    </TableCell>
                    <TableCell>
                      <UserDisplay userId={project.ownerUserId} />
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatDate(project.createdAtUtc)}
                    </TableCell>
                    <TableCell onClick={(event) => event.stopPropagation()}>
                      <ProjectRowMenu
                        project={project}
                        canManage={canManageProject(user, project)}
                        onEdit={() => setEditing(project)}
                        onDelete={() => setDeleting(project)}
                      />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Mobil kart satırları */}
          <ul className="space-y-2 md:hidden">
            {data.map((project) => (
              <li key={project.id} className="rounded-lg border bg-card">
                <div className="flex items-center">
                  <Link
                    href={`/projects/${project.id}`}
                    className="min-w-0 flex-1 px-3.5 py-3"
                  >
                    <span className="block truncate text-sm font-semibold">
                      {project.name}
                    </span>
                    <span className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground">
                      {formatDate(project.createdAtUtc)} ·{" "}
                      <UserDisplay userId={project.ownerUserId} />
                    </span>
                  </Link>
                  <div className="pr-2">
                    <ProjectRowMenu
                      project={project}
                      canManage={canManageProject(user, project)}
                      onEdit={() => setEditing(project)}
                      onDelete={() => setDeleting(project)}
                    />
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </>
      ) : null}

      <ProjectFormDialog open={createOpen} onOpenChange={setCreateOpen} />
      {editing ? (
        <ProjectFormDialog
          open
          onOpenChange={(open) => {
            if (!open) setEditing(null);
          }}
          project={editing}
        />
      ) : null}
      {deleting ? (
        <DeleteProjectDialog
          open
          onOpenChange={(open) => {
            if (!open) setDeleting(null);
          }}
          project={deleting}
        />
      ) : null}
    </div>
  );
}

function ProjectRowMenu({
  project,
  canManage,
  onEdit,
  onDelete,
}: {
  project: ProjectResponse;
  canManage: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const router = useRouter();
  // Tasarım §13: sahip değilse Düzenle/Sil render edilmez, yalnız "Aç" kalır.
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={`${project.name} eylemleri`}
        >
          <MoreHorizontal aria-hidden className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onSelect={() => router.push(`/projects/${project.id}`)}>
          <FolderOpen aria-hidden className="size-4" />
          Aç
        </DropdownMenuItem>
        {canManage ? (
          <>
            <DropdownMenuItem onSelect={onEdit}>
              <Pencil aria-hidden className="size-4" />
              Düzenle
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive" onSelect={onDelete}>
              <Trash2 aria-hidden className="size-4" />
              Sil
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

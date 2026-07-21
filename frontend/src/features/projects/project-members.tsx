"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Trash2, UserPlus } from "lucide-react";
import { useState } from "react";
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
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { PageHeader } from "@/components/shared/page-header";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { TableSkeleton } from "@/components/shared/states";
import { UserDisplay } from "@/components/shared/user-display";
import { UserPicker } from "@/components/shared/user-picker";
import { useProjectMembers } from "./use-project-members";
import { formatDate } from "@/lib/dates";
import { projectsApi } from "@/lib/api/endpoints";
import { problemMessage } from "@/lib/api/problem";
import { canManageProject } from "@/lib/permissions";
import { useAuth } from "@/lib/auth/auth-context";
import type { ProjectMemberResponse, UserSummaryResponse } from "@/lib/api/types";

// Tasarım §07 + DESIGN-DECISIONS.md §1: üye yanıtı artık güvenli kullanıcı
// özetini (ad, kullanıcı adı) taşır, GUID yalnız tooltip'te ikincil bilgidir.
// Ekleme, /api/users arama ucuyla isimle yapılır; API'ye userId gönderilir.

export function ProjectMembers({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [addOpen, setAddOpen] = useState(false);
  const [removing, setRemoving] = useState<ProjectMemberResponse | null>(null);

  const projectQuery = useQuery({
    queryKey: ["project", projectId],
    queryFn: ({ signal }) => projectsApi.get(projectId, signal),
  });

  const membersQuery = useProjectMembers(projectId);

  const removeMutation = useMutation({
    mutationFn: (userId: string) => projectsApi.removeMember(projectId, userId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "members"],
      });
      setRemoving(null);
      toast.success("Üye projeden kaldırıldı.");
    },
    onError: (cause) => {
      toast.error("Üye kaldırılamadı.", { description: problemMessage(cause) });
    },
  });

  const project = projectQuery.data;
  const isOwnerView = project ? canManageProject(user, project) : false;

  return (
    <div className="mx-auto w-full max-w-4xl space-y-5">
      <PageHeader
        title="Üyeler"
        description={
          project ? (
            <>
              {project.name}
              {membersQuery.data ? ` · ${membersQuery.data.length} üye` : null}
              {!isOwnerView ? (
                <span className="mt-0.5 block text-xs">
                  Üyeleri yalnızca proje sahibi yönetebilir.
                </span>
              ) : null}
            </>
          ) : undefined
        }
        actions={
          isOwnerView ? (
            <Button type="button" onClick={() => setAddOpen(true)}>
              <UserPlus aria-hidden className="size-4" />
              Üye ekle
            </Button>
          ) : undefined
        }
      />

      {membersQuery.isPending ? (
        <div className="rounded-lg border p-4">
          <TableSkeleton rows={4} columns={3} />
        </div>
      ) : membersQuery.error ? (
        <ProblemDetailsAlert
          error={membersQuery.error}
          title="Üyeler yüklenemedi."
          onRetry={() => void membersQuery.refetch()}
        />
      ) : membersQuery.data ? (
        <>
          {/* Masaüstü tablo */}
          <div className="hidden overflow-hidden rounded-lg border md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Üye</TableHead>
                  <TableHead className="w-44">Kullanıcı adı</TableHead>
                  <TableHead className="w-32">Katılım</TableHead>
                  {isOwnerView ? (
                    <TableHead className="w-12">
                      <span className="sr-only">Eylemler</span>
                    </TableHead>
                  ) : null}
                </TableRow>
              </TableHeader>
              <TableBody>
                {membersQuery.data.map((member) => (
                  <MemberRow
                    key={member.userId}
                    member={member}
                    ownerUserId={project?.ownerUserId}
                    isOwnerView={isOwnerView}
                    onRemove={() => setRemoving(member)}
                  />
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Mobil kart satırları (tasarım §13) */}
          <ul className="space-y-2 md:hidden">
            {membersQuery.data.map((member) => (
              <li
                key={member.userId}
                className="flex items-center gap-3 rounded-lg border bg-card px-3.5 py-3"
              >
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-1.5 text-sm font-semibold">
                    <UserDisplay userId={member.userId} />
                    {member.userId === project?.ownerUserId ? (
                      <Badge variant="outline" className="px-1.5 py-0 text-[10px]">
                        sahip
                      </Badge>
                    ) : null}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    Katılım: {formatDate(member.joinedAtUtc)}
                  </div>
                </div>
                {isOwnerView && member.userId !== project?.ownerUserId ? (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    aria-label="Üyeyi kaldır"
                    onClick={() => setRemoving(member)}
                  >
                    <Trash2 aria-hidden className="size-4 text-destructive" />
                  </Button>
                ) : null}
              </li>
            ))}
          </ul>
        </>
      ) : null}

      <AddMemberDialog
        open={addOpen}
        onOpenChange={setAddOpen}
        projectId={projectId}
        existingUserIds={(membersQuery.data ?? []).map((member) => member.userId)}
      />

      {removing ? (
        <RemoveMemberConfirm
          member={removing}
          pending={removeMutation.isPending}
          onCancel={() => setRemoving(null)}
          onConfirm={() => removeMutation.mutate(removing.userId)}
        />
      ) : null}
    </div>
  );
}

function MemberRow({
  member,
  ownerUserId,
  isOwnerView,
  onRemove,
}: {
  member: ProjectMemberResponse;
  ownerUserId: string | undefined;
  isOwnerView: boolean;
  onRemove: () => void;
}) {
  const isOwnerRow = member.userId === ownerUserId;
  return (
    <TableRow>
      <TableCell>
        <span className="flex items-center gap-1.5 font-medium">
          <UserDisplay userId={member.userId} />
          {isOwnerRow ? (
            <Badge variant="outline" className="px-1.5 py-0 text-[10px]">
              sahip
            </Badge>
          ) : null}
        </span>
      </TableCell>
      <TableCell className="text-muted-foreground">
        {member.user ? `@${member.user.userName}` : "—"}
      </TableCell>
      <TableCell className="text-muted-foreground">
        {formatDate(member.joinedAtUtc)}
      </TableCell>
      {isOwnerView ? (
        <TableCell>
          {/* Sahip kendini kaldıramaz (backend kuralı). */}
          {!isOwnerRow ? (
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={`Üyeyi kaldır`}
              onClick={onRemove}
            >
              <Trash2 aria-hidden className="size-4 text-destructive" />
            </Button>
          ) : null}
        </TableCell>
      ) : null}
    </TableRow>
  );
}

function AddMemberDialog({
  open,
  onOpenChange,
  projectId,
  existingUserIds,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectId: string;
  existingUserIds: readonly string[];
}) {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<UserSummaryResponse | null>(null);
  const [error, setError] = useState<unknown>(null);

  const mutation = useMutation({
    mutationFn: (userId: string) => projectsApi.addMember(projectId, { userId }),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["project", projectId, "members"],
      });
      toast.success("Üye eklendi.");
      setSelected(null);
      setError(null);
      onOpenChange(false);
    },
    onError: (cause) => setError(cause),
  });

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (mutation.isPending) return;
        if (!next) {
          setSelected(null);
          setError(null);
        }
        onOpenChange(next);
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Üye ekle</DialogTitle>
          <DialogDescription>
            Eklenecek kişiyi adıyla arayıp seçin.
          </DialogDescription>
        </DialogHeader>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            if (selected) mutation.mutate(selected.id);
          }}
          className="space-y-4"
          noValidate
        >
          {error ? <ProblemDetailsAlert error={error} title="Üye eklenemedi." /> : null}
          <div className="grid gap-2">
            <Label htmlFor="add-member-user">Kişi</Label>
            <UserPicker
              id="add-member-user"
              value={selected}
              onChange={setSelected}
              excludeUserIds={existingUserIds}
              disabled={mutation.isPending}
              aria-describedby="add-member-user-help"
              placeholder="Kişi arayın…"
            />
            <p id="add-member-user-help" className="text-sm text-muted-foreground">
              Zaten üye olan kişiler listede çıkmaz.
            </p>
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={mutation.isPending}
              onClick={() => onOpenChange(false)}
            >
              Vazgeç
            </Button>
            <Button type="submit" disabled={mutation.isPending || selected === null}>
              {mutation.isPending ? (
                <>
                  <Loader2 aria-hidden className="size-4 animate-spin" />
                  Ekleniyor…
                </>
              ) : (
                "Ekle"
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function RemoveMemberConfirm({
  member,
  pending,
  onCancel,
  onConfirm,
}: {
  member: ProjectMemberResponse;
  pending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <ConfirmDialog
      open
      onOpenChange={(open) => {
        if (!open) onCancel();
      }}
      title="Üye projeden kaldırılsın mı?"
      description={
        <>
          <UserDisplay userId={member.userId} showYouBadge={false} /> projeye ve
          görevlerine erişimini kaybeder. Kendisine atanmış görevler atanmamış
          duruma geçmez.
        </>
      }
      confirmLabel="Kaldır"
      destructive
      pending={pending}
      onConfirm={onConfirm}
    />
  );
}

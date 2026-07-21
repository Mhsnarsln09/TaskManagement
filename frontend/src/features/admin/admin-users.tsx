"use client";

import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Search, ShieldAlert, X } from "lucide-react";
import { notFound } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { RoleBadge } from "@/components/shared/badges";
import { PageHeader } from "@/components/shared/page-header";
import { PaginationBar } from "@/components/shared/pagination-bar";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { EmptyState, TableSkeleton } from "@/components/shared/states";
import { adminApi } from "@/lib/api/endpoints";
import {
  APPLICATION_ROLES,
  totalPagesOf,
  type AdminUserResponse,
} from "@/lib/api/types";
import { useAuth } from "@/lib/auth/auth-context";
import { userDirectory } from "@/lib/user-directory";

// Tasarım §11: yalnız SuperAdmin. Sayfalı arama (debounce 300 ms), rol diyaloğu
// checkbox + en az bir rol; 409 son SuperAdmin, 404 silinmiş kullanıcı.
// SuperAdmin olmayana rota 404 gösterir (kaynak sızdırmama, karar §14).

const PAGE_SIZE = 10;

const ROLE_DESCRIPTIONS: Record<(typeof APPLICATION_ROLES)[number], string> = {
  SuperAdmin: "sistem ve kullanıcı yönetimi",
  Admin: "tüm projelerde tam yetki",
  ProjectManager: "proje oluşturma/yönetme",
  Member: "üyesi olduğu projelerde çalışma",
};

export function AdminUsers() {
  const { hasRole } = useAuth();
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [managing, setManaging] = useState<AdminUserResponse | null>(null);

  // Arama debounce 300ms.
  useEffect(() => {
    const timer = setTimeout(() => {
      setSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const query = useQuery({
    queryKey: ["admin", "users", page, search],
    queryFn: ({ signal }) =>
      adminApi.users({ page, pageSize: PAGE_SIZE, search: search || undefined }, signal),
    placeholderData: keepPreviousData,
    enabled: hasRole("SuperAdmin"),
  });

  // Yönetim listesi kullanıcı dizinini besler (karar §1).
  useEffect(() => {
    if (query.data) {
      userDirectory.upsertMany(
        query.data.items.map((user) => ({
          id: user.id,
          userName: user.userName,
          displayName: user.displayName,
        })),
      );
    }
  }, [query.data]);

  if (!hasRole("SuperAdmin")) {
    // Rota koruması: diğer roller için 404 (sunucu 403 döner; UI sızdırmaz).
    notFound();
  }

  const data = query.data;

  return (
    <div className="mx-auto w-full max-w-5xl space-y-4">
      <PageHeader
        title="Kullanıcı yönetimi"
        description={
          data ? `Sistem rolleri · ${data.totalCount} kullanıcı` : "Sistem rolleri"
        }
      />

      <div className="relative max-w-sm">
        <Search
          aria-hidden
          className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
        />
        <Input
          type="search"
          placeholder="Kullanıcı adı, e-posta veya ad ara…"
          aria-label="Kullanıcı ara"
          className="pl-8"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
      </div>

      {query.isPending ? (
        <div className="rounded-lg border p-4">
          <TableSkeleton rows={8} columns={4} />
        </div>
      ) : query.error ? (
        <ProblemDetailsAlert
          error={query.error}
          title="Kullanıcılar yüklenemedi."
          onRetry={() => void query.refetch()}
        />
      ) : data ? (
        data.items.length === 0 ? (
          <EmptyState
            icon={Search}
            title={
              search
                ? `"${search}" ile eşleşen kullanıcı yok`
                : "Kullanıcı bulunamadı"
            }
            action={
              search ? (
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setSearchInput("")}
                >
                  <X aria-hidden className="size-3.5" />
                  Aramayı temizle
                </Button>
              ) : undefined
            }
          />
        ) : (
          <>
            {/* Masaüstü tablo */}
            <div className="hidden overflow-hidden rounded-lg border md:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Kullanıcı adı</TableHead>
                    <TableHead>E-posta</TableHead>
                    <TableHead>Görünen ad</TableHead>
                    <TableHead>Roller</TableHead>
                    <TableHead className="w-32">
                      <span className="sr-only">Eylemler</span>
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">{user.userName}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {user.email}
                      </TableCell>
                      <TableCell>
                        {user.displayName?.trim() ? (
                          user.displayName
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <span className="flex flex-wrap gap-1">
                          {user.roles.map((role) => (
                            <RoleBadge key={role} role={role} />
                          ))}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => setManaging(user)}
                        >
                          Rolleri yönet
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            {/* Mobil kart satırları (tasarım §13) */}
            <ul className="space-y-2 md:hidden">
              {data.items.map((user) => (
                <li key={user.id} className="rounded-lg border bg-card px-3.5 py-3">
                  <div className="flex items-center justify-between gap-2">
                    <span className="truncate text-sm font-semibold">
                      {user.userName}
                    </span>
                    <Button
                      type="button"
                      variant="outline"
                      size="xs"
                      onClick={() => setManaging(user)}
                    >
                      Roller
                    </Button>
                  </div>
                  <p className="mt-0.5 truncate text-xs text-muted-foreground">
                    {user.email}
                    {user.displayName?.trim() ? ` · ${user.displayName}` : null}
                  </p>
                  <p className="mt-1.5 flex flex-wrap gap-1">
                    {user.roles.map((role) => (
                      <RoleBadge key={role} role={role} />
                    ))}
                  </p>
                </li>
              ))}
            </ul>

            <PaginationBar
              page={data.page}
              totalPages={totalPagesOf(data)}
              totalCount={data.totalCount}
              pageSize={data.pageSize}
              itemCount={data.items.length}
              itemsLabel="kullanıcıdan"
              onPageChange={setPage}
            />
          </>
        )
      ) : null}

      {managing ? (
        <RoleDialog
          user={managing}
          onClose={() => setManaging(null)}
        />
      ) : null}
    </div>
  );
}

function RoleDialog({
  user,
  onClose,
}: {
  user: AdminUserResponse;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string[]>([...user.roles]);
  const [error, setError] = useState<unknown>(null);
  const noneSelected = selected.length === 0;

  const dirty = useMemo(() => {
    const current = new Set(user.roles);
    return (
      selected.length !== user.roles.length ||
      selected.some((role) => !current.has(role))
    );
  }, [selected, user.roles]);

  const mutation = useMutation({
    mutationFn: () => adminApi.replaceRoles(user.id, { roles: selected }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Roller güncellendi.", {
        description: "Kullanıcının oturumu sonlandırıldı; yeniden giriş yapması gerekir.",
      });
      onClose();
    },
    onError: (cause) => {
      setError(cause);
      // 404: kullanıcı silinmiş olabilir → liste yenilenir (karar §8).
      void queryClient.invalidateQueries({ queryKey: ["admin", "users"] });
    },
  });

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !mutation.isPending) onClose();
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Rolleri yönet</DialogTitle>
          <DialogDescription>
            {user.userName} · {user.email}
          </DialogDescription>
        </DialogHeader>

        <Alert className="border-warning/40 bg-warning/5 text-warning [&>svg]:text-warning">
          <ShieldAlert aria-hidden className="size-4" />
          <AlertTitle>Oturum sonlandırılır</AlertTitle>
          <AlertDescription className="text-warning/90">
            Kaydedildiğinde kullanıcının mevcut oturumu sonlandırılır; yeniden
            giriş yapması gerekir.
          </AlertDescription>
        </Alert>

        {error ? <ProblemDetailsAlert error={error} /> : null}

        <div className="space-y-2.5">
          {APPLICATION_ROLES.map((role) => {
            const id = `role-${role}`;
            return (
              <div key={role} className="flex items-start gap-2.5">
                <Checkbox
                  id={id}
                  checked={selected.includes(role)}
                  disabled={mutation.isPending}
                  onCheckedChange={(checked) => {
                    setSelected((current) =>
                      checked === true
                        ? [...current, role]
                        : current.filter((existing) => existing !== role),
                    );
                  }}
                />
                <Label htmlFor={id} className="flex-col items-start gap-0.5 font-normal">
                  <span className="font-medium">{role}</span>
                  <span className="text-xs text-muted-foreground">
                    {ROLE_DESCRIPTIONS[role]}
                  </span>
                </Label>
              </div>
            );
          })}
        </div>

        {noneSelected ? (
          <p role="alert" className="text-sm font-medium text-destructive">
            En az bir rol seçilmelidir.
          </p>
        ) : null}

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            disabled={mutation.isPending}
            onClick={onClose}
          >
            Vazgeç
          </Button>
          <Button
            type="button"
            disabled={mutation.isPending || noneSelected || !dirty}
            onClick={() => {
              setError(null);
              mutation.mutate();
            }}
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
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

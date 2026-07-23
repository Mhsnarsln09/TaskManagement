"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  ClipboardList,
  FilterX,
  Plus,
} from "lucide-react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { PageHeader } from "@/components/shared/page-header";
import { PaginationBar } from "@/components/shared/pagination-bar";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { EmptyState, TableSkeleton } from "@/components/shared/states";
import {
  OverdueBadge,
  PriorityBadge,
  StatusBadge,
  STATUS_LABELS,
  PRIORITY_LABELS,
} from "@/components/shared/badges";
import { UserDisplay } from "@/components/shared/user-display";
import { projectsApi, tasksApi } from "@/lib/api/endpoints";
import { useAuth } from "@/lib/auth/auth-context";
import { canManageTasks } from "@/lib/permissions";
import {
  TASK_PRIORITIES,
  TASK_SORT_FIELDS,
  WORK_ITEM_STATUSES,
  type SortDirection,
  type TaskPriority,
  type TaskSortField,
  type WorkItemStatus,
  totalPagesOf,
} from "@/lib/api/types";
import { formatDate, formatDateOnly } from "@/lib/dates";
import { cn } from "@/lib/utils";
import { TaskFormDialog } from "./task-form-dialog";
import { TaskDetailSheet } from "./task-detail-sheet";

// Tasarım §04: tek durum + tek öncelik filtresi, başlıktan sıralama, sayfalama.
// Metin araması yok (endpoint yok). Durum URL'de tutulur; detay ?task= ile açılır.

const ALL = "__all__";
const PAGE_SIZE = 10;

const SORT_LABELS: Record<TaskSortField, string> = {
  title: "Başlık",
  status: "Durum",
  priority: "Öncelik",
  dueDate: "Son Tarih",
  createdAtUtc: "Oluşturulma",
};

interface Filters {
  page: number;
  status?: WorkItemStatus;
  priority?: TaskPriority;
  sortBy: TaskSortField;
  sortDirection: SortDirection;
  task?: string;
}

function parseFilters(params: URLSearchParams): Filters {
  const status = params.get("status");
  const priority = params.get("priority");
  const sortBy = params.get("sortBy");
  const sortDirection = params.get("sortDirection");
  const page = Number(params.get("page"));
  return {
    page: Number.isInteger(page) && page > 0 ? page : 1,
    status: WORK_ITEM_STATUSES.includes(status as WorkItemStatus)
      ? (status as WorkItemStatus)
      : undefined,
    priority: TASK_PRIORITIES.includes(priority as TaskPriority)
      ? (priority as TaskPriority)
      : undefined,
    sortBy: TASK_SORT_FIELDS.includes(sortBy as TaskSortField)
      ? (sortBy as TaskSortField)
      : "createdAtUtc",
    sortDirection: sortDirection === "asc" ? "asc" : "desc",
    task: params.get("task") ?? undefined,
  };
}

export function TasksView({ projectId }: { projectId: string }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const filters = parseFilters(searchParams);
  const [createOpen, setCreateOpen] = useState(false);
  const { user } = useAuth();

  const setFilters = useCallback(
    (next: Partial<Filters>) => {
      const merged = { ...filters, ...next };
      const params = new URLSearchParams();
      if (merged.page > 1) params.set("page", String(merged.page));
      if (merged.status) params.set("status", merged.status);
      if (merged.priority) params.set("priority", merged.priority);
      if (merged.sortBy !== "createdAtUtc") params.set("sortBy", merged.sortBy);
      if (merged.sortDirection !== "desc") {
        params.set("sortDirection", merged.sortDirection);
      }
      if (merged.task) params.set("task", merged.task);
      const text = params.toString();
      router.replace(text ? `${pathname}?${text}` : pathname, { scroll: false });
    },
    [filters, pathname, router],
  );

  const projectQuery = useQuery({
    queryKey: ["project", projectId],
    queryFn: ({ signal }) => projectsApi.get(projectId, signal),
  });

  const tasksQuery = useQuery({
    queryKey: [
      "project",
      projectId,
      "tasks",
      filters.page,
      filters.status ?? null,
      filters.priority ?? null,
      filters.sortBy,
      filters.sortDirection,
    ],
    queryFn: ({ signal }) =>
      tasksApi.list(
        projectId,
        {
          page: filters.page,
          pageSize: PAGE_SIZE,
          status: filters.status,
          priority: filters.priority,
          sortBy: filters.sortBy,
          sortDirection: filters.sortDirection,
        },
        signal,
      ),
    placeholderData: keepPreviousData,
  });

  const hasFilters = filters.status !== undefined || filters.priority !== undefined;
  const data = tasksQuery.data;
  // Görev oluşturma yalnız sahip/Admin'e görünür (B10-02 / F10-03).
  const canCreate = projectQuery.data
    ? canManageTasks(user, projectQuery.data)
    : false;

  function toggleSort(field: TaskSortField) {
    if (filters.sortBy === field) {
      setFilters({
        sortDirection: filters.sortDirection === "asc" ? "desc" : "asc",
        page: 1,
      });
    } else {
      setFilters({ sortBy: field, sortDirection: "asc", page: 1 });
    }
  }

  return (
    <div className="mx-auto w-full max-w-6xl space-y-4">
      <PageHeader
        title="Görevler"
        description={projectQuery.data?.name}
        actions={
          canCreate ? (
            <Button type="button" onClick={() => setCreateOpen(true)}>
              <Plus aria-hidden className="size-4" />
              Yeni görev
            </Button>
          ) : null
        }
      />

      {/* Filtre çubuğu */}
      <div className="flex flex-wrap items-center gap-2">
        <Select
          value={filters.status ?? ALL}
          onValueChange={(value) =>
            setFilters({
              status: value === ALL ? undefined : (value as WorkItemStatus),
              page: 1,
            })
          }
        >
          <SelectTrigger className="w-40" aria-label="Durum filtresi">
            <SelectValue>
              {`Durum: ${filters.status ? STATUS_LABELS[filters.status] : "Tümü"}`}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>Tümü</SelectItem>
            {WORK_ITEM_STATUSES.map((status) => (
              <SelectItem key={status} value={status}>
                {STATUS_LABELS[status]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={filters.priority ?? ALL}
          onValueChange={(value) =>
            setFilters({
              priority: value === ALL ? undefined : (value as TaskPriority),
              page: 1,
            })
          }
        >
          <SelectTrigger className="w-40" aria-label="Öncelik filtresi">
            <SelectValue>
              {`Öncelik: ${filters.priority ? PRIORITY_LABELS[filters.priority] : "Tümü"}`}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>Tümü</SelectItem>
            {TASK_PRIORITIES.map((priority) => (
              <SelectItem key={priority} value={priority}>
                {PRIORITY_LABELS[priority]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {hasFilters ? (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() =>
              setFilters({ status: undefined, priority: undefined, page: 1 })
            }
          >
            <FilterX aria-hidden className="size-3.5" />
            Temizle
          </Button>
        ) : null}

        <span className="ml-auto hidden text-xs text-muted-foreground sm:block">
          {SORT_LABELS[filters.sortBy]} ·{" "}
          {filters.sortDirection === "asc" ? "Artan" : "Azalan"}
        </span>
      </div>

      {tasksQuery.isPending ? (
        <div className="rounded-lg border p-4">
          <TableSkeleton rows={10} columns={6} />
        </div>
      ) : tasksQuery.error ? (
        <ProblemDetailsAlert
          error={tasksQuery.error}
          title="Görevler yüklenemedi."
          onRetry={() => void tasksQuery.refetch()}
        />
      ) : data ? (
        data.items.length === 0 ? (
          hasFilters ? (
            <EmptyState
              icon={FilterX}
              title="Filtreyle eşleşen görev yok"
              description={[
                filters.status ? `Durum: ${STATUS_LABELS[filters.status]}` : null,
                filters.priority
                  ? `Öncelik: ${PRIORITY_LABELS[filters.priority]}`
                  : null,
              ]
                .filter(Boolean)
                .join(" · ")}
              action={
                <Button
                  type="button"
                  variant="outline"
                  onClick={() =>
                    setFilters({ status: undefined, priority: undefined, page: 1 })
                  }
                >
                  Filtreleri temizle
                </Button>
              }
            />
          ) : (
            <EmptyState
              icon={ClipboardList}
              title="Bu projede henüz görev yok"
              description={
                canCreate
                  ? "İlk görevi oluşturun."
                  : "Görev oluşturma yetkisi proje sahibinde veya Admin'dedir."
              }
              action={
                canCreate ? (
                  <Button type="button" onClick={() => setCreateOpen(true)}>
                    <Plus aria-hidden className="size-4" />
                    Yeni görev
                  </Button>
                ) : undefined
              }
            />
          )
        ) : (
          <>
            {/* Masaüstü tablo */}
            <div
              className={cn(
                "hidden overflow-hidden rounded-lg border md:block",
                tasksQuery.isPlaceholderData && "opacity-60",
              )}
            >
              <Table>
                <TableHeader>
                  <TableRow>
                    <SortableHead
                      field="title"
                      filters={filters}
                      onSort={toggleSort}
                    >
                      Başlık
                    </SortableHead>
                    <SortableHead
                      field="status"
                      filters={filters}
                      onSort={toggleSort}
                      className="w-36"
                    >
                      Durum
                    </SortableHead>
                    <SortableHead
                      field="priority"
                      filters={filters}
                      onSort={toggleSort}
                      className="w-28"
                    >
                      Öncelik
                    </SortableHead>
                    <SortableHead
                      field="dueDate"
                      filters={filters}
                      onSort={toggleSort}
                      className="w-36"
                    >
                      Son Tarih
                    </SortableHead>
                    <TableHead className="w-40">Atanan</TableHead>
                    <SortableHead
                      field="createdAtUtc"
                      filters={filters}
                      onSort={toggleSort}
                      className="w-32"
                    >
                      Oluşturulma
                    </SortableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((task) => (
                    <TableRow
                      key={task.id}
                      className="cursor-pointer"
                      onClick={() => setFilters({ task: task.id })}
                    >
                      <TableCell className="max-w-0">
                        <span className="block truncate font-medium">
                          {task.title}
                        </span>
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={task.status} />
                      </TableCell>
                      <TableCell>
                        <PriorityBadge priority={task.priority} />
                      </TableCell>
                      <TableCell>
                        {task.dueDate ? (
                          <span
                            className={cn(
                              "flex items-center gap-1.5",
                              task.isOverdue && "text-destructive",
                            )}
                          >
                            {formatDateOnly(task.dueDate)}
                            {task.isOverdue ? (
                              <span className="text-xs font-medium">· Gecikti</span>
                            ) : null}
                          </span>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell>
                        <UserDisplay userId={task.assigneeUserId} />
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {formatDate(task.createdAtUtc)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            {/* Mobil yapılandırılmış satırlar */}
            <ul className="space-y-2 md:hidden">
              {data.items.map((task) => (
                <li key={task.id}>
                  <button
                    type="button"
                    onClick={() => setFilters({ task: task.id })}
                    className="w-full rounded-lg border bg-card px-3.5 py-3 text-left"
                  >
                    <span className="block truncate text-sm font-semibold">
                      {task.title}
                    </span>
                    <span className="mt-1.5 flex flex-wrap items-center gap-1.5">
                      <StatusBadge status={task.status} />
                      <PriorityBadge priority={task.priority} />
                      {task.isOverdue ? <OverdueBadge /> : null}
                    </span>
                    <span className="mt-1.5 flex flex-wrap items-center gap-x-3 text-xs text-muted-foreground">
                      {task.dueDate ? (
                        <span className={cn(task.isOverdue && "text-destructive")}>
                          Son: {formatDateOnly(task.dueDate)}
                        </span>
                      ) : null}
                      <UserDisplay userId={task.assigneeUserId} />
                    </span>
                  </button>
                </li>
              ))}
            </ul>

            <PaginationBar
              page={data.page}
              totalPages={totalPagesOf(data)}
              totalCount={data.totalCount}
              pageSize={data.pageSize}
              itemCount={data.items.length}
              itemsLabel="görevden"
              onPageChange={(page) => setFilters({ page })}
            />
          </>
        )
      ) : null}

      <TaskFormDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        projectId={projectId}
      />

      {filters.task ? (
        <TaskDetailSheet
          projectId={projectId}
          taskId={filters.task}
          onClose={() => setFilters({ task: undefined })}
        />
      ) : null}
    </div>
  );
}

function SortableHead({
  field,
  filters,
  onSort,
  className,
  children,
}: {
  field: TaskSortField;
  filters: { sortBy: TaskSortField; sortDirection: SortDirection };
  onSort: (field: TaskSortField) => void;
  className?: string;
  children: React.ReactNode;
}) {
  const active = filters.sortBy === field;
  const Icon = active
    ? filters.sortDirection === "asc"
      ? ArrowUp
      : ArrowDown
    : ArrowUpDown;
  return (
    <TableHead
      className={className}
      aria-sort={
        active
          ? filters.sortDirection === "asc"
            ? "ascending"
            : "descending"
          : undefined
      }
    >
      <button
        type="button"
        onClick={() => onSort(field)}
        className={cn(
          "flex items-center gap-1 font-medium hover:text-foreground",
          active ? "text-foreground" : "text-muted-foreground",
        )}
      >
        {children}
        <Icon aria-hidden className="size-3" />
      </button>
    </TableHead>
  );
}

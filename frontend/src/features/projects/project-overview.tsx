"use client";

import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Settings } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/shared/page-header";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { FullPageState } from "@/components/shared/states";
import { PriorityBadge } from "@/components/shared/badges";
import { UserDisplay } from "@/components/shared/user-display";
import { formatDate, formatDateOnly } from "@/lib/dates";
import { projectsApi, tasksApi } from "@/lib/api/endpoints";
import { isApiError } from "@/lib/api/problem";
import { canManageProject } from "@/lib/permissions";
import { useAuth } from "@/lib/auth/auth-context";

// Tasarım §03: sayfa başlığı proje adının kendisi; altı metrik + tamamlanma;
// aktivite akışı YOK (endpoint yok). Geciken listesi görev listesinden türetilir.

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-lg border bg-card px-3.5 py-3">
      <div className="text-xs font-medium text-muted-foreground">{label}</div>
      <div className="mt-1 text-xl font-bold tabular-nums">{value}</div>
    </div>
  );
}

export function ProjectOverview({ projectId }: { projectId: string }) {
  const { user } = useAuth();

  const projectQuery = useQuery({
    queryKey: ["project", projectId],
    queryFn: ({ signal }) => projectsApi.get(projectId, signal),
  });

  const statisticsQuery = useQuery({
    queryKey: ["project", projectId, "statistics"],
    queryFn: ({ signal }) => projectsApi.statistics(projectId, signal),
    enabled: projectQuery.isSuccess,
  });

  const overdueQuery = useQuery({
    queryKey: ["project", projectId, "overdue-preview"],
    queryFn: ({ signal }) =>
      tasksApi.list(
        projectId,
        { page: 1, pageSize: 20, sortBy: "dueDate", sortDirection: "asc" },
        signal,
      ),
    enabled: projectQuery.isSuccess,
  });

  if (projectQuery.isPending) {
    return (
      <div className="mx-auto w-full max-w-5xl space-y-5" aria-busy>
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24" />
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-20" />
          ))}
        </div>
      </div>
    );
  }

  if (projectQuery.error) {
    // Üye olmayana backend 404 döner (kaynak sızdırmama); 403 üye-yetkisizdir.
    if (isApiError(projectQuery.error, 404) || isApiError(projectQuery.error, 403)) {
      return (
        <FullPageState
          code={isApiError(projectQuery.error, 403) ? "403" : "404"}
          title="Bu projeye erişiminiz yok"
          description="Bağlantı size ait olmayan bir projeye işaret ediyor olabilir. Proje sahibinden sizi eklemesini isteyin."
          actionLabel="Projelerime dön"
          actionHref="/projects"
        />
      );
    }
    return (
      <div className="mx-auto w-full max-w-5xl">
        <ProblemDetailsAlert
          error={projectQuery.error}
          title="Proje yüklenemedi."
          onRetry={() => void projectQuery.refetch()}
        />
      </div>
    );
  }

  const project = projectQuery.data;
  if (!project) return null;
  const statistics = statisticsQuery.data;
  const overdueTasks = overdueQuery.data?.items.filter((task) => task.isOverdue) ?? [];

  return (
    <div className="mx-auto w-full max-w-5xl space-y-5">
      <PageHeader
        title={project.name}
        description={
          <div className="space-y-1">
            {project.description?.trim() ? <p>{project.description}</p> : null}
            <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
              <span className="inline-flex items-center gap-1">
                Sahip: <UserDisplay userId={project.ownerUserId} />
              </span>
              <span>Oluşturulma: {formatDate(project.createdAtUtc)}</span>
              {project.updatedAtUtc ? (
                <span>Güncelleme: {formatDate(project.updatedAtUtc)}</span>
              ) : null}
            </p>
          </div>
        }
        actions={
          canManageProject(user, project) ? (
            <Button asChild variant="outline">
              <Link href={`/projects/${projectId}/settings`}>
                <Settings aria-hidden className="size-4" />
                Proje ayarları
              </Link>
            </Button>
          ) : undefined
        }
      />

      {statisticsQuery.error ? (
        <ProblemDetailsAlert
          error={statisticsQuery.error}
          title="İstatistikler yüklenemedi."
          onRetry={() => void statisticsQuery.refetch()}
        />
      ) : (
        <>
          <div className="rounded-lg border bg-card px-4 py-3.5">
            <div className="flex items-center justify-between text-sm">
              <span className="font-medium">Tamamlanma</span>
              <span className="font-bold tabular-nums">
                {statistics ? `%${Math.round(statistics.completionPercentage)}` : "—"}
              </span>
            </div>
            <Progress
              value={statistics ? Number(statistics.completionPercentage) : 0}
              className="mt-2"
              aria-label="Tamamlanma yüzdesi"
            />
          </div>

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
            <StatCard label="Toplam" value={statistics?.totalTasks ?? 0} />
            <StatCard label="Yapılacak" value={statistics?.todoTasks ?? 0} />
            <StatCard label="Devam Eden" value={statistics?.inProgressTasks ?? 0} />
            <StatCard label="Tamamlanan" value={statistics?.completedTasks ?? 0} />
            <StatCard label="İptal" value={statistics?.cancelledTasks ?? 0} />
            <StatCard label="Geciken" value={statistics?.overdueTasks ?? 0} />
          </div>
        </>
      )}

      <section className="rounded-lg border bg-card">
        <div className="flex items-center justify-between border-b px-4 py-3">
          <h2 className="text-sm font-semibold">
            Geciken görevler
            {statistics ? (
              <span className="ml-1.5 rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-bold text-destructive">
                {statistics.overdueTasks}
              </span>
            ) : null}
          </h2>
          <Button asChild variant="ghost" size="sm">
            <Link href={`/projects/${projectId}/tasks?sortBy=dueDate&sortDirection=asc`}>
              Görevlerde filtrele
              <ArrowRight aria-hidden className="size-3.5" />
            </Link>
          </Button>
        </div>
        {overdueQuery.isPending ? (
          <div className="space-y-2 p-4" aria-hidden>
            <Skeleton className="h-9" />
            <Skeleton className="h-9" />
          </div>
        ) : overdueTasks.length === 0 ? (
          <p className="px-4 py-6 text-sm text-muted-foreground">
            Geciken görev yok.
          </p>
        ) : (
          <ul className="divide-y">
            {overdueTasks.slice(0, 5).map((task) => (
              <li key={task.id}>
                <Link
                  href={`/projects/${projectId}/tasks?task=${task.id}`}
                  className="flex flex-wrap items-center gap-x-3 gap-y-1 px-4 py-2.5 hover:bg-muted/50"
                >
                  <span className="min-w-0 flex-1 truncate text-sm font-medium">
                    {task.title}
                  </span>
                  <PriorityBadge priority={task.priority} />
                  <span className="text-xs text-destructive">
                    Son: {task.dueDate ? formatDateOnly(task.dueDate) : "—"}
                  </span>
                  <UserDisplay userId={task.assigneeUserId} className="text-xs" />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

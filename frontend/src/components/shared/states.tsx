import type { LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed px-6 py-12 text-center",
        className,
      )}
    >
      {Icon ? (
        <Icon aria-hidden className="size-8 text-muted-foreground/60" />
      ) : null}
      <p className="text-sm font-semibold">{title}</p>
      {description ? (
        <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
      ) : null}
      {action ? <div className="mt-2">{action}</div> : null}
    </div>
  );
}

interface FullPageStateProps {
  code?: string;
  title: string;
  description?: string;
  actionLabel?: string;
  onAction?: () => void;
  actionHref?: string;
}

export function FullPageState({
  code,
  title,
  description,
  actionLabel,
  onAction,
  actionHref,
}: FullPageStateProps) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-3 px-6 py-24 text-center">
      {code ? (
        <span className="font-mono text-xs font-semibold tracking-widest text-muted-foreground">
          {code}
        </span>
      ) : null}
      <h1 className="text-lg font-bold">{title}</h1>
      {description ? (
        <p className="max-w-md text-sm text-muted-foreground">{description}</p>
      ) : null}
      {actionLabel ? (
        actionHref ? (
          <Button asChild className="mt-2">
            <a href={actionHref}>{actionLabel}</a>
          </Button>
        ) : (
          <Button type="button" className="mt-2" onClick={onAction}>
            {actionLabel}
          </Button>
        )
      ) : null}
    </div>
  );
}

/** Gerçek yerleşimle aynı ızgarayı koruyan tablo iskeleti (tasarım §02/§04). */
export function TableSkeleton({
  rows = 6,
  columns = 4,
  className,
}: {
  rows?: number;
  columns?: number;
  className?: string;
}) {
  return (
    <div aria-hidden className={cn("space-y-2", className)}>
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div
          key={rowIndex}
          className="grid items-center gap-4"
          style={{ gridTemplateColumns: `2fr repeat(${columns - 1}, 1fr)` }}
        >
          {Array.from({ length: columns }).map((_, columnIndex) => (
            <Skeleton
              key={columnIndex}
              className={cn("h-4", columnIndex === 0 ? "w-3/4" : "w-1/2")}
            />
          ))}
        </div>
      ))}
    </div>
  );
}

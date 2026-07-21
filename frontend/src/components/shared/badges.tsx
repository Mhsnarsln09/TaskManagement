import {
  AlertOctagon,
  ArrowDown,
  ArrowRight,
  ArrowUp,
  Ban,
  CheckCircle2,
  Circle,
  Shield,
  Timer,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import type {
  ApplicationRole,
  TaskPriority,
  WorkItemStatus,
} from "@/lib/api/types";
import { cn } from "@/lib/utils";

// Renk asla tek başına anlam taşımaz: her rozet ikon + metin içerir (PRD §5).

export const STATUS_LABELS: Record<WorkItemStatus, string> = {
  Todo: "Yapılacak",
  InProgress: "Devam Ediyor",
  Completed: "Tamamlandı",
  Cancelled: "İptal",
};

const STATUS_STYLES: Record<WorkItemStatus, string> = {
  Todo: "bg-secondary text-secondary-foreground border-transparent",
  InProgress: "bg-info/10 text-info border-info/30",
  Completed: "bg-success/10 text-success border-success/30",
  Cancelled: "bg-muted text-muted-foreground border-transparent",
};

const STATUS_ICONS: Record<WorkItemStatus, typeof Circle> = {
  Todo: Circle,
  InProgress: Timer,
  Completed: CheckCircle2,
  Cancelled: Ban,
};

export function StatusBadge({
  status,
  className,
}: {
  status: WorkItemStatus;
  className?: string;
}) {
  const Icon = STATUS_ICONS[status];
  return (
    <Badge variant="outline" className={cn(STATUS_STYLES[status], className)}>
      <Icon aria-hidden className="size-3" />
      {STATUS_LABELS[status]}
    </Badge>
  );
}

export const PRIORITY_LABELS: Record<TaskPriority, string> = {
  Low: "Düşük",
  Medium: "Orta",
  High: "Yüksek",
  Critical: "Kritik",
};

const PRIORITY_STYLES: Record<TaskPriority, string> = {
  Low: "bg-muted text-muted-foreground border-transparent",
  Medium: "bg-secondary text-secondary-foreground border-transparent",
  High: "bg-warning/10 text-warning border-warning/30",
  Critical: "bg-destructive/10 text-destructive border-destructive/30",
};

const PRIORITY_ICONS: Record<TaskPriority, typeof ArrowDown> = {
  Low: ArrowDown,
  Medium: ArrowRight,
  High: ArrowUp,
  Critical: AlertOctagon,
};

export function PriorityBadge({
  priority,
  className,
}: {
  priority: TaskPriority;
  className?: string;
}) {
  const Icon = PRIORITY_ICONS[priority];
  return (
    <Badge
      variant="outline"
      className={cn(PRIORITY_STYLES[priority], className)}
    >
      <Icon aria-hidden className="size-3" />
      {PRIORITY_LABELS[priority]}
    </Badge>
  );
}

export function OverdueBadge({ className }: { className?: string }) {
  return (
    <Badge
      variant="outline"
      className={cn(
        "border-destructive/30 bg-destructive/10 text-destructive",
        className,
      )}
    >
      <Timer aria-hidden className="size-3" />
      Gecikti
    </Badge>
  );
}

export function RoleBadge({
  role,
  className,
}: {
  role: ApplicationRole | string;
  className?: string;
}) {
  const emphasized = role === "SuperAdmin" || role === "Admin";
  return (
    <Badge
      variant="outline"
      className={cn(
        emphasized
          ? "border-primary/30 bg-accent text-accent-foreground"
          : "bg-secondary text-secondary-foreground border-transparent",
        className,
      )}
    >
      {emphasized ? <Shield aria-hidden className="size-3" /> : null}
      {role}
    </Badge>
  );
}

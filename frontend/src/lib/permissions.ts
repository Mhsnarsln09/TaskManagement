import type { ProjectResponse, UserResponse } from "@/lib/api/types";

// Yetki matrisi DESIGN-DECISIONS.md §7'nin aynasıdır; nihai karar backend'dedir.

export function isProjectOwner(
  user: UserResponse | null,
  project: Pick<ProjectResponse, "ownerUserId">,
): boolean {
  return user !== null && user.id === project.ownerUserId;
}

/** Proje düzenleme/silme ve üye yönetimi: sahip veya Admin. */
export function canManageProject(
  user: UserResponse | null,
  project: Pick<ProjectResponse, "ownerUserId">,
): boolean {
  if (!user) return false;
  return user.roles.includes("Admin") || user.id === project.ownerUserId;
}

/** Görev silme: Admin veya (sahip VE ProjectManager rolü). */
export function canDeleteTasks(
  user: UserResponse | null,
  project: Pick<ProjectResponse, "ownerUserId">,
): boolean {
  if (!user) return false;
  if (user.roles.includes("Admin")) return true;
  return user.id === project.ownerUserId && user.roles.includes("ProjectManager");
}

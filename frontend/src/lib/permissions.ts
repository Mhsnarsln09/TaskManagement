import type {
  ProjectResponse,
  TaskResponse,
  UserResponse,
} from "@/lib/api/types";

// Yetki matrisi backend B10-02'nin (ProjectAuthorizationService + TaskService)
// aynasıdır; nihai karar her zaman backend'dedir. UI yalnızca görünürlüğü buradan
// türetir, kritik kontrolü değil.

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

/**
 * Tam görev yönetimi (oluşturma, tüm alanları düzenleme, yeniden atama, silme):
 * proje sahibi veya Admin (B10-02). Sistem `ProjectManager` rolü tek başına yetki
 * vermez.
 */
export function canManageTasks(
  user: UserResponse | null,
  project: Pick<ProjectResponse, "ownerUserId">,
): boolean {
  return canManageProject(user, project);
}

export function isTaskAssignee(
  user: UserResponse | null,
  task: Pick<TaskResponse, "assigneeUserId">,
): boolean {
  return user !== null && task.assigneeUserId !== null && task.assigneeUserId === user.id;
}

export interface TaskPermissions {
  /** Sahip/Admin: tüm alanları düzenler, yeniden atar, siler. */
  canManage: boolean;
  /** Sahip/Admin tüm alanları; başka kimse edit formunu göremez. */
  canEditAllFields: boolean;
  /** Sahip/Admin veya atanmış üye: durum değiştirebilir. */
  canChangeStatus: boolean;
  canDelete: boolean;
  /** Yazma yetkisi olmayan üye: yalnız okuma (+ yorum). */
  readOnly: boolean;
}

export function taskPermissions(
  user: UserResponse | null,
  project: Pick<ProjectResponse, "ownerUserId">,
  task: Pick<TaskResponse, "assigneeUserId">,
): TaskPermissions {
  const manage = canManageTasks(user, project);
  const assignee = !manage && isTaskAssignee(user, task);
  return {
    canManage: manage,
    canEditAllFields: manage,
    canChangeStatus: manage || assignee,
    canDelete: manage,
    readOnly: !manage && !assignee,
  };
}

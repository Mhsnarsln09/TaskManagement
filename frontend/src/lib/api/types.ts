import type { components } from "./schema";

type Schemas = components["schemas"];

export type AuthResponse = Schemas["AuthResponse"];
export type UserResponse = Schemas["UserResponse"];
export type UserSummaryResponse = Schemas["UserSummaryResponse"];
export type RegisterRequest = Schemas["RegisterRequest"];
export type LoginRequest = Schemas["LoginRequest"];
export type RefreshTokenRequest = Schemas["RefreshTokenRequest"];

export type ProjectResponse = Schemas["ProjectResponse"];
export type CreateProjectRequest = Schemas["CreateProjectRequest"];
export type UpdateProjectRequest = Schemas["UpdateProjectRequest"];
export type ProjectMemberResponse = Schemas["ProjectMemberResponse"];
export type AddProjectMemberRequest = Schemas["AddProjectMemberRequest"];
export type ProjectStatisticsResponse = Schemas["ProjectStatisticsResponse"];

export type TaskResponse = Schemas["TaskResponse"];
export type CreateTaskRequest = Schemas["CreateTaskRequest"];
export type UpdateTaskRequest = Schemas["UpdateTaskRequest"];
export type WorkItemStatus = Schemas["WorkItemStatus"];
export type TaskPriority = Schemas["TaskPriority"];
export type PagedTasks = Schemas["PagedResponseOfTaskResponse"];

export type CommentResponse = Schemas["CommentResponse"];
export type CreateCommentRequest = Schemas["CreateCommentRequest"];
export type PagedComments = Schemas["PagedResponseOfCommentResponse"];

// Ek listesi endpoint'i sayfasızdır; düz dizi döner (DESIGN-DECISIONS.md §10).
export type AttachmentResponse = Schemas["AttachmentResponse"];

export type NotificationResponse = Schemas["NotificationResponse"];
export type PagedNotifications = Schemas["PagedResponseOfNotificationResponse"];

export type AdminUserResponse = Schemas["AdminUserResponse"];
export type PagedAdminUsers = Schemas["PagedResponseOfAdminUserResponse"];
export type ReplaceUserRolesRequest = Schemas["ReplaceUserRolesRequest"];

/** totalPages şemada salt-okunur/opsiyonel; istemci güvenli biçimde türetir. */
export function totalPagesOf(paged: {
  totalCount: number;
  pageSize: number;
  totalPages?: number;
}): number {
  return paged.totalPages ?? Math.max(1, Math.ceil(paged.totalCount / paged.pageSize));
}

export const WORK_ITEM_STATUSES = [
  "Todo",
  "InProgress",
  "Completed",
  "Cancelled",
] as const satisfies readonly WorkItemStatus[];

export const TASK_PRIORITIES = [
  "Low",
  "Medium",
  "High",
  "Critical",
] as const satisfies readonly TaskPriority[];

export const APPLICATION_ROLES = [
  "SuperAdmin",
  "Admin",
  "ProjectManager",
  "Member",
] as const;

export type ApplicationRole = (typeof APPLICATION_ROLES)[number];

export const TASK_SORT_FIELDS = [
  "title",
  "status",
  "priority",
  "dueDate",
  "createdAtUtc",
] as const;

export type TaskSortField = (typeof TASK_SORT_FIELDS)[number];
export type SortDirection = "asc" | "desc";

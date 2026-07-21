import { apiFetch, apiFetchBlob } from "./client";
import type {
  AddProjectMemberRequest,
  AdminUserResponse,
  AttachmentResponse,
  AuthResponse,
  CommentResponse,
  CreateCommentRequest,
  CreateProjectRequest,
  CreateTaskRequest,
  LoginRequest,
  NotificationResponse,
  PagedAdminUsers,
  PagedComments,
  PagedNotifications,
  PagedTasks,
  ProjectMemberResponse,
  ProjectResponse,
  ProjectStatisticsResponse,
  RegisterRequest,
  ReplaceUserRolesRequest,
  SortDirection,
  TaskPriority,
  TaskResponse,
  TaskSortField,
  UpdateProjectRequest,
  UpdateTaskRequest,
  UserSummaryResponse,
  WorkItemStatus,
} from "./types";

function query(params: Record<string, string | number | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") search.set(key, String(value));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
}

export const authApi = {
  register: (body: RegisterRequest) =>
    apiFetch<AuthResponse>("/api/auth/register", {
      method: "POST",
      body,
      anonymous: true,
    }),
  login: (body: LoginRequest) =>
    apiFetch<AuthResponse>("/api/auth/login", {
      method: "POST",
      body,
      anonymous: true,
    }),
};

export const usersApi = {
  /** İsimle kişi arama; yalnız güvenli özet döner (e-posta ve rol yok). */
  search: (search: string, signal?: AbortSignal) =>
    apiFetch<UserSummaryResponse[]>(`/api/users${query({ search, limit: 10 })}`, {
      signal,
    }),
};

export const projectsApi = {
  list: (signal?: AbortSignal) =>
    apiFetch<ProjectResponse[]>("/api/projects", { signal }),
  get: (id: string, signal?: AbortSignal) =>
    apiFetch<ProjectResponse>(`/api/projects/${id}`, { signal }),
  create: (body: CreateProjectRequest) =>
    apiFetch<ProjectResponse>("/api/projects", { method: "POST", body }),
  update: (id: string, body: UpdateProjectRequest) =>
    apiFetch<ProjectResponse>(`/api/projects/${id}`, { method: "PUT", body }),
  remove: (id: string) =>
    apiFetch<void>(`/api/projects/${id}`, { method: "DELETE" }),
  members: (id: string, signal?: AbortSignal) =>
    apiFetch<ProjectMemberResponse[]>(`/api/projects/${id}/members`, { signal }),
  addMember: (id: string, body: AddProjectMemberRequest) =>
    apiFetch<ProjectMemberResponse>(`/api/projects/${id}/members`, {
      method: "POST",
      body,
    }),
  removeMember: (id: string, userId: string) =>
    apiFetch<void>(`/api/projects/${id}/members/${userId}`, {
      method: "DELETE",
    }),
  statistics: (id: string, signal?: AbortSignal) =>
    apiFetch<ProjectStatisticsResponse>(`/api/projects/${id}/statistics`, {
      signal,
    }),
};

export interface TaskListParams {
  page?: number;
  pageSize?: number;
  status?: WorkItemStatus;
  priority?: TaskPriority;
  sortBy?: TaskSortField;
  sortDirection?: SortDirection;
}

export const tasksApi = {
  list: (projectId: string, params: TaskListParams, signal?: AbortSignal) =>
    apiFetch<PagedTasks>(
      `/api/projects/${projectId}/tasks${query({ ...params })}`,
      { signal },
    ),
  get: (projectId: string, taskId: string, signal?: AbortSignal) =>
    apiFetch<TaskResponse>(`/api/projects/${projectId}/tasks/${taskId}`, {
      signal,
    }),
  create: (projectId: string, body: CreateTaskRequest) =>
    apiFetch<TaskResponse>(`/api/projects/${projectId}/tasks`, {
      method: "POST",
      body,
    }),
  update: (projectId: string, taskId: string, body: UpdateTaskRequest) =>
    apiFetch<TaskResponse>(`/api/projects/${projectId}/tasks/${taskId}`, {
      method: "PUT",
      body,
    }),
  remove: (projectId: string, taskId: string) =>
    apiFetch<void>(`/api/projects/${projectId}/tasks/${taskId}`, {
      method: "DELETE",
    }),
};

export const commentsApi = {
  list: (
    projectId: string,
    taskId: string,
    page: number,
    pageSize = 20,
    signal?: AbortSignal,
  ) =>
    apiFetch<PagedComments>(
      `/api/projects/${projectId}/tasks/${taskId}/comments${query({ page, pageSize })}`,
      { signal },
    ),
  create: (projectId: string, taskId: string, body: CreateCommentRequest) =>
    apiFetch<CommentResponse>(
      `/api/projects/${projectId}/tasks/${taskId}/comments`,
      { method: "POST", body },
    ),
};

export const attachmentsApi = {
  list: (projectId: string, taskId: string, signal?: AbortSignal) =>
    apiFetch<AttachmentResponse[]>(
      `/api/projects/${projectId}/tasks/${taskId}/attachments`,
      { signal },
    ),
  upload: (projectId: string, taskId: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return apiFetch<AttachmentResponse>(
      `/api/projects/${projectId}/tasks/${taskId}/attachments`,
      { method: "POST", rawBody: form },
    );
  },
  download: (projectId: string, taskId: string, attachmentId: string) =>
    apiFetchBlob(
      `/api/projects/${projectId}/tasks/${taskId}/attachments/${attachmentId}/content`,
    ),
};

export const notificationsApi = {
  list: (page: number, pageSize = 20, signal?: AbortSignal) =>
    apiFetch<PagedNotifications>(
      `/api/notifications${query({ page, pageSize })}`,
      { signal },
    ),
  markRead: (id: string) =>
    apiFetch<void>(`/api/notifications/${id}/read`, { method: "PUT" }),
};

export const adminApi = {
  users: (
    params: { page?: number; pageSize?: number; search?: string },
    signal?: AbortSignal,
  ) => apiFetch<PagedAdminUsers>(`/api/admin/users${query(params)}`, { signal }),
  replaceRoles: (userId: string, body: ReplaceUserRolesRequest) =>
    apiFetch<AdminUserResponse>(`/api/admin/users/${userId}/roles`, {
      method: "PUT",
      body,
    }),
};

export type { NotificationResponse };

import { expect, type APIRequestContext } from "@playwright/test";

// Real-backend helpers for the F10-09 full-stack contract suite. Each test creates its
// own users with unique names, so there is no dependency on shared/pre-existing data.

const PASSWORD = "Password1";

export interface RegisteredUser {
  token: string;
  refreshToken: string;
  userId: string;
  userName: string;
}

function authHeaders(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
}

export async function registerUser(request: APIRequestContext): Promise<RegisteredUser> {
  const suffix = Math.random().toString(36).slice(2, 10);
  const userName = `e2e_${suffix}`;
  const response = await request.post("/api/auth/register", {
    data: {
      email: `${userName}@test.local`,
      userName,
      password: PASSWORD,
      displayName: "E2E User",
    },
  });
  expect(response.status(), "register should return 201").toBe(201);
  const body = await response.json();
  return {
    token: body.accessToken,
    refreshToken: body.refreshToken,
    userId: body.user.id,
    userName,
  };
}

export async function createProject(
  request: APIRequestContext,
  token: string,
  name = "E2E Project",
): Promise<string> {
  const response = await request.post("/api/projects", {
    headers: authHeaders(token),
    data: { name, description: null },
  });
  expect(response.status(), "create project should return 201").toBe(201);
  return (await response.json()).id;
}

export async function addMember(
  request: APIRequestContext,
  token: string,
  projectId: string,
  userId: string,
): Promise<void> {
  const response = await request.post(`/api/projects/${projectId}/members`, {
    headers: authHeaders(token),
    data: { userId },
  });
  expect(response.status(), "add member should return 201").toBe(201);
}

export interface CreatedTask {
  id: string;
  version: string;
}

export async function createTask(
  request: APIRequestContext,
  token: string,
  projectId: string,
  assigneeUserId: string | null = null,
): Promise<CreatedTask> {
  const response = await request.post(`/api/projects/${projectId}/tasks`, {
    headers: authHeaders(token),
    data: {
      title: "Task",
      description: null,
      priority: "Medium",
      dueDate: null,
      assigneeUserId,
    },
  });
  expect(response.status(), "create task should return 201").toBe(201);
  const body = await response.json();
  return { id: body.id, version: body.version };
}

export async function addComment(
  request: APIRequestContext,
  token: string,
  projectId: string,
  taskId: string,
  content: string,
): Promise<void> {
  const response = await request.post(
    `/api/projects/${projectId}/tasks/${taskId}/comments`,
    { headers: authHeaders(token), data: { content } },
  );
  expect(response.status(), "add comment should return 201").toBe(201);
}

export function bearer(token: string): { headers: Record<string, string> } {
  return { headers: authHeaders(token) };
}

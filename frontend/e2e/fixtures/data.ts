import type { TaskResponse } from "@/lib/api/types";

// Sözleşmeyle birebir örnek veriler (backend Contracts/*.cs alanları).
// Alan adları ve tipleri üretilen src/lib/api/schema.d.ts ile aynıdır.

export const USERS = {
  ayse: {
    id: "2c9e4b7a-1111-4c3f-9a2b-000000008a12",
    email: "ayse@ornek.com",
    userName: "aysedemir",
    displayName: "Ayşe Demir",
    roles: ["Member", "ProjectManager"],
  },
  superAdmin: {
    id: "9b6e2f4a-2222-4c3f-9a2b-00000000d208",
    email: "super@ornek.com",
    userName: "superadmin",
    displayName: "Süper Yönetici",
    roles: ["SuperAdmin"],
  },
} as const;

export function authResponse(user: (typeof USERS)[keyof typeof USERS]) {
  return {
    accessToken: "test-access-token",
    expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    refreshToken: "test-refresh-token",
    refreshTokenExpiresAtUtc: new Date(Date.now() + 86_400_000).toISOString(),
    user,
  };
}

export const PROJECT_ID = "3f5a1c88-3333-4c3f-9a2b-00000000c556";

export const PROJECT = {
  id: PROJECT_ID,
  name: "Web Sitesi Yenileme",
  description: "Kurumsal sitenin yeni tasarım ve altyapı geçişi",
  ownerUserId: USERS.ayse.id,
  createdAtUtc: "2026-06-03T09:00:00+00:00",
  updatedAtUtc: "2026-07-19T16:30:00+00:00",
};

export const OTHER_PROJECT = {
  id: "7d1f9c3b-4444-4c3f-9a2b-00000000f931",
  name: "Mobil Uygulama v2",
  description: null,
  ownerUserId: USERS.superAdmin.id,
  createdAtUtc: "2026-05-18T11:20:00+00:00",
  updatedAtUtc: null,
};

export const STATISTICS = {
  projectId: PROJECT_ID,
  totalTasks: 24,
  todoTasks: 9,
  inProgressTasks: 6,
  completedTasks: 7,
  cancelledTasks: 2,
  overdueTasks: 3,
  completionPercentage: 29.17,
};

function summary(user: (typeof USERS)[keyof typeof USERS]) {
  return { id: user.id, userName: user.userName, displayName: user.displayName };
}

export const MEMBERS = [
  {
    userId: USERS.ayse.id,
    user: summary(USERS.ayse),
    joinedAtUtc: "2026-06-03T09:00:00+00:00",
  },
  {
    userId: USERS.superAdmin.id,
    user: summary(USERS.superAdmin),
    joinedAtUtc: "2026-06-05T14:10:00+00:00",
  },
];

// /api/users arama sonuçları: güvenli özet (e-posta ve rol yok).
export const DIRECTORY = [
  summary(USERS.ayse),
  summary(USERS.superAdmin),
  {
    id: "5e0d8a2c-5555-4c3f-9a2b-00000000c556",
    userName: "zeyneparslan",
    displayName: "Zeynep Arslan",
  },
];

// Üretilen sözleşme tipiyle bağlanır: alan adı veya nullability değişirse
// fixture'lar derlemede kırılır.
function task(overrides: Partial<TaskResponse> = {}): TaskResponse {
  return {
    id: "11111111-0000-4c3f-9a2b-000000000001",
    projectId: PROJECT_ID,
    title: "Ödeme sayfası hata düzeltmesi",
    description: "3D Secure yönlendirmesi sonrası sepetin boşalması hatası.",
    status: "InProgress",
    priority: "Critical",
    dueDate: "2026-07-18",
    assigneeUserId: USERS.superAdmin.id,
    isOverdue: true,
    createdAtUtc: "2026-07-02T11:12:00+00:00",
    updatedAtUtc: "2026-07-19T16:05:00+00:00",
    ...overrides,
  };
}

export const TASKS = [
  task(),
  task({
    id: "11111111-0000-4c3f-9a2b-000000000002",
    title: "Ana sayfa hero bölümü revizyonu",
    description: null,
    status: "Todo",
    priority: "High",
    dueDate: "2026-07-24",
    assigneeUserId: USERS.ayse.id,
    isOverdue: false,
    createdAtUtc: "2026-07-05T08:00:00+00:00",
    updatedAtUtc: null,
  }),
  task({
    id: "11111111-0000-4c3f-9a2b-000000000003",
    title: "SEO meta etiketleri güncellemesi",
    description: null,
    status: "Todo",
    priority: "Medium",
    dueDate: "2026-07-28",
    assigneeUserId: null,
    isOverdue: false,
    createdAtUtc: "2026-07-08T13:45:00+00:00",
    updatedAtUtc: null,
  }),
  task({
    id: "11111111-0000-4c3f-9a2b-000000000004",
    title: "Footer bağlantılarını düzelt",
    description: null,
    status: "Completed",
    priority: "Low",
    dueDate: "2026-07-12",
    assigneeUserId: USERS.ayse.id,
    isOverdue: false,
    createdAtUtc: "2026-06-20T10:00:00+00:00",
    updatedAtUtc: "2026-07-12T09:30:00+00:00",
  }),
];

export function paged<T>(items: T[], page = 1, pageSize = 10, totalCount = items.length) {
  return {
    items,
    page,
    pageSize,
    totalCount,
    totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
  };
}

export const NOTIFICATIONS = [
  {
    id: "aaaa1111-0000-4c3f-9a2b-000000000001",
    taskItemId: TASKS[0].id,
    type: "TaskAssigned",
    message: 'Size "Ödeme sayfası hata düzeltmesi" görevi atandı.',
    isRead: false,
    createdAtUtc: new Date(Date.now() - 12 * 60_000).toISOString(),
    readAtUtc: null,
  },
  {
    id: "aaaa1111-0000-4c3f-9a2b-000000000002",
    taskItemId: TASKS[3].id,
    type: "TaskCompleted",
    message: '"Footer bağlantılarını düzelt" görevi tamamlandı.',
    isRead: true,
    createdAtUtc: new Date(Date.now() - 26 * 3_600_000).toISOString(),
    readAtUtc: new Date(Date.now() - 20 * 3_600_000).toISOString(),
  },
];

export const ADMIN_USERS = [
  {
    id: USERS.ayse.id,
    email: USERS.ayse.email,
    userName: USERS.ayse.userName,
    displayName: USERS.ayse.displayName,
    roles: ["Member", "ProjectManager"],
  },
  {
    id: USERS.superAdmin.id,
    email: USERS.superAdmin.email,
    userName: USERS.superAdmin.userName,
    displayName: USERS.superAdmin.displayName,
    roles: ["SuperAdmin"],
  },
];

/** Backend'in application/problem+json gövdesi. */
export function problem(
  status: number,
  title: string,
  detail: string,
  errors?: Record<string, string[]>,
) {
  return { status, title, detail, ...(errors ? { errors } : {}) };
}

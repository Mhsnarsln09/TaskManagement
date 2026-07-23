import type { NotificationResponse, NotificationType } from "@/lib/api/types";

// F10-05: birincil bildirim metni backend'in İngilizce `message` alanından değil,
// yapılandırılmış `type` alanından Türkçeye çevrilir (B10-07 lokalizasyon kararı).
const TYPE_LABELS: Record<NotificationType, string> = {
  TaskAssigned: "Size bir görev atandı",
  TaskStatusChanged: "Görev durumu değişti",
  DueDateReminder: "Görev son tarihi yaklaşıyor",
};

export function notificationTitle(type: NotificationType): string {
  return TYPE_LABELS[type] ?? "Bildirim";
}

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

// Bildirim ilgili göreve linklenebilir mi? Görev rotaları proje kapsamlıdır; eski
// (migration öncesi) kayıtlar boş projectId taşıyabilir, onlar linklenmez.
export function notificationHref(
  notification: Pick<NotificationResponse, "projectId" | "taskItemId">,
): string | null {
  if (!notification.projectId || notification.projectId === EMPTY_GUID) {
    return null;
  }
  return `/projects/${notification.projectId}/tasks?task=${notification.taskItemId}`;
}

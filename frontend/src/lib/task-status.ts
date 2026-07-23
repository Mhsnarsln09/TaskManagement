import type { WorkItemStatus } from "@/lib/api/types";

// Görev durum makinesi (backend TECHNICAL-DECISIONS.md ile aynı):
//   Todo       -> InProgress | Completed | Cancelled
//   InProgress -> Completed  | Cancelled
//   Completed  -> InProgress (yeniden açma)
//   Cancelled  -> (terminal)
// `Todo -> Completed` backend'de bir kolaylık olarak kabul edilir (Start + Complete
// olarak çalışır), böylece atanmış kullanıcı bir Todo görevi önce InProgress yapmadan
// doğrudan tamamlayabilir. UI yalnız geçerli hedefleri sunar; nihai kural yine
// backend'dedir (geçersiz geçiş 409'a düşer).
const TRANSITIONS: Record<WorkItemStatus, WorkItemStatus[]> = {
  Todo: ["InProgress", "Completed", "Cancelled"],
  InProgress: ["Completed", "Cancelled"],
  Completed: ["InProgress"],
  Cancelled: [],
};

export function allowedNextStatuses(current: WorkItemStatus): WorkItemStatus[] {
  return TRANSITIONS[current];
}

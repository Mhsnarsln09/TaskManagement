// Backend FileUpload yapılandırmasının aynası (DESIGN-DECISIONS.md §4).
// Kaynak: backend/TaskManagement.Api/appsettings.json → FileUpload.

export const MAX_UPLOAD_SIZE_BYTES = 5_242_880; // 5 MB

export const ALLOWED_UPLOAD_EXTENSIONS = [
  ".png",
  ".jpg",
  ".jpeg",
  ".gif",
  ".pdf",
  ".txt",
  ".csv",
] as const;

export const UPLOAD_RULES_TEXT = `En fazla 5 MB; izin verilen türler: ${ALLOWED_UPLOAD_EXTENSIONS.map((extension) => extension.slice(1)).join(", ")}.`;

export function validateUpload(file: File): string | null {
  const dot = file.name.lastIndexOf(".");
  const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";
  if (!(ALLOWED_UPLOAD_EXTENSIONS as readonly string[]).includes(extension)) {
    return `"${file.name}" yüklenemedi. ${UPLOAD_RULES_TEXT}`;
  }
  if (file.size > MAX_UPLOAD_SIZE_BYTES) {
    return `"${file.name}" 5 MB sınırını aşıyor.`;
  }
  return null;
}

export function formatFileSize(bytes: number): string {
  if (bytes >= 1_048_576) {
    return `${(bytes / 1_048_576).toLocaleString("tr-TR", { maximumFractionDigits: 1 })} MB`;
  }
  if (bytes >= 1024) return `${Math.round(bytes / 1024)} KB`;
  return `${bytes} B`;
}

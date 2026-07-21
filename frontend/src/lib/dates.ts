// PRD §5: tarihler kullanıcı saat diliminde tr-TR gösterilir; API'ye
// sözleşmedeki formatta gider (DateTimeOffset ISO / DateOnly yyyy-MM-dd).

const dateFormat = new Intl.DateTimeFormat("tr-TR", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

const dateTimeFormat = new Intl.DateTimeFormat("tr-TR", {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export function formatDate(value: string | Date): string {
  return dateFormat.format(typeof value === "string" ? new Date(value) : value);
}

export function formatDateTime(value: string | Date): string {
  return dateTimeFormat.format(
    typeof value === "string" ? new Date(value) : value,
  );
}

/** DateOnly (yyyy-MM-dd) değerini yerel gün olarak biçimler (TZ kayması olmadan). */
export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split("-").map(Number);
  return dateFormat.format(new Date(year, month - 1, day));
}

export function toDateOnly(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

export function parseDateOnly(value: string): Date {
  const [year, month, day] = value.split("-").map(Number);
  return new Date(year, month - 1, day);
}

/** Göreli zaman: "12 dk önce", "2 sa önce", "Dün", sonrası tarih. */
export function formatRelative(value: string): string {
  const then = new Date(value).getTime();
  const now = Date.now();
  const minutes = Math.round((now - then) / 60_000);
  if (minutes < 1) return "şimdi";
  if (minutes < 60) return `${minutes} dk önce`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours} sa önce`;
  const days = Math.round(hours / 24);
  if (days === 1) return "Dün";
  if (days < 7) return `${days} gün önce`;
  return formatDate(value);
}

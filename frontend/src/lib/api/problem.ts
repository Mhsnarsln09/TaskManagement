// RFC 9457 Problem Details sözleşmesi (FRONTEND-INTEGRATION.md "Hata sözleşmesi").
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;
  /** 429 yanıtındaki Retry-After başlığı (saniye); yoksa null. */
  readonly retryAfterSeconds: number | null;

  constructor(
    status: number,
    problem: ProblemDetails,
    retryAfterSeconds: number | null = null,
  ) {
    super(problem.detail ?? problem.title ?? `HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
    this.retryAfterSeconds = retryAfterSeconds;
  }
}

/** Sunucuya hiç ulaşılamadı (DNS, çevrimdışı, CORS, iptal dışı ağ hatası). */
export class NetworkError extends Error {
  constructor(cause?: unknown) {
    super("Sunucuya ulaşılamıyor.");
    this.name = "NetworkError";
    this.cause = cause;
  }
}

export function isApiError(error: unknown, status?: number): error is ApiError {
  return (
    error instanceof ApiError && (status === undefined || error.status === status)
  );
}

export function isNetworkError(error: unknown): error is NetworkError {
  return error instanceof NetworkError;
}

/**
 * Alan adlarını Problem Details `errors` sözlüğünden form alanlarına eşler.
 * Backend PascalCase ("Title") veya camelCase ("title") anahtar dönebilir.
 */
export function fieldErrors(
  problem: ProblemDetails,
  field: string,
): string[] {
  const errors = problem.errors;
  if (!errors) return [];
  const lower = field.toLowerCase();
  for (const [key, messages] of Object.entries(errors)) {
    if (key.toLowerCase() === lower) return messages;
  }
  return [];
}

/** Kullanıcıya gösterilecek genel mesajı üretir. */
export function problemMessage(error: unknown): string {
  if (isNetworkError(error)) return error.message;
  if (error instanceof ApiError) {
    return (
      error.problem.detail ??
      error.problem.title ??
      "Beklenmeyen bir hata oluştu."
    );
  }
  return "Beklenmeyen bir hata oluştu.";
}

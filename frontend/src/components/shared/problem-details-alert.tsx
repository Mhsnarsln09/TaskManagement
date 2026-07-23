"use client";

import { useEffect, useState } from "react";
import { AlertTriangle, WifiOff, XCircle } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  ApiError,
  isNetworkError,
  problemMessage,
} from "@/lib/api/problem";
import { cn } from "@/lib/utils";

// Tek hata sunum kaynağı (DESIGN-DECISIONS.md §8): severity + başlık + detay +
// isteğe bağlı eylem. 409/429 amber, diğerleri kırmızı; 429'da geri sayım.

const DEFAULT_RETRY_AFTER_SECONDS = 15;

interface ProblemDetailsAlertProps {
  error: unknown;
  /** Bağlama özgü başlık; verilmezse durumdan türetilir. */
  title?: string;
  /** Bağlama özgü açıklama; verilmezse Problem Details detayından türetilir. */
  description?: string;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
}

function defaultTitle(error: unknown): string {
  if (isNetworkError(error)) return "Sunucuya ulaşılamıyor.";
  if (error instanceof ApiError) {
    switch (error.status) {
      case 400:
        return "İstek doğrulanamadı.";
      case 401:
        return "Giriş başarısız.";
      case 403:
        return "Bu işlem için yetkiniz yok.";
      case 404:
        return "Kayıt bulunamadı.";
      case 409:
        return "İşlem çakıştı.";
      case 429:
        return "Çok fazla istek.";
      default:
        return "Beklenmeyen bir hata oluştu.";
    }
  }
  return "Beklenmeyen bir hata oluştu.";
}

export function ProblemDetailsAlert({
  error,
  title,
  description: descriptionOverride,
  onRetry,
  retryLabel = "Tekrar dene",
  className,
}: ProblemDetailsAlertProps) {
  const status = error instanceof ApiError ? error.status : null;
  const isAmber = status === 409 || status === 429;
  const isRateLimited = status === 429;

  const initialCountdown = isRateLimited
    ? ((error as ApiError).retryAfterSeconds ?? DEFAULT_RETRY_AFTER_SECONDS)
    : 0;

  const [countdown, setCountdown] = useState(initialCountdown);
  // Render sırasında türetilmiş durumu sıfırlama deseni: hata nesnesi
  // değişince geri sayım baştan başlar (efekt içinde senkron setState yok).
  const [previousError, setPreviousError] = useState<unknown>(error);
  if (previousError !== error) {
    setPreviousError(error);
    setCountdown(initialCountdown);
  }

  useEffect(() => {
    if (!isRateLimited) return;
    const timer = setInterval(
      () => setCountdown((current) => (current > 0 ? current - 1 : 0)),
      1000,
    );
    return () => clearInterval(timer);
  }, [isRateLimited]);

  const Icon = isNetworkError(error)
    ? WifiOff
    : isAmber
      ? AlertTriangle
      : XCircle;

  const description = isRateLimited
    ? countdown > 0
      ? `${countdown} sn sonra tekrar deneyebilirsiniz.`
      : "Şimdi tekrar deneyebilirsiniz."
    : (descriptionOverride ?? problemMessage(error));

  return (
    <Alert
      role="alert"
      className={cn(
        isAmber
          ? "border-warning/40 bg-warning/5 text-warning [&>svg]:text-warning"
          : "border-destructive/40 bg-destructive/5 text-destructive [&>svg]:text-destructive",
        className,
      )}
    >
      <Icon aria-hidden className="size-4" />
      <AlertTitle>{title ?? defaultTitle(error)}</AlertTitle>
      <AlertDescription
        className={cn(isAmber ? "text-warning/90" : "text-destructive/90")}
      >
        <span>
          {description}
          {status ? (
            <span className="ml-1 font-mono text-xs opacity-70">({status})</span>
          ) : null}
        </span>
        {onRetry ? (
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="mt-2 w-fit"
            onClick={onRetry}
            disabled={isRateLimited && countdown > 0}
          >
            {retryLabel}
          </Button>
        ) : null}
      </AlertDescription>
    </Alert>
  );
}

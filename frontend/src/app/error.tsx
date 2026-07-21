"use client";

import { FullPageState } from "@/components/shared/states";

export default function GlobalError({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <FullPageState
      title="Bir şeyler ters gitti"
      description="Beklenmeyen bir hata oluştu. Sorun sürerse daha sonra tekrar deneyin."
      actionLabel="Tekrar dene"
      onAction={reset}
    />
  );
}

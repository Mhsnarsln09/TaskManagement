import { Suspense } from "react";
import type { Metadata } from "next";
import { AuthCard } from "@/features/auth/auth-card";
import { LoginForm } from "@/features/auth/login-form";

export const metadata: Metadata = { title: "Oturum aç" };

export default function LoginPage() {
  return (
    <AuthCard title="Oturum aç" subtitle="Projelerinize devam edin">
      {/* useSearchParams istemci sınırı gerektirir. */}
      <Suspense>
        <LoginForm />
      </Suspense>
    </AuthCard>
  );
}

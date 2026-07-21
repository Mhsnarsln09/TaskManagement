import type { Metadata } from "next";
import { AuthCard } from "@/features/auth/auth-card";
import { RegisterForm } from "@/features/auth/register-form";

export const metadata: Metadata = { title: "Hesap oluştur" };

export default function RegisterPage() {
  return (
    <AuthCard
      title="Hesap oluştur"
      subtitle="Üye olarak katılırsınız; rol seçimi yoktur."
    >
      <RegisterForm />
    </AuthCard>
  );
}

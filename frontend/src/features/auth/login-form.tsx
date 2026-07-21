"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Eye, EyeOff, Info, Loader2 } from "lucide-react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { useAuth } from "@/lib/auth/auth-context";

const schema = z.object({
  userNameOrEmail: z.string().min(1, "Kullanıcı adı veya e-posta zorunludur."),
  password: z.string().min(1, "Parola zorunludur."),
});

type FormValues = z.infer<typeof schema>;

/** returnUrl yalnız uygulama içi yol olabilir (open redirect koruması). */
function safeReturnUrl(value: string | null): string {
  if (value && value.startsWith("/") && !value.startsWith("//")) return value;
  return "/projects";
}

export function LoginForm() {
  const { login } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<unknown>(null);
  const [showPassword, setShowPassword] = useState(false);
  const expired = searchParams.get("expired") === "1";

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { userNameOrEmail: "", password: "" },
  });

  const submitting = form.formState.isSubmitting;

  async function onSubmit(values: FormValues) {
    setError(null);
    try {
      await login(values);
      router.replace(safeReturnUrl(searchParams.get("returnUrl")));
    } catch (cause) {
      setError(cause);
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {expired && !error ? (
          <Alert className="border-info/30 bg-info/5 text-info [&>svg]:text-info">
            <Info aria-hidden className="size-4" />
            <AlertTitle>Oturum süreniz doldu.</AlertTitle>
            <AlertDescription className="text-info/90">
              Güvenliğiniz için lütfen yeniden oturum açın.
            </AlertDescription>
          </Alert>
        ) : null}
        {error ? (
          <ProblemDetailsAlert
            error={error}
            title="Giriş başarısız."
          />
        ) : null}
        <FormField
          control={form.control}
          name="userNameOrEmail"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Kullanıcı adı veya e-posta</FormLabel>
              <FormControl>
                <Input
                  autoComplete="username"
                  autoFocus
                  disabled={submitting}
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="password"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Parola</FormLabel>
              <div className="relative">
                <FormControl>
                  <Input
                    type={showPassword ? "text" : "password"}
                    autoComplete="current-password"
                    disabled={submitting}
                    className="pr-9"
                    {...field}
                  />
                </FormControl>
                <button
                    type="button"
                    aria-label={showPassword ? "Parolayı gizle" : "Parolayı göster"}
                    className="absolute inset-y-0 right-0 flex w-9 items-center justify-center text-muted-foreground hover:text-foreground"
                    onClick={() => setShowPassword((current) => !current)}
                  >
                    {showPassword ? (
                      <EyeOff aria-hidden className="size-4" />
                    ) : (
                      <Eye aria-hidden className="size-4" />
                    )}
                </button>
              </div>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit" className="w-full" disabled={submitting}>
          {submitting ? (
            <>
              <Loader2 aria-hidden className="size-4 animate-spin" />
              Oturum açılıyor…
            </>
          ) : (
            "Oturum aç"
          )}
        </Button>
        <p className="text-center text-sm text-muted-foreground">
          Hesabınız yok mu?{" "}
          <Link href="/register" className="font-medium text-primary hover:underline">
            Kayıt olun
          </Link>
        </p>
      </form>
    </Form>
  );
}

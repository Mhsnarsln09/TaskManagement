"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { Eye, EyeOff, Loader2 } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { ProblemDetailsAlert } from "@/components/shared/problem-details-alert";
import { useAuth } from "@/lib/auth/auth-context";
import { ApiError, fieldErrors } from "@/lib/api/problem";

// Alanlar sözleşmeyle birebir (FRONTEND-INTEGRATION.md): displayName? +
// userName + email + password. Rol seçici, parola onayı, KVKK kutusu YOK.
const schema = z.object({
  displayName: z.string().max(120, "En fazla 120 karakter.").optional(),
  userName: z
    .string()
    .min(3, "En az 3 karakter olmalıdır.")
    .max(256, "En fazla 256 karakter.")
    .refine((value) => !/\s/.test(value), "Kullanıcı adı boşluk içeremez."),
  email: z.string().email("Geçerli bir e-posta adresi girin."),
  password: z
    .string()
    .min(8, "En az 8 karakter olmalıdır.")
    .max(128, "En fazla 128 karakter.")
    .regex(/[A-Z]/, "En az bir büyük harf içermelidir.")
    .regex(/[a-z]/, "En az bir küçük harf içermelidir.")
    .regex(/[0-9]/, "En az bir rakam içermelidir."),
});

type FormValues = z.infer<typeof schema>;

export function RegisterForm() {
  const { register } = useAuth();
  const router = useRouter();
  const [error, setError] = useState<unknown>(null);
  const [showPassword, setShowPassword] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: "", userName: "", email: "", password: "" },
  });

  const submitting = form.formState.isSubmitting;

  async function onSubmit(values: FormValues) {
    setError(null);
    try {
      await register({
        displayName: values.displayName?.trim() ? values.displayName.trim() : null,
        userName: values.userName,
        email: values.email,
        password: values.password,
      });
      router.replace("/projects");
    } catch (cause) {
      // 400 alan hataları ilgili FormField altına eşlenir (DESIGN-DECISIONS.md §8).
      if (cause instanceof ApiError && cause.status === 400 && cause.problem.errors) {
        let mapped = false;
        for (const field of ["displayName", "userName", "email", "password"] as const) {
          const messages = fieldErrors(cause.problem, field);
          if (messages.length > 0) {
            form.setError(field, { type: "server", message: messages[0] });
            mapped = true;
          }
        }
        if (mapped) return;
      }
      setError(cause);
    }
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {error ? <ProblemDetailsAlert error={error} title="Kayıt başarısız." /> : null}
        <FormField
          control={form.control}
          name="displayName"
          render={({ field }) => (
            <FormItem>
              <FormLabel>
                Görünen ad{" "}
                <span className="font-normal text-muted-foreground">(isteğe bağlı)</span>
              </FormLabel>
              <FormControl>
                <Input autoComplete="name" disabled={submitting} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="userName"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Kullanıcı adı</FormLabel>
              <FormControl>
                <Input autoComplete="username" disabled={submitting} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>E-posta</FormLabel>
              <FormControl>
                <Input type="email" autoComplete="email" disabled={submitting} {...field} />
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
                    autoComplete="new-password"
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
              <FormDescription>
                En az 8 karakter; büyük harf, küçük harf ve rakam içermeli.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit" className="w-full" disabled={submitting}>
          {submitting ? (
            <>
              <Loader2 aria-hidden className="size-4 animate-spin" />
              Hesap oluşturuluyor…
            </>
          ) : (
            "Hesap oluştur"
          )}
        </Button>
        <p className="text-center text-sm text-muted-foreground">
          Zaten hesabınız var mı?{" "}
          <Link href="/login" className="font-medium text-primary hover:underline">
            Oturum açın
          </Link>
        </p>
      </form>
    </Form>
  );
}

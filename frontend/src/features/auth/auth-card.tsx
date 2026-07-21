import { Check } from "lucide-react";

/** Giriş/kayıt sayfalarının ortak dış çerçevesi (tasarım §01). */
export function AuthCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: React.ReactNode;
}) {
  return (
    <main className="flex flex-1 items-center justify-center bg-muted/40 px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center justify-center gap-2">
          <span className="flex size-6 items-center justify-center rounded-md bg-primary">
            <Check aria-hidden className="size-3.5 text-primary-foreground" strokeWidth={3} />
          </span>
          <span className="text-sm font-bold tracking-tight">TaskManagement</span>
        </div>
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <h1 className="text-lg font-bold">{title}</h1>
          <p className="mb-5 mt-0.5 text-sm text-muted-foreground">{subtitle}</p>
          {children}
        </div>
      </div>
    </main>
  );
}

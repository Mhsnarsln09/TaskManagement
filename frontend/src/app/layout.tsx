import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "./providers";

// F10-07: Fontlar artık next/font/google yerine güvenli sistem font yığınıyla
// (globals.css `:root --font-sans` / `--font-geist-mono`) sağlanır; production build
// Google Fonts ağ erişimi gerektirmez.

export const metadata: Metadata = {
  title: {
    default: "TaskManagement",
    template: "%s · TaskManagement",
  },
  description: "Küçük ekipler için proje ve görev yönetimi.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr" className="h-full antialiased">
      <body className="flex min-h-full flex-col bg-background text-foreground">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}

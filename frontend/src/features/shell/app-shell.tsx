"use client";

import { Menu, RefreshCw, WifiOff } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { useRequireAuth } from "@/lib/auth/auth-context";
import { NotificationsProvider, useNotifications } from "@/features/notifications/notifications-provider";
import { NotificationCenter } from "@/features/notifications/notification-center";
import { AccountMenu, SidebarAccountFooter } from "./account-menu";
import { SidebarContent } from "./sidebar-nav";

// Masaüstü: sabit sol gezinim + kompakt üst bar. Mobil: Sheet çekmece
// (tasarım §02 mobil varyantı); tüm ana eylemler korunur.

function ConnectionBanner() {
  const { hubStatus } = useNotifications();
  // Bileşen yalnız istemcide render edilir (auth guard sonrası).
  const [online, setOnline] = useState(() =>
    typeof navigator === "undefined" ? true : navigator.onLine,
  );

  useEffect(() => {
    const goOnline = () => setOnline(true);
    const goOffline = () => setOnline(false);
    window.addEventListener("online", goOnline);
    window.addEventListener("offline", goOffline);
    return () => {
      window.removeEventListener("online", goOnline);
      window.removeEventListener("offline", goOffline);
    };
  }, []);

  if (!online) {
    return (
      <div
        role="status"
        className="flex items-center gap-2 bg-foreground px-4 py-1.5 text-xs font-medium text-background"
      >
        <WifiOff aria-hidden className="size-3.5" />
        Çevrimdışısınız. Bağlantı gelince veriler yenilenir.
      </div>
    );
  }
  if (hubStatus === "reconnecting") {
    return (
      <div
        role="status"
        className="flex items-center gap-2 bg-warning/15 px-4 py-1.5 text-xs font-medium text-warning"
      >
        <RefreshCw aria-hidden className="size-3.5 animate-spin" />
        Bağlantı koptu. Yeniden bağlanılıyor… Bağlanınca liste otomatik yenilenir.
      </div>
    );
  }
  return null;
}

function ShellLayout({ children }: { children: React.ReactNode }) {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <div className="flex min-h-dvh w-full">
      <aside className="sticky top-0 hidden h-dvh w-58 shrink-0 flex-col border-r bg-sidebar md:flex">
        <SidebarContent />
        <SidebarAccountFooter />
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 border-b bg-background/95 backdrop-blur">
          <ConnectionBanner />
          <div className="flex h-12 items-center gap-2 px-4">
            <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
              <SheetTrigger asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Menüyü aç"
                  className="md:hidden"
                >
                  <Menu aria-hidden className="size-4" />
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-72 p-0">
                <SheetTitle className="sr-only">Gezinim</SheetTitle>
                <div className="flex h-full flex-col">
                  <SidebarContent onNavigate={() => setMobileNavOpen(false)} />
                  <SidebarAccountFooter />
                </div>
              </SheetContent>
            </Sheet>
            <div className="min-w-0 flex-1" />
            <NotificationCenter />
            <AccountMenu />
          </div>
        </header>
        <main className="flex flex-1 flex-col px-4 py-5 md:px-6">{children}</main>
      </div>
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useRequireAuth();

  // Yönlendirme tamamlanana kadar korumalı içerik render edilmez.
  if (!isAuthenticated) return null;

  return (
    <NotificationsProvider>
      <ShellLayout>{children}</ShellLayout>
    </NotificationsProvider>
  );
}

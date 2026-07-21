"use client";

import { Check, Copy, LogOut, MoreHorizontal } from "lucide-react";
import { useState } from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { RoleBadge } from "@/components/shared/badges";
import { useAuth } from "@/lib/auth/auth-context";
import { initialsFor } from "@/lib/user-directory";

// Tasarım §13: kimlik özeti + roller (auth yanıtından) + Çıkış. Profil
// düzenleme endpoint'i yok.
export function AccountMenu({ trigger }: { trigger?: React.ReactNode }) {
  const { user, logout } = useAuth();
  const [copied, setCopied] = useState(false);
  if (!user) return null;

  const name = user.displayName?.trim() ? user.displayName : user.userName;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {trigger ?? (
          <button
            type="button"
            aria-label="Hesap menüsü"
            className="flex items-center gap-2 rounded-md p-1 hover:bg-muted"
          >
            <span className="flex size-7 items-center justify-center rounded-md bg-accent text-xs font-bold text-accent-foreground">
              {initialsFor(name)}
            </span>
          </button>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuLabel>
          <div className="text-sm font-semibold">{name}</div>
          <div className="truncate text-xs font-normal text-muted-foreground">
            @{user.userName} · {user.email}
          </div>
        </DropdownMenuLabel>
        <div className="flex flex-wrap gap-1 px-2 pb-1.5">
          {user.roles.map((role) => (
            <RoleBadge key={role} role={role} />
          ))}
        </div>
        <DropdownMenuSeparator />
        {/* Üye ekleme GUID ile yapıldığından kullanıcı kendi kimliğini paylaşabilmelidir (tasarım §07). */}
        <DropdownMenuItem
          onSelect={(event) => {
            event.preventDefault();
            void navigator.clipboard.writeText(user.id).then(() => {
              setCopied(true);
              setTimeout(() => setCopied(false), 1500);
            });
          }}
        >
          {copied ? (
            <Check aria-hidden className="size-4 text-success" />
          ) : (
            <Copy aria-hidden className="size-4" />
          )}
          Kullanıcı ID&apos;sini kopyala
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onSelect={() => logout()}>
          <LogOut aria-hidden className="size-4" />
          Çıkış yap
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export function SidebarAccountFooter() {
  const { user } = useAuth();
  if (!user) return null;
  const name = user.displayName?.trim() ? user.displayName : user.userName;

  return (
    <div className="mt-auto flex items-center gap-2.5 border-t px-3 py-3">
      <span className="flex size-7 shrink-0 items-center justify-center rounded-md bg-accent text-[11px] font-bold text-accent-foreground">
        {initialsFor(name)}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-[13px] font-semibold">{name}</span>
        <span className="block truncate text-[11px] text-muted-foreground">
          @{user.userName}
        </span>
      </span>
      <AccountMenu
        trigger={
          <button
            type="button"
            aria-label="Hesap menüsü"
            title="Hesap menüsü"
            className="rounded-md p-1 text-muted-foreground hover:bg-muted hover:text-foreground"
          >
            <MoreHorizontal aria-hidden className="size-4" />
          </button>
        }
      />
    </div>
  );
}

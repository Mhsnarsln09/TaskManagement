"use client";

import { Check, Copy } from "lucide-react";
import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/auth-context";
import {
  displayNameFor,
  shortId,
  useDirectoryEntry,
} from "@/lib/user-directory";
import { cn } from "@/lib/utils";

// DESIGN-DECISIONS.md §1: dizinde ad varsa ad + tooltip'te GUID; yoksa
// kısaltılmış GUID + kopyalama. Oturum kullanıcısına "siz" rozeti.

interface UserDisplayProps {
  userId: string | null | undefined;
  /** Atanmamış görünümü için metin (görev listesi "Atanmamış"). */
  emptyLabel?: string;
  showYouBadge?: boolean;
  withCopy?: boolean;
  className?: string;
}

export function UserDisplay({
  userId,
  emptyLabel = "Atanmamış",
  showYouBadge = true,
  withCopy = false,
  className,
}: UserDisplayProps) {
  const entry = useDirectoryEntry(userId);
  const { user } = useAuth();
  const [copied, setCopied] = useState(false);

  if (!userId) {
    return (
      <span className={cn("text-muted-foreground", className)}>
        {emptyLabel}
      </span>
    );
  }

  const isYou = user?.id === userId;
  const label = entry ? displayNameFor(entry) : shortId(userId);

  return (
    <span className={cn("inline-flex min-w-0 items-center gap-1.5", className)}>
      <Tooltip>
        <TooltipTrigger asChild>
          <span
            className={cn(
              "truncate",
              entry ? undefined : "font-mono text-xs text-muted-foreground",
            )}
          >
            {label}
          </span>
        </TooltipTrigger>
        <TooltipContent>
          <span className="font-mono text-xs">{userId}</span>
        </TooltipContent>
      </Tooltip>
      {showYouBadge && isYou ? (
        <Badge
          variant="outline"
          className="border-primary/30 bg-accent px-1.5 py-0 text-[10px] text-accent-foreground"
        >
          siz
        </Badge>
      ) : null}
      {withCopy ? (
        <Button
          type="button"
          variant="ghost"
          size="icon-xs"
          aria-label="Kullanıcı ID'sini kopyala"
          onClick={async () => {
            await navigator.clipboard.writeText(userId);
            setCopied(true);
            setTimeout(() => setCopied(false), 1500);
          }}
        >
          {copied ? (
            <Check aria-hidden className="size-3 text-success" />
          ) : (
            <Copy aria-hidden className="size-3" />
          )}
        </Button>
      ) : null}
    </span>
  );
}

"use client";

import { useQuery } from "@tanstack/react-query";
import { Check, ChevronsUpDown, Loader2, Search } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { Input } from "@/components/ui/input";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { usersApi } from "@/lib/api/endpoints";
import type { UserSummaryResponse } from "@/lib/api/types";
import { userDirectory } from "@/lib/user-directory";
import { cn } from "@/lib/utils";

// Kişi isimle seçilir, API'ye yalnız userId gider. Arama ucu güvenli özet döner:
// e-posta ve sistem rolleri burada gösterilmez (bkz. DESIGN-DECISIONS.md §1).

const MIN_SEARCH_LENGTH = 2;

interface UserPickerProps {
  value: UserSummaryResponse | null;
  onChange: (user: UserSummaryResponse | null) => void;
  /** Seçilemeyecek kişiler (ör. hâlihazırda üye olanlar). */
  excludeUserIds?: readonly string[];
  placeholder?: string;
  disabled?: boolean;
  id?: string;
  "aria-describedby"?: string;
}

export function UserPicker({
  value,
  onChange,
  excludeUserIds = [],
  placeholder = "Kişi arayın…",
  disabled = false,
  id,
  "aria-describedby": describedBy,
}: UserPickerProps) {
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState("");
  const [search, setSearch] = useState("");

  // Arama debounce 300 ms (SuperAdmin listesiyle aynı desen).
  useEffect(() => {
    const timer = setTimeout(() => setSearch(input.trim()), 300);
    return () => clearTimeout(timer);
  }, [input]);

  const enabled = open && search.length >= MIN_SEARCH_LENGTH;

  const { data, isFetching, error } = useQuery({
    queryKey: ["users", "search", search],
    queryFn: ({ signal }) => usersApi.search(search, signal),
    enabled,
  });

  // Bulunan kişiler dizini besler: liste ve rozetlerde de ad görünür.
  useEffect(() => {
    if (data) userDirectory.upsertMany(data);
  }, [data]);

  const results = (data ?? []).filter(
    (user) => !excludeUserIds.includes(user.id),
  );

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) setInput("");
      }}
    >
      <PopoverTrigger asChild>
        <Button
          id={id}
          type="button"
          variant="outline"
          role="combobox"
          aria-expanded={open}
          aria-describedby={describedBy}
          disabled={disabled}
          className={cn(
            "w-full justify-between font-normal",
            !value && "text-muted-foreground",
          )}
        >
          {value ? (
            <span className="truncate">
              {value.displayName?.trim() ? value.displayName : value.userName}
              <span className="ml-1.5 text-muted-foreground">
                @{value.userName}
              </span>
            </span>
          ) : (
            placeholder
          )}
          <ChevronsUpDown aria-hidden className="size-3.5 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-(--radix-popover-trigger-width) p-0"
      >
        {/* Filtreleme sunucuda yapılır; Command'ın kendi eşleştirmesi kapatılır. */}
        <Command shouldFilter={false}>
          <div className="flex items-center gap-2 border-b px-3">
            <Search aria-hidden className="size-4 shrink-0 text-muted-foreground" />
            <Input
              autoFocus
              value={input}
              onChange={(event) => setInput(event.target.value)}
              placeholder="İsim veya kullanıcı adı"
              aria-label="Kişi ara"
              className="h-9 border-0 px-0 shadow-none focus-visible:ring-0"
            />
            {isFetching ? (
              <Loader2 aria-hidden className="size-3.5 animate-spin text-muted-foreground" />
            ) : null}
          </div>
          <CommandList>
            {search.length < MIN_SEARCH_LENGTH ? (
              <p className="px-3 py-6 text-center text-sm text-muted-foreground">
                Aramak için en az {MIN_SEARCH_LENGTH} karakter yazın.
              </p>
            ) : error ? (
              <p role="alert" className="px-3 py-6 text-center text-sm text-destructive">
                Kişiler aranamadı.
              </p>
            ) : isFetching && results.length === 0 ? (
              <p className="px-3 py-6 text-center text-sm text-muted-foreground">
                Aranıyor…
              </p>
            ) : results.length === 0 ? (
              <CommandEmpty>
                &quot;{search}&quot; ile eşleşen kişi yok.
              </CommandEmpty>
            ) : (
              <CommandGroup>
                {results.map((user) => (
                  <CommandItem
                    key={user.id}
                    value={user.id}
                    onSelect={() => {
                      onChange(user);
                      setOpen(false);
                      setInput("");
                    }}
                  >
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-medium">
                        {user.displayName?.trim() ? user.displayName : user.userName}
                      </span>
                      <span className="block truncate text-xs text-muted-foreground">
                        @{user.userName}
                      </span>
                    </span>
                    {value?.id === user.id ? (
                      <Check aria-hidden className="size-4" />
                    ) : null}
                  </CommandItem>
                ))}
              </CommandGroup>
            )}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

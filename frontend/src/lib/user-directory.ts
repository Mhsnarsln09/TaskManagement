import { useSyncExternalStore } from "react";
import type { UserSummaryResponse } from "@/lib/api/types";

// DESIGN-DECISIONS.md §1: sözleşme çoğu yerde yalnız GUID döndürür. Ad döndüren
// yanıtlar (auth kullanıcı, yorum/ek yazarları, SuperAdmin listesi) bu dizini
// besler; UserDisplay eşleşme yoksa kısaltılmış GUID gösterir.

export interface DirectoryEntry {
  id: string;
  userName: string;
  displayName: string | null;
}

const entries = new Map<string, DirectoryEntry>();
const listeners = new Set<() => void>();
let version = 0;

function notify() {
  version += 1;
  for (const listener of listeners) listener();
}

export const userDirectory = {
  upsert(user: { id: string; userName: string; displayName?: string | null }) {
    const existing = entries.get(user.id);
    const next: DirectoryEntry = {
      id: user.id,
      userName: user.userName,
      displayName: user.displayName ?? null,
    };
    if (
      !existing ||
      existing.userName !== next.userName ||
      existing.displayName !== next.displayName
    ) {
      entries.set(user.id, next);
      notify();
    }
  },

  upsertMany(users: readonly UserSummaryResponse[]) {
    let changed = false;
    for (const user of users) {
      const existing = entries.get(user.id);
      if (
        !existing ||
        existing.userName !== user.userName ||
        existing.displayName !== (user.displayName ?? null)
      ) {
        entries.set(user.id, {
          id: user.id,
          userName: user.userName,
          displayName: user.displayName ?? null,
        });
        changed = true;
      }
    }
    if (changed) notify();
  },

  get(id: string): DirectoryEntry | undefined {
    return entries.get(id);
  },

  clear() {
    entries.clear();
    notify();
  },

  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
};

/** Kısaltılmış GUID: `2c9e4b7a…8a12` (tasarım §07). */
export function shortId(id: string): string {
  return `${id.slice(0, 8)}…${id.slice(-4)}`;
}

export function displayNameFor(entry: DirectoryEntry): string {
  return entry.displayName?.trim() ? entry.displayName : entry.userName;
}

/** Baş harfler (avatar kutusu için): "Ayşe Demir" → "AD". */
export function initialsFor(name: string): string {
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((part) => part[0]?.toLocaleUpperCase("tr-TR") ?? "").join("");
}

export function useDirectoryEntry(id: string | null | undefined) {
  return useSyncExternalStore(
    userDirectory.subscribe,
    () => (id ? (userDirectory.get(id) ?? null) : null),
    () => null,
  );
}

export function useDirectoryVersion() {
  return useSyncExternalStore(
    userDirectory.subscribe,
    () => version,
    () => 0,
  );
}

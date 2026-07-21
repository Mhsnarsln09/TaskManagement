"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface PaginationBarProps {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  itemCount: number;
  onPageChange: (page: number) => void;
  /** "24 görevden 1–10 gösteriliyor" kalıbındaki isim. */
  itemsLabel: string;
  className?: string;
}

function pageNumbers(page: number, totalPages: number): number[] {
  const pages = new Set<number>([1, totalPages, page - 1, page, page + 1]);
  return [...pages]
    .filter((candidate) => candidate >= 1 && candidate <= totalPages)
    .sort((a, b) => a - b);
}

export function PaginationBar({
  page,
  totalPages,
  totalCount,
  pageSize,
  itemCount,
  onPageChange,
  itemsLabel,
  className,
}: PaginationBarProps) {
  if (totalCount === 0) return null;
  const start = (page - 1) * pageSize + 1;
  const end = start + itemCount - 1;
  const numbers = pageNumbers(page, totalPages);

  return (
    <nav
      aria-label="Sayfalama"
      className={cn(
        "flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground",
        className,
      )}
    >
      <span>
        {totalCount} {itemsLabel} {start}–{end} gösteriliyor
      </span>
      <div className="flex items-center gap-1">
        <Button
          type="button"
          variant="outline"
          size="icon-sm"
          aria-label="Önceki sayfa"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          <ChevronLeft aria-hidden className="size-4" />
        </Button>
        {numbers.map((number, index) => (
          <span key={number} className="flex items-center gap-1">
            {index > 0 && numbers[index - 1] !== number - 1 ? (
              <span aria-hidden className="px-1">
                …
              </span>
            ) : null}
            <Button
              type="button"
              variant={number === page ? "default" : "ghost"}
              size="icon-sm"
              aria-label={`Sayfa ${number}`}
              aria-current={number === page ? "page" : undefined}
              onClick={() => onPageChange(number)}
            >
              {number}
            </Button>
          </span>
        ))}
        <Button
          type="button"
          variant="outline"
          size="icon-sm"
          aria-label="Sonraki sayfa"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          <ChevronRight aria-hidden className="size-4" />
        </Button>
      </div>
    </nav>
  );
}

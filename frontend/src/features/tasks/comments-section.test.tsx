import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import type { PagedComments } from "@/lib/api/types";

const listMock = vi.fn();
const createMock = vi.fn();

vi.mock("@/lib/api/endpoints", () => ({
  commentsApi: {
    list: (...args: unknown[]) => listMock(...args),
    create: (...args: unknown[]) => createMock(...args),
  },
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ replace: vi.fn(), push: vi.fn() }),
  usePathname: () => "/",
  useSearchParams: () => new URLSearchParams(),
}));

import { CommentsSection } from "./comments-section";
import { AuthProvider } from "@/lib/auth/auth-context";
import { TooltipProvider } from "@/components/ui/tooltip";

// Builds a newest-first page (B10-04): the API returns the newest comments on page 1
// and progressively older comments on higher pages.
function page(items: number[], totalCount: number): PagedComments {
  return {
    items: items.map((n) => ({
      id: `c${n}`,
      taskItemId: "task-1",
      content: `comment-${n}`,
      author: { id: "u1", userName: "user1", displayName: "User One" },
      createdAtUtc: `2026-07-01T00:00:${String(n).padStart(2, "0")}+00:00`,
    })),
    page: 1,
    pageSize: 20,
    totalCount,
    totalPages: Math.ceil(totalCount / 20),
  };
}

function renderSection() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <AuthProvider>
        <TooltipProvider>
          <CommentsSection projectId="project-1" taskId="task-1" />
        </TooltipProvider>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

function renderedContents(): string[] {
  const list = screen.getByRole("list");
  return within(list)
    .getAllByRole("listitem")
    .map((li) => li.querySelector("p:last-of-type")?.textContent ?? "");
}

describe("CommentsSection newest-first paging", () => {
  beforeEach(() => {
    listMock.mockReset();
    createMock.mockReset();
  });

  it("renders the first page oldest→newest for 20 comments", async () => {
    // 20 comments, one full page. Newest first from the API: 20..1.
    listMock.mockImplementation((_p: string, _t: string, pageNumber: number) => {
      if (pageNumber === 1) {
        return Promise.resolve(page([20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1], 20));
      }
      return Promise.resolve(page([], 20));
    });

    renderSection();

    await waitFor(() => expect(screen.getByRole("list")).toBeInTheDocument());
    // Chronological in the view: oldest at the top, newest at the bottom.
    expect(renderedContents()).toEqual(
      Array.from({ length: 20 }, (_, i) => `comment-${i + 1}`),
    );
    // No "load older" button: everything fits on page 1.
    expect(screen.queryByRole("button", { name: /daha eski/i })).not.toBeInTheDocument();
  });

  it("stitches older pages without gaps or duplicates for 21 comments", async () => {
    // 21 comments over two pages: page 1 = 21..2 (newest 20), page 2 = [1].
    listMock.mockImplementation((_p: string, _t: string, pageNumber: number) => {
      if (pageNumber === 1) {
        return Promise.resolve(
          page([21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2], 21),
        );
      }
      if (pageNumber === 2) {
        return Promise.resolve(page([1], 21));
      }
      return Promise.resolve(page([], 21));
    });

    renderSection();

    await waitFor(() => expect(screen.getByRole("list")).toBeInTheDocument());
    // Before loading older: newest 20 shown oldest→newest (comment-2 … comment-21).
    expect(renderedContents()).toEqual(
      Array.from({ length: 20 }, (_, i) => `comment-${i + 2}`),
    );

    const olderButton = screen.getByRole("button", { name: /daha eski/i });
    await userEvent.click(olderButton);

    // After loading the older page, the whole thread reads comment-1 … comment-21 with
    // no duplicate and no missing comment.
    await waitFor(() =>
      expect(renderedContents()).toEqual(
        Array.from({ length: 21 }, (_, i) => `comment-${i + 1}`),
      ),
    );
  });
});

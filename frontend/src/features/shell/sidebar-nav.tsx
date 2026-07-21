"use client";

import {
  Check,
  ChevronsUpDown,
  Folder,
  LayoutDashboard,
  ListChecks,
  Shield,
  Users,
} from "lucide-react";
import Link from "next/link";
import { useParams, usePathname } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { projectsApi } from "@/lib/api/endpoints";
import { useAuth } from "@/lib/auth/auth-context";
import { cn } from "@/lib/utils";

// Tasarım Sidebar.dc.html birebir: logo, proje seçici, proje gezinimi,
// SuperAdmin'e özel YÖNETİM bölümü. Diğer roller için yönetim hiç render edilmez.

export const PROJECTS_QUERY_KEY = ["projects"] as const;

function NavLink({
  href,
  active,
  icon: Icon,
  children,
  onNavigate,
}: {
  href: string;
  active: boolean;
  icon: typeof LayoutDashboard;
  children: React.ReactNode;
  onNavigate?: () => void;
}) {
  return (
    <Link
      href={href}
      aria-current={active ? "page" : undefined}
      onClick={onNavigate}
      className={cn(
        // Mobilde dokunma hedefi 44px; masaüstünde tasarımın kompakt yoğunluğu.
        "flex min-h-11 items-center gap-2.5 rounded-md px-2.5 py-2 text-[13px] font-medium transition-colors md:min-h-0",
        active
          ? "bg-accent font-semibold text-accent-foreground"
          : "text-muted-foreground hover:bg-muted hover:text-foreground",
      )}
    >
      <Icon aria-hidden className="size-4 shrink-0" />
      {children}
    </Link>
  );
}

export function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const { hasRole, isAuthenticated } = useAuth();
  const pathname = usePathname();
  const params = useParams<{ projectId?: string }>();
  const projectId = params.projectId;

  const { data: projects } = useQuery({
    queryKey: PROJECTS_QUERY_KEY,
    queryFn: ({ signal }) => projectsApi.list(signal),
    enabled: isAuthenticated,
  });

  const currentProject = projects?.find((project) => project.id === projectId);

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center gap-2 px-4 pb-2.5 pt-4">
        <span className="flex size-[22px] items-center justify-center rounded-md bg-primary">
          <Check aria-hidden className="size-3 text-primary-foreground" strokeWidth={3} />
        </span>
        <span className="text-sm font-bold tracking-tight">TaskManagement</span>
      </div>

      <div className="px-3 pb-1 pt-1.5">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              aria-label="Proje değiştir"
              className="flex min-h-11 w-full items-center justify-between gap-2 rounded-md border bg-background px-2.5 py-2 text-[13px] font-semibold hover:bg-muted md:min-h-0"
            >
              <span className="flex min-w-0 items-center gap-2">
                <Folder aria-hidden className="size-3.5 shrink-0 text-primary" />
                <span className="truncate">
                  {currentProject?.name ?? "Proje seçin"}
                </span>
              </span>
              <ChevronsUpDown aria-hidden className="size-3.5 shrink-0 text-muted-foreground" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-56">
            {(projects ?? []).map((project) => (
              <DropdownMenuItem key={project.id} asChild>
                <Link href={`/projects/${project.id}`} onClick={onNavigate}>
                  <span className="truncate">{project.name}</span>
                  {project.id === projectId ? (
                    <Check aria-hidden className="ml-auto size-3.5" />
                  ) : null}
                </Link>
              </DropdownMenuItem>
            ))}
            <DropdownMenuItem asChild>
              <Link href="/projects" onClick={onNavigate} className="text-muted-foreground">
                Tüm projeler
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <nav aria-label="Proje gezinimi" className="flex flex-col gap-0.5 px-3 py-2">
        {projectId ? (
          <>
            <NavLink
              href={`/projects/${projectId}`}
              active={pathname === `/projects/${projectId}`}
              icon={LayoutDashboard}
              onNavigate={onNavigate}
            >
              Genel Bakış
            </NavLink>
            <NavLink
              href={`/projects/${projectId}/tasks`}
              active={pathname.startsWith(`/projects/${projectId}/tasks`)}
              icon={ListChecks}
              onNavigate={onNavigate}
            >
              Görevler
            </NavLink>
            <NavLink
              href={`/projects/${projectId}/members`}
              active={pathname.startsWith(`/projects/${projectId}/members`)}
              icon={Users}
              onNavigate={onNavigate}
            >
              Üyeler
            </NavLink>
          </>
        ) : (
          <NavLink
            href="/projects"
            active={pathname === "/projects"}
            icon={Folder}
            onNavigate={onNavigate}
          >
            Projeler
          </NavLink>
        )}
      </nav>

      {hasRole("SuperAdmin") ? (
        <div className="flex flex-col gap-1 px-3 pt-2.5">
          <div className="px-2.5 text-[10px] font-bold uppercase tracking-[0.08em] text-muted-foreground/70">
            Yönetim
          </div>
          <nav aria-label="Yönetim">
            <NavLink
              href="/admin/users"
              active={pathname.startsWith("/admin/users")}
              icon={Shield}
              onNavigate={onNavigate}
            >
              Kullanıcılar
            </NavLink>
          </nav>
        </div>
      ) : null}
    </div>
  );
}

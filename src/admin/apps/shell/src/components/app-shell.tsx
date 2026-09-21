import { bootstrapSession, currentSession, logout } from "@raytha/api";
import {
  Avatar,
  cn,
  CommandPalette,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Toaster,
  type CommandItem,
} from "@raytha/ui";
import { useQueryClient } from "@tanstack/react-query";
import { Link, Outlet, useLocation, useNavigate } from "@tanstack/react-router";
import {
  ChevronRight,
  ChevronsUpDown,
  ClipboardList,
  ExternalLink,
  FileText,
  HardDrive,
  KeyRound,
  LayoutDashboard,
  LayoutTemplate,
  List,
  LogOut,
  Mail,
  Mails,
  Menu,
  PanelLeftClose,
  PanelLeftOpen,
  Plus,
  ScrollText,
  Search,
  Settings,
  UserRound,
  Users,
  UsersRound,
  Webhook,
  Image,
  Wrench,
  X,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useDocumentTitle } from "../lib/document-title";

const SIDEBAR_KEY = "raytha.sidebar.collapsed";
const SETTINGS_KEY = "raytha.sidebar.settings";

interface NavItem {
  to: string;
  label: string;
  icon: typeof Settings;
  external?: boolean;
  alsoMatch?: string[];
}

function itemIsActive(item: NavItem, pathname: string): boolean {
  if (item.external) {
    return false;
  }
  const prefixes = [item.to, ...(item.alsoMatch ?? [])];
  return prefixes.some((prefix) => {
    const normalized = prefix.replace(/\/+$/, "") || "/";
    return pathname === normalized || pathname.startsWith(`${normalized}/`);
  });
}

const PRIMARY_NAV: NavItem[] = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/users", label: "Users", icon: Users },
  { to: "/site-pages", label: "Site Pages", icon: FileText },
  { to: "/content-types", label: "Content types", icon: ClipboardList, alsoMatch: ["/content"] },
];

const AFTER_CONTENT_NAV: NavItem[] = [
  { to: "/", label: "Live Website", icon: ExternalLink, external: true },
  // public site root, not the SPA dashboard
  { to: "/themes", label: "Themes", icon: LayoutTemplate },
  { to: "/email-templates", label: "Email Templates", icon: Mails },
  { to: "/menus", label: "Menus", icon: List },
  { to: "/functions", label: "Functions", icon: Wrench },
  { to: "/audit-log", label: "Audit Log", icon: ScrollText },
  { to: "/webhooks", label: "Webhooks", icon: Webhook },
  { to: "/email-log", label: "Email Log", icon: Mail },
  { to: "/media", label: "Media", icon: Image },
];

const SETTINGS_NAV: NavItem[] = [
  { to: "/settings/admins", label: "Admins", icon: UsersRound },
  { to: "/settings/roles", label: "Roles", icon: KeyRound },
  { to: "/settings/configuration", label: "Configuration", icon: Settings },
  { to: "/settings/authentication", label: "Authentication", icon: KeyRound },
  { to: "/maintenance", label: "Maintenance", icon: HardDrive },
];

const ALL_NAV: NavItem[] = [
  ...PRIMARY_NAV,
  { to: "/content-types/new", label: "New Content Type", icon: Plus },
  ...AFTER_CONTENT_NAV,
  ...SETTINGS_NAV,
  { to: "/profile", label: "My Profile", icon: UserRound },
];

function navItemClassName(active: boolean, collapsed: boolean, indented = false) {
  return cn(
    "flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-sidebar-foreground/85 transition-colors hover:bg-sidebar-accent hover:text-white",
    collapsed && "justify-center px-0",
    indented && !collapsed && "pl-9",
    active && "bg-sidebar-active text-white shadow-card hover:bg-sidebar-active",
  );
}

function NavLinkItem({
  item,
  collapsed,
  pathname,
  onNavigate,
  indented = false,
}: {
  item: NavItem;
  collapsed: boolean;
  pathname: string;
  onNavigate?: () => void;
  indented?: boolean;
}) {
  const active = itemIsActive(item, pathname);
  const className = navItemClassName(active, collapsed, indented);
  const content = (
    <>
      <item.icon className="size-4 shrink-0" aria-hidden />
      {!collapsed && <span className="truncate">{item.label}</span>}
    </>
  );

  return (
    <li>
      {item.external ? (
        <a
          href={item.to}
          target="_blank"
          rel="noreferrer"
          onClick={onNavigate}
          title={collapsed ? item.label : undefined}
          className={className}
        >
          {content}
        </a>
      ) : (
        <Link
          to={item.to}
          onClick={onNavigate}
          aria-current={active ? "page" : undefined}
          title={collapsed ? item.label : undefined}
          className={className}
        >
          {content}
        </Link>
      )}
    </li>
  );
}

function BrandMark({ collapsed }: { collapsed: boolean }) {
  return (
    <Link to="/" className="flex items-center gap-2.5 px-1" aria-label="Raytha Admin home">
      <img src="/raytha/white.svg" alt="" className={cn("shrink-0", collapsed ? "h-8" : "h-10")} />
      {!collapsed && <span className="sr-only">Raytha Admin</span>}
    </Link>
  );
}

function SidebarContent({
  collapsed,
  pathname,
  onNavigate,
}: {
  collapsed: boolean;
  pathname: string;
  onNavigate?: () => void;
}) {
  const [settingsOpen, setSettingsOpen] = useState(() => localStorage.getItem(SETTINGS_KEY) !== "0");
  const settingsActive = SETTINGS_NAV.some((item) => itemIsActive(item, pathname));

  return (
    <div className="flex h-full flex-col">
      <div className={cn("flex h-16 items-center border-b border-white/10 px-3", collapsed && "justify-center")}>
        <BrandMark collapsed={collapsed} />
      </div>

      <nav aria-label="Main" className="flex-1 overflow-y-auto px-2 pb-4">
        <ul className="space-y-0.5 pt-3">
          {PRIMARY_NAV.map((item) => (
            <NavLinkItem key={item.to} item={item} collapsed={collapsed} pathname={pathname} onNavigate={onNavigate} />
          ))}
        </ul>

        <div className={cn("pt-4", collapsed && "flex justify-center")}>
          {collapsed ? (
            <Link
              to="/content-types/new"
              onClick={onNavigate}
              title="New Content Type"
              className={navItemClassName(pathname === "/content-types/new", true)}
            >
              <Plus className="size-4" aria-hidden />
            </Link>
          ) : (
            <Link
              to="/content-types/new"
              onClick={onNavigate}
              className="flex items-center justify-center gap-2 rounded-lg bg-white/15 px-3 py-2 text-sm font-medium text-white hover:bg-white/25"
            >
              <Plus className="size-4" aria-hidden />
              New Content Type
            </Link>
          )}
        </div>

        <div className="mx-3 my-4 border-t border-white/15" role="separator" />

        <ul className="space-y-0.5">
          {AFTER_CONTENT_NAV.map((item) => (
            <NavLinkItem key={item.label} item={item} collapsed={collapsed} pathname={pathname} onNavigate={onNavigate} />
          ))}
        </ul>

        <div className="mx-3 my-4 border-t border-white/15" role="separator" />

        {collapsed ? (
          <ul className="space-y-0.5">
            {SETTINGS_NAV.map((item) => (
              <NavLinkItem key={item.to} item={item} collapsed pathname={pathname} onNavigate={onNavigate} />
            ))}
          </ul>
        ) : (
          <div>
            <button
              type="button"
              aria-expanded={settingsOpen}
              aria-controls="nav-settings"
              onClick={() => {
                const next = !settingsOpen;
                setSettingsOpen(next);
                localStorage.setItem(SETTINGS_KEY, next ? "1" : "0");
              }}
              className={cn(
                "flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium transition-colors hover:bg-sidebar-accent hover:text-white",
                settingsActive ? "text-white" : "text-sidebar-foreground/85",
              )}
            >
              <Settings className="size-4 shrink-0" aria-hidden />
              <span className="truncate text-left">Settings</span>
              <ChevronRight className={cn("ml-auto size-4 shrink-0 transition-transform", settingsOpen && "rotate-90")} aria-hidden />
            </button>
            {settingsOpen && (
              <ul id="nav-settings" className="space-y-0.5">
                {SETTINGS_NAV.map((item) => (
                  <NavLinkItem key={item.to} item={item} collapsed={false} pathname={pathname} onNavigate={onNavigate} indented />
                ))}
              </ul>
            )}
          </div>
        )}
      </nav>
    </div>
  );
}

export function AppShell() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const location = useLocation();
  const pathname = location.pathname.replace(/\/+$/, "") || "/";

  const [collapsed, setCollapsed] = useState(() => localStorage.getItem(SIDEBAR_KEY) === "1");
  const [mobileOpen, setMobileOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const mobileDrawerRef = useRef<HTMLDivElement>(null);
  const session = currentSession();

  useEffect(() => {
    localStorage.setItem(SIDEBAR_KEY, collapsed ? "1" : "0");
  }, [collapsed]);

  useEffect(() => {
    const refresh = () => {
      void bootstrapSession().then(() => {
        void queryClient.invalidateQueries({ queryKey: ["me"] });
      });
    };
    const onVisibility = () => {
      if (document.visibilityState === "visible") {
        refresh();
      }
    };
    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("focus", refresh);
    return () => {
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("focus", refresh);
    };
  }, [queryClient]);

  const [prevPathname, setPrevPathname] = useState(pathname);
  if (pathname !== prevPathname) {
    setPrevPathname(pathname);
    setMobileOpen(false);
    setPaletteOpen(false);
  }

  useEffect(() => {
    document.body.style.overflow = "";
    document.getElementById("main")?.focus();
  }, [pathname]);

  useEffect(() => {
    if (!mobileOpen) {
      return;
    }

    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const drawer = mobileDrawerRef.current;
    drawer?.querySelector<HTMLElement>('button[aria-label="Close navigation"]')?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setMobileOpen(false);
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      previouslyFocused?.focus();
    };
  }, [mobileOpen]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setPaletteOpen((open) => !open);
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, []);

  const handleLogout = async () => {
    await logout();
    queryClient.clear();
    window.location.assign("/raytha/login");
  };

  const commands = useMemo<CommandItem[]>(() => {
    const go = (to: string) => () => void navigate({ to });
    return [
      ...ALL_NAV.filter((item) => !item.external).map((item) => ({
        id: item.to,
        label: item.label,
        group: "Navigate",
        icon: <item.icon />,
        onSelect: go(item.to),
      })),
      {
        id: "live-website",
        label: "Live Website",
        group: "Navigate",
        icon: <ExternalLink />,
        onSelect: () => window.open("/", "_blank", "noopener,noreferrer"),
      },
      {
        id: "sign-out",
        label: "Sign out",
        group: "Session",
        icon: <LogOut />,
        onSelect: () => void handleLogout(),
      },
    ];
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [navigate]);

  const breadcrumbs = useMemo(() => {
    const match = ALL_NAV.find((item) => !item.external && itemIsActive(item, pathname));
    if (pathname === "/") {
      return [{ label: "Dashboard" }];
    }
    return [{ label: "Dashboard", to: "/" }, { label: match?.label ?? pathname.replace(/^\//, "") }];
  }, [pathname]);

  useDocumentTitle([breadcrumbs.at(-1)?.label ?? "Dashboard"]);

  return (
    <div className="flex min-h-screen min-w-0 overflow-x-hidden bg-background">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-0 focus:top-0 focus:z-50 focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground"
      >
        Skip to content
      </a>
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-30 hidden bg-sidebar text-sidebar-foreground transition-[width] duration-200 lg:block",
          collapsed ? "w-16" : "w-64",
        )}
      >
        <SidebarContent collapsed={collapsed} pathname={pathname} />
      </aside>

      {mobileOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="absolute inset-0 bg-black/50" onClick={() => setMobileOpen(false)} aria-hidden />
          <div
            ref={mobileDrawerRef}
            id="mobile-navigation"
            role="dialog"
            aria-modal="true"
            aria-label="Navigation"
            className="absolute inset-y-0 left-0 w-72 bg-sidebar text-sidebar-foreground shadow-pop"
          >
            <button
              type="button"
              aria-label="Close navigation"
              onClick={() => setMobileOpen(false)}
              className="absolute right-3 top-4 rounded-lg p-1.5 text-sidebar-muted hover:bg-sidebar-accent hover:text-white"
            >
              <X className="size-5" />
            </button>
            <SidebarContent collapsed={false} pathname={pathname} onNavigate={() => setMobileOpen(false)} />
          </div>
        </div>
      )}

      <div className={cn("flex min-h-screen min-w-0 flex-1 flex-col transition-[margin] duration-200", collapsed ? "lg:ml-16" : "lg:ml-64")}>
        <header className="sticky top-0 z-20 flex h-16 min-w-0 items-center gap-3 border-b border-border bg-card/85 px-4 backdrop-blur-md lg:px-6">
          <button
            type="button"
            aria-label="Open navigation"
            aria-expanded={mobileOpen}
            aria-controls="mobile-navigation"
            onClick={() => setMobileOpen(true)}
            className="rounded-lg p-2 text-muted-foreground hover:bg-accent lg:hidden"
          >
            <Menu className="size-5" />
          </button>
          <button
            type="button"
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
            onClick={() => setCollapsed((c) => !c)}
            className="hidden rounded-lg p-2 text-muted-foreground hover:bg-accent lg:block"
          >
            {collapsed ? <PanelLeftOpen className="size-5" /> : <PanelLeftClose className="size-5" />}
          </button>

          <nav aria-label="Breadcrumb" className="hidden min-w-0 md:block">
            <ol className="flex items-center gap-1.5 text-sm">
              {breadcrumbs.map((crumb, i) => (
                <li key={i} className="flex items-center gap-1.5">
                  {i > 0 && <span className="text-muted-foreground/60">/</span>}
                  {crumb.to && i < breadcrumbs.length - 1 ? (
                    <Link to={crumb.to} className="text-muted-foreground hover:text-foreground">
                      {crumb.label}
                    </Link>
                  ) : (
                    <span className={cn("truncate", i === breadcrumbs.length - 1 ? "font-semibold text-foreground" : "text-muted-foreground")}>
                      {crumb.label}
                    </span>
                  )}
                </li>
              ))}
            </ol>
          </nav>

          <div className="ml-auto flex items-center gap-2">
            <button
              type="button"
              aria-expanded={paletteOpen}
              aria-haspopup="dialog"
              onClick={() => setPaletteOpen(true)}
              className="hidden h-9 w-56 items-center gap-2 rounded-lg border border-input bg-background px-3 text-sm text-muted-foreground shadow-card transition-colors hover:border-brand-300 sm:flex"
            >
              <Search className="size-4" aria-hidden />
              <span className="flex-1 text-left">Search…</span>
              <kbd className="rounded border border-border bg-muted px-1.5 py-0.5 text-[10px] font-medium">⌘K</kbd>
            </button>
            <button
              type="button"
              aria-label="Search"
              aria-expanded={paletteOpen}
              aria-haspopup="dialog"
              onClick={() => setPaletteOpen(true)}
              className="rounded-lg p-2 text-muted-foreground hover:bg-accent sm:hidden"
            >
              <Search className="size-5" />
            </button>

            <DropdownMenu>
              <DropdownMenuTrigger>
                <button
                  type="button"
                  aria-label={`Account menu for ${session?.fullName ?? session?.email ?? "current user"}`}
                  className="flex items-center gap-2 rounded-lg px-2 py-1.5 transition-colors hover:bg-accent"
                >
                  <Avatar name={session?.fullName ?? session?.email ?? "?"} />
                  <span className="hidden max-w-40 truncate text-sm font-medium md:block">
                    {session?.fullName ?? session?.email}
                  </span>
                  <ChevronsUpDown className="hidden size-3.5 text-muted-foreground md:block" aria-hidden />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuLabel>{session?.email}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onSelect={() => void navigate({ to: "/profile" })}>
                  <UserRound />
                  My Profile
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onSelect={() => void handleLogout()}>
                  <LogOut />
                  Sign out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>

        <main id="main" tabIndex={-1} className="mx-auto w-full min-w-0 max-w-6xl flex-1 px-4 py-6 outline-none lg:px-8">
          <Outlet />
        </main>
      </div>

      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} items={commands} />
      <Toaster />
    </div>
  );
}

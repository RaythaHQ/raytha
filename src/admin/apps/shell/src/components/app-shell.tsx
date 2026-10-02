import {
  adminApi,
  bootstrapSession,
  currentSession,
  formatError,
  hasContentTypePermission,
  hasPermission,
  logout,
  platformPermissions,
  type ImpersonationSession,
} from "@raytha/api";
import {
  Avatar,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbSeparator,
  Button,
  cn,
  CommandPalette,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  toast,
  Toaster,
  type CommandItem,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, Outlet, useLocation, useNavigate } from "@tanstack/react-router";
import {
  Activity,
  ArrowUpRight,
  Blocks,
  BookOpen,
  ChevronsUpDown,
  FileText,
  Globe,
  HardDrive,
  Image,
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
  ShieldCheck,
  SquareFunction,
  UserRound,
  Users,
  UsersRound,
  VenetianMask,
  Webhook,
  X,
  type LucideIcon,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useDocumentTitle } from "../lib/document-title";
import { entityFields, readString } from "../pages/entity";
import { AppLink } from "./list-back-link";

const SIDEBAR_KEY = "raytha.sidebar.collapsed";

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  /** Built-in system permission the matching API endpoints require; omitted means any admin. */
  permission?: string;
  alsoMatch?: string[];
  /** Readable content types render nested under this item. */
  nestsContentTypes?: boolean;
}

interface NavGroup {
  label?: string;
  items: NavItem[];
}

/** An allowed item with its resolved children, ready to render. */
interface VisibleNavItem extends NavItem {
  children: NavItem[];
}

interface VisibleNavGroup {
  label?: string;
  items: VisibleNavItem[];
}

const NAV: NavGroup[] = [
  { items: [{ to: "/", label: "Dashboard", icon: LayoutDashboard }] },
  {
    label: "Content",
    items: [
      {
        to: "/content-types",
        label: "Content types",
        icon: Blocks,
        permission: platformPermissions.contentTypes,
        nestsContentTypes: true,
      },
      { to: "/site-pages", label: "Site pages", icon: FileText, permission: platformPermissions.sitePages },
      { to: "/media", label: "Media", icon: Image, permission: platformPermissions.media },
      { to: "/menus", label: "Menus", icon: List, permission: platformPermissions.contentTypes },
    ],
  },
  {
    label: "Design",
    items: [
      { to: "/themes", label: "Themes", icon: LayoutTemplate, permission: platformPermissions.templates },
      { to: "/email-templates", label: "Email templates", icon: Mails, permission: platformPermissions.systemSettings },
    ],
  },
  {
    label: "Automation",
    items: [
      { to: "/functions", label: "Functions", icon: SquareFunction, permission: platformPermissions.systemSettings },
      { to: "/webhooks", label: "Webhooks", icon: Webhook, permission: platformPermissions.systemSettings },
    ],
  },
  {
    label: "People",
    items: [
      { to: "/users", label: "Users", icon: Users, permission: platformPermissions.users },
      { to: "/settings/admins", label: "Admins", icon: UsersRound, permission: platformPermissions.admins },
      { to: "/settings/roles", label: "Roles", icon: ShieldCheck, permission: platformPermissions.admins },
    ],
  },
  {
    label: "Observability",
    items: [
      { to: "/audit-log", label: "Audit log", icon: ScrollText, permission: platformPermissions.auditLogs },
      { to: "/email-log", label: "Email log", icon: Mail, permission: platformPermissions.systemSettings },
    ],
  },
  {
    label: "Settings",
    items: [
      { to: "/settings/configuration", label: "Configuration", icon: Settings, permission: platformPermissions.systemSettings },
      { to: "/settings/authentication", label: "Authentication", icon: KeyRound, permission: platformPermissions.systemSettings },
      { to: "/maintenance", label: "Maintenance", icon: HardDrive, permission: platformPermissions.systemSettings },
      { to: "/background-tasks", label: "Background tasks", icon: Activity, permission: platformPermissions.systemSettings },
    ],
  },
];

const NEW_CONTENT_TYPE: NavItem = {
  to: "/content-types/new",
  label: "New content type",
  icon: Plus,
  permission: platformPermissions.contentTypes,
};

const PROFILE: NavItem = { to: "/profile", label: "My profile", icon: UserRound };

const ALL_NAV: NavItem[] = [...NAV.flatMap((group) => group.items), NEW_CONTENT_TYPE, PROFILE];

function allowed(item: NavItem): boolean {
  return item.permission === undefined || hasPermission(item.permission);
}

/**
 * Drops items the session cannot open and groups left empty. Content types stay
 * reachable without the manage permission: they move up into the group instead.
 */
function visibleNav(contentTypes: NavItem[]): VisibleNavGroup[] {
  return NAV.map((group) => ({
    label: group.label,
    items: group.items.flatMap((item): VisibleNavItem[] => {
      const children = item.nestsContentTypes ? contentTypes : [];
      if (allowed(item)) {
        return [{ ...item, children }];
      }
      return children.map((child) => ({ ...child, children: [] }));
    }),
  })).filter((group) => group.items.length > 0);
}

function itemIsActive(item: NavItem, pathname: string): boolean {
  const prefixes = [item.to, ...(item.alsoMatch ?? [])];
  return prefixes.some((prefix) => {
    const normalized = prefix.replace(/\/+$/, "") || "/";
    if (normalized === "/") {
      return pathname === "/";
    }
    return pathname === normalized || pathname.startsWith(`${normalized}/`);
  });
}

/** Readable content types, as sidebar entries pointing at each type's items workspace. */
function useContentTypeNav(): NavItem[] {
  const query = useQuery({
    queryKey: ["content-types", "nav"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 1000 }),
  });
  return useMemo(() => {
    const items: NavItem[] = [];
    for (const entity of query.data?.items ?? []) {
      const fields = entityFields(entity);
      const developerName = readString(fields, "developerName");
      if (!developerName || !hasContentTypePermission(developerName, "read")) {
        continue;
      }
      items.push({
        to: `/content/${developerName}`,
        label: readString(fields, "labelPlural", "labelSingular") || developerName,
        icon: FileText,
        alsoMatch: [`/content-types/${developerName}`],
      });
    }
    return items;
  }, [query.data]);
}

const SEGMENT_LABELS: Record<string, string> = {
  new: "New",
  "web-templates": "Web templates",
  "widget-templates": "Widget templates",
  assets: "Assets",
  import: "Import",
  layout: "Layout",
  widgets: "Widgets",
  fields: "Fields",
  configuration: "Configuration",
  trash: "Trash",
  views: "Views",
  items: "Items",
  groups: "User groups",
  authentication: "Authentication",
  admins: "Admins",
  roles: "Roles",
  smtp: "SMTP",
};

function buildBreadcrumbs(pathname: string, contentTypes: NavItem[]): { label: string; to?: string }[] {
  if (pathname === "/") {
    return [{ label: "Dashboard" }];
  }
  // A content type owns both /content/{dev} and /content-types/{dev} paths.
  for (const type of contentTypes) {
    for (const prefix of [type.to, ...(type.alsoMatch ?? [])]) {
      if (pathname !== prefix && !pathname.startsWith(`${prefix}/`)) {
        continue;
      }
      const rest = pathname.slice(prefix.length).split("/").filter(Boolean);
      // /content/{dev}/{viewId} is the items workspace; the view label is the
      // page title, so the crumb is just the type.
      if (rest.length === 1 && !SEGMENT_LABELS[rest[0]]) {
        return [{ label: type.label }];
      }
      return walkSegments({ label: type.label, to: type.to }, prefix, pathname);
    }
  }
  const match = ALL_NAV.filter((item) => item.to !== "/" && itemIsActive(item, pathname)).sort(
    (a, b) => b.to.length - a.to.length,
  )[0];
  if (!match) {
    return [{ label: "Admin" }];
  }
  return walkSegments({ label: match.label, to: match.to }, match.to, pathname);
}

function walkSegments(
  base: { label: string; to: string },
  basePath: string,
  pathname: string,
): { label: string; to?: string }[] {
  const rest = pathname.slice(basePath.length).split("/").filter(Boolean);
  const crumbs: { label: string; to?: string }[] = [{ label: base.label, to: base.to }];
  let path = basePath;
  let skippedId = false;
  for (const segment of rest) {
    path += `/${segment}`;
    const label = SEGMENT_LABELS[segment];
    if (!label) {
      skippedId = true;
      continue;
    }
    crumbs.push({ label, to: path });
    skippedId = false;
  }
  if (skippedId) {
    crumbs.push({ label: "Details" });
  }
  if (crumbs.length === 1 && rest.length === 0) {
    return [{ label: base.label }];
  }
  return crumbs.map((crumb, index) => (index === crumbs.length - 1 ? { label: crumb.label } : crumb));
}

// The router stamps aria-current="page" on every prefix match; the sidebar decides
// "current" itself, so the router may only agree on exact matches.
const EXACT_ONLY = { exact: true, includeSearch: false } as const;

function navItemClassName(active: boolean, collapsed: boolean) {
  return cn(
    "group flex h-7 items-center gap-2.5 rounded-md px-2 text-[13px] font-medium outline-none transition-colors focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring",
    collapsed && "mx-auto size-8 justify-center px-0",
    active
      ? "bg-sidebar-active text-foreground shadow-xs ring-1 ring-black/[0.06]"
      : "text-sidebar-foreground hover:bg-sidebar-accent hover:text-foreground",
  );
}

function iconClassName(active: boolean) {
  return cn(
    "size-4 shrink-0 transition-colors",
    active ? "text-brand-600" : "text-sidebar-muted group-hover:text-foreground",
  );
}

function ContentTypeTile({ label, active }: { label: string; active: boolean }) {
  return (
    <span
      aria-hidden
      className={cn(
        "flex size-5 shrink-0 items-center justify-center rounded-md text-[10px] font-semibold uppercase",
        active ? "bg-brand-500 text-white" : "bg-[#e4e4ea] text-sidebar-foreground group-hover:bg-[#dcdce3]",
      )}
    >
      {label.charAt(0)}
    </span>
  );
}

function NavLinkItem({
  item,
  collapsed,
  pathname,
  onNavigate,
}: {
  item: VisibleNavItem;
  collapsed: boolean;
  pathname: string;
  onNavigate?: () => void;
}) {
  const childActive = item.children.some((child) => itemIsActive(child, pathname));
  const active = !childActive && itemIsActive(item, pathname);
  const isContentType = item.to.startsWith("/content/");

  return (
    <li>
      <Link
        to={item.to}
        activeOptions={EXACT_ONLY}
        onClick={onNavigate}
        aria-current={active ? "page" : undefined}
        title={collapsed ? item.label : undefined}
        className={navItemClassName(active, collapsed)}
      >
        {isContentType ? (
          <ContentTypeTile label={item.label} active={active} />
        ) : (
          <item.icon className={iconClassName(active)} aria-hidden />
        )}
        {collapsed ? <span className="sr-only">{item.label}</span> : <span className="truncate">{item.label}</span>}
      </Link>
      {item.children.length > 0 && (
        <ul className={cn("mt-px space-y-px", collapsed ? "" : "ml-[15px] border-l border-sidebar-border pl-2")}>
          {item.children.map((child) => {
            const active = itemIsActive(child, pathname);
            return (
              <li key={child.to}>
                <Link
                  to={child.to}
                  activeOptions={EXACT_ONLY}
                  onClick={onNavigate}
                  aria-current={active ? "page" : undefined}
                  title={collapsed ? child.label : undefined}
                  className={cn(navItemClassName(active, collapsed), !collapsed && "font-normal", active && "font-medium")}
                >
                  {collapsed ? (
                    <>
                      <ContentTypeTile label={child.label} active={active} />
                      <span className="sr-only">{child.label}</span>
                    </>
                  ) : (
                    <span className="truncate">{child.label}</span>
                  )}
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </li>
  );
}

function BrandMark({ collapsed }: { collapsed: boolean }) {
  return (
    <AppLink
      href="/raytha"
      className="flex items-center rounded-md outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
    >
      <span className="sr-only">Raytha Admin home</span>
      {collapsed ? (
        <span className="block size-7 overflow-hidden rounded-md">
          <img src="/raytha/color-no-background.svg" alt="" className="-mt-[2.5px] -ml-[2.5px] h-[33px] max-w-none" />
        </span>
      ) : (
        <img src="/raytha/color-no-background.svg" alt="" className="h-8 shrink-0" />
      )}
    </AppLink>
  );
}

/** Scalar reference for this instance, including a configured path base. */
function apiReferenceHref(): string {
  const pathBase = (currentSession()?.organization.pathBase ?? window.location.pathname.split("/raytha")[0] ?? "").replace(
    /\/+$/,
    "",
  );
  return `${pathBase}/raytha/api`;
}

function SidebarContent({
  collapsed,
  pathname,
  contentTypes,
  onNavigate,
}: {
  collapsed: boolean;
  pathname: string;
  contentTypes: NavItem[];
  onNavigate?: () => void;
}) {
  const groups = visibleNav(contentTypes);
  const canCreateType = allowed(NEW_CONTENT_TYPE);
  const navRef = useRef<HTMLElement>(null);

  useEffect(() => {
    navRef.current?.querySelector('[aria-current="page"]')?.scrollIntoView({ block: "nearest" });
  }, [pathname]);

  return (
    <div className="flex h-full flex-col">
      <div className={cn("flex h-14 shrink-0 items-center px-4", collapsed && "justify-center px-0")}>
        <BrandMark collapsed={collapsed} />
      </div>

      <nav
        ref={navRef}
        aria-label="Main"
        className={cn("sidebar-scroll flex-1 overflow-y-auto pb-6", collapsed ? "px-2" : "px-3")}
      >
        {groups.map((group) => (
          <div key={group.label ?? "top"}>
            {group.label &&
              (collapsed ? (
                <div className="mx-2 my-2.5 border-t border-sidebar-border" role="separator" />
              ) : (
                <div className="flex h-7 items-end justify-between pr-1 pb-1 pl-2">
                  <h2 className="text-[11px] font-medium uppercase tracking-[0.06em] text-sidebar-muted">{group.label}</h2>
                  {group.label === "Content" && canCreateType && (
                    <Link
                      to={NEW_CONTENT_TYPE.to}
                      onClick={onNavigate}
                      aria-label="New content type"
                      title="New content type"
                      className="flex size-5 items-center justify-center rounded-md text-sidebar-muted transition-colors hover:bg-sidebar-accent hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                    >
                      <Plus className="size-3.5" aria-hidden />
                    </Link>
                  )}
                </div>
              ))}
            <ul className="space-y-px">
              {group.items.map((item) => (
                <NavLinkItem key={item.to} item={item} collapsed={collapsed} pathname={pathname} onNavigate={onNavigate} />
              ))}
            </ul>
          </div>
        ))}
      </nav>

      <div className={cn("shrink-0 border-t border-sidebar-border", collapsed ? "px-2 py-2" : "px-3 py-2")}>
        <a
          href={apiReferenceHref()}
          target="_blank"
          rel="noreferrer"
          title={collapsed ? "API reference" : undefined}
          className={navItemClassName(false, collapsed)}
        >
          <BookOpen className={iconClassName(false)} aria-hidden />
          {collapsed ? (
            <span className="sr-only">API reference</span>
          ) : (
            <>
              <span className="truncate">API reference</span>
              <ArrowUpRight className="ml-auto size-3.5 shrink-0 text-sidebar-muted" aria-hidden />
            </>
          )}
        </a>
      </div>
    </div>
  );
}

const isMac = typeof navigator !== "undefined" && /Mac|iPhone|iPad/.test(navigator.userAgent);

function ImpersonationStrip({
  name,
  email,
  impersonation,
}: {
  name: string;
  email: string;
  impersonation: ImpersonationSession;
}) {
  const stop = useMutation({
    mutationFn: () => adminApi.impersonation.stop(),
    onSuccess: ({ redirectUrl }) => window.location.assign(redirectUrl),
    onError: (error) => toast.error(formatError(error)),
  });
  const endsAt = new Date(impersonation.expiresAt);

  return (
    <div
      role="status"
      className="flex min-w-0 flex-wrap items-center gap-x-3 gap-y-2 border-b border-warning-border bg-warning-soft px-3 py-2 lg:px-5"
    >
      <VenetianMask aria-hidden className="size-4 shrink-0 text-warning" />
      <p className="min-w-0 flex-1 text-[13px] leading-5 text-foreground">
        You are signed in as <strong className="font-semibold">{name}</strong>{" "}
        <span className="text-muted-foreground">({email})</span> · started by {impersonation.impersonatorName}
        {Number.isNaN(endsAt.getTime())
          ? null
          : ` · ends at ${endsAt.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })}`}
      </p>
      <Button
        type="button"
        size="sm"
        variant="outline"
        loading={stop.isPending || stop.isSuccess}
        onClick={() => stop.mutate()}
      >
        Stop impersonating
      </Button>
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
  const contentTypes = useContentTypeNav();

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

  const go = (to: string) => () => void navigate({ to });
  const navCommands: CommandItem[] = visibleNav(contentTypes).flatMap((group) =>
    group.items.flatMap((item) => [item, ...item.children]).map((item) => ({
      id: item.to,
      label: item.label,
      group: "Navigate",
      hint: group.label,
      icon: <item.icon />,
      onSelect: go(item.to),
    })),
  );
  const commands: CommandItem[] = [
    ...navCommands,
    ...[NEW_CONTENT_TYPE, PROFILE].filter(allowed).map((item) => ({
      id: item.to,
      label: item.label,
      group: "Actions",
      icon: <item.icon />,
      onSelect: go(item.to),
    })),
    {
      id: "live-website",
      label: "View live site",
      group: "Actions",
      icon: <ArrowUpRight />,
      onSelect: () => window.open("/", "_blank", "noopener,noreferrer"),
    },
    {
      id: "api-reference",
      label: "API reference",
      group: "Actions",
      icon: <BookOpen />,
      onSelect: () => window.open(apiReferenceHref(), "_blank", "noopener,noreferrer"),
    },
    {
      id: "sign-out",
      label: "Sign out",
      group: "Session",
      icon: <LogOut />,
      onSelect: () => void handleLogout(),
    },
  ];

  const breadcrumbs = useMemo(() => buildBreadcrumbs(pathname, contentTypes), [pathname, contentTypes]);

  useDocumentTitle([breadcrumbs.at(-1)?.label ?? "Dashboard"]);

  const displayName = session?.fullName || session?.email || "?";

  return (
    <div className="flex min-h-screen min-w-0 bg-background">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded-lg focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground"
      >
        Skip to content
      </a>
      <aside
        className={cn(
          "hidden shrink-0 border-r border-sidebar-border bg-sidebar transition-[width] duration-200 lg:block",
          collapsed ? "w-15" : "w-60",
        )}
      >
        <div className="sticky top-0 h-screen">
          <SidebarContent collapsed={collapsed} pathname={pathname} contentTypes={contentTypes} />
        </div>
      </aside>

      {mobileOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="absolute inset-0 animate-fade bg-zinc-950/40 backdrop-blur-[2px]" onClick={() => setMobileOpen(false)} aria-hidden />
          <div
            ref={mobileDrawerRef}
            id="mobile-navigation"
            role="dialog"
            aria-modal="true"
            aria-label="Navigation"
            className="absolute inset-y-0 left-0 w-72 border-r border-sidebar-border bg-sidebar shadow-pop"
          >
            <button
              type="button"
              aria-label="Close navigation"
              onClick={() => setMobileOpen(false)}
              className="absolute right-3 top-3 rounded-lg p-1.5 text-sidebar-muted hover:bg-sidebar-accent hover:text-foreground"
            >
              <X className="size-5" />
            </button>
            <SidebarContent
              collapsed={false}
              pathname={pathname}
              contentTypes={contentTypes}
              onNavigate={() => setMobileOpen(false)}
            />
          </div>
        </div>
      )}

      <div className="flex min-h-screen min-w-0 flex-1 flex-col">
        <div className="sticky top-0 z-20">
          {session?.impersonation && (
            <ImpersonationStrip
              name={session.fullName || session.email}
              email={session.email}
              impersonation={session.impersonation}
            />
          )}
          <header className="flex h-14 min-w-0 items-center gap-2 border-b border-border bg-background/80 px-3 backdrop-blur-xl lg:px-5">
            <button
              type="button"
              aria-label="Open navigation"
              aria-expanded={mobileOpen}
              aria-controls="mobile-navigation"
              onClick={() => setMobileOpen(true)}
              className="rounded-lg p-2 text-muted-foreground hover:bg-accent hover:text-foreground lg:hidden"
            >
              <Menu className="size-4.5" />
            </button>
            <button
              type="button"
              aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
              title={collapsed ? "Expand sidebar" : "Collapse sidebar"}
              onClick={() => setCollapsed((c) => !c)}
              className="hidden rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground lg:block"
            >
              {collapsed ? <PanelLeftOpen className="size-4.5" /> : <PanelLeftClose className="size-4.5" />}
            </button>
            <span className="mx-1 hidden h-5 w-px bg-border md:block" aria-hidden />

            <Breadcrumb className="hidden min-w-0 md:block">
              {breadcrumbs.map((crumb, i) => {
                const last = i === breadcrumbs.length - 1;
                return [
                  i > 0 ? <BreadcrumbSeparator key={`sep-${i}`} /> : null,
                  <BreadcrumbItem key={i} current={last}>
                    {crumb.to && !last ? (
                      <AppLink href={crumb.to === "/" ? "/raytha" : `/raytha${crumb.to}`} className="transition-colors">
                        {crumb.label}
                      </AppLink>
                    ) : (
                      crumb.label
                    )}
                  </BreadcrumbItem>,
                ];
              })}
            </Breadcrumb>

            <div className="ml-auto flex items-center gap-1.5">
              <button
                type="button"
                aria-expanded={paletteOpen}
                aria-haspopup="dialog"
                onClick={() => setPaletteOpen(true)}
                className="hidden h-8 w-64 items-center gap-2 rounded-lg border border-border bg-card px-2.5 text-[13px] text-muted-foreground shadow-xs transition-colors hover:border-border-strong hover:text-foreground sm:flex"
              >
                <Search className="size-3.5" aria-hidden />
                <span className="flex-1 text-left">Search or jump to…</span>
                <kbd className="rounded-md border border-border bg-muted px-1.5 py-px font-sans text-[10px] font-medium text-muted-foreground">
                  {isMac ? "⌘K" : "Ctrl K"}
                </kbd>
              </button>
              <button
                type="button"
                aria-label="Search"
                aria-expanded={paletteOpen}
                aria-haspopup="dialog"
                onClick={() => setPaletteOpen(true)}
                className="rounded-lg p-2 text-muted-foreground hover:bg-accent hover:text-foreground sm:hidden"
              >
                <Search className="size-4.5" />
              </button>
              <a
                href="/"
                target="_blank"
                rel="noreferrer"
                aria-label="View live site"
                title="View live site"
                className="rounded-lg p-2 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
              >
                <Globe className="size-4.5" aria-hidden />
              </a>

              <DropdownMenu>
                <DropdownMenuTrigger>
                  <button
                    type="button"
                    aria-label={`Account menu for ${displayName}`}
                    className="ml-1 flex items-center gap-2 rounded-lg py-1 pr-1.5 pl-1 transition-colors hover:bg-accent focus-visible:outline-2 focus-visible:outline-ring"
                  >
                    <Avatar name={displayName} className="size-7" />
                    <span className="hidden max-w-36 truncate text-[13px] font-medium md:block">{displayName}</span>
                    <ChevronsUpDown className="hidden size-3.5 text-muted-foreground md:block" aria-hidden />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="min-w-60">
                  <div className="flex items-center gap-2.5 px-2.5 py-2">
                    <Avatar name={displayName} />
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium text-foreground">{session?.fullName || "Signed in"}</p>
                      <p className="truncate text-xs text-muted-foreground">{session?.email}</p>
                    </div>
                  </div>
                  <DropdownMenuSeparator />
                  <DropdownMenuLabel>Account</DropdownMenuLabel>
                  <DropdownMenuItem onSelect={() => void navigate({ to: "/profile" })}>
                    <UserRound />
                    My profile
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
        </div>

        <main id="main" tabIndex={-1} className="mx-auto w-full min-w-0 max-w-6xl flex-1 px-4 pt-7 pb-16 outline-none sm:px-6 lg:px-10">
          <Outlet />
        </main>
      </div>

      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} items={commands} />
      <Toaster />
    </div>
  );
}

import { adminApi, currentSession, hasContentTypePermission, hasPermission, platformPermissions } from "@raytha/api";
import type { EntityRef, JsonObject } from "@raytha/api";
import { buttonVariants, Card, EmptyState, PageHeader, QueryGate, Skeleton } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import {
  Activity,
  ArrowRight,
  Blocks,
  ChevronRight,
  Database,
  FileText,
  HardDrive,
  Image,
  LayoutTemplate,
  LogIn,
  Mail,
  Settings,
  ShieldCheck,
  SquareFunction,
  UserPlus,
  Users,
  Webhook,
  type LucideIcon,
} from "lucide-react";
import type { ReactNode } from "react";
import { entityFields, humanizeAuditCategory, isRecord, jsonBoolean, jsonNumber, jsonString, readString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function DashboardPage() {
  useDocumentTitle(["Dashboard"]);
  const query = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => adminApi.dashboard(),
  });
  const session = currentSession();
  const firstName = session?.fullName?.split(" ")[0];

  return (
    <div className="space-y-6">
      <PageHeader
        title={firstName ? `${greeting()}, ${firstName}` : greeting()}
        description="Here is what is happening across your site."
      />
      <QueryGate query={query}>{(data) => <Dashboard data={data} />}</QueryGate>
    </div>
  );
}

function Dashboard({ data }: { data: JsonObject }) {
  const contentTypes = useQuery({
    queryKey: ["content-types", "nav"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 1000 }),
  });
  const typeCount = contentTypes.data?.totalCount;
  const canSeeActivity = hasPermission(platformPermissions.auditLogs);

  const fileStorage = isRecord(data.fileStorage) ? data.fileStorage : {};
  const database = isRecord(data.database) ? data.database : {};
  const usedBytes = jsonNumber(fileStorage, "usedBytes") ?? 0;
  const filePercent = jsonNumber(fileStorage, "percentUsed") ?? 0;
  const maxGb = jsonNumber(fileStorage, "maxGb") ?? 0;
  const dbPercent = jsonNumber(database, "percentUsed") ?? 0;
  const usedMb = jsonNumber(database, "usedMb") ?? 0;
  const maxMb = jsonNumber(database, "maxMb") ?? 0;
  const provider = jsonString(fileStorage, "providerName") || "storage";

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Content items"
          icon={FileText}
          value={formatCount(jsonNumber(data, "totalContentItems"))}
          footnote={typeCount === undefined ? "Across your content types" : `Across ${typeCount} content ${typeCount === 1 ? "type" : "types"}`}
        />
        <StatCard
          label="Users"
          icon={Users}
          value={formatCount(jsonNumber(data, "totalUsers"))}
          footnote="Accounts on the public site"
        />
        <StatCard
          label="File storage"
          icon={HardDrive}
          value={formatBytes(usedBytes)}
          footnote={`${formatPercent(filePercent)} of ${formatGigabytes(maxGb)} · ${provider}`}
          progress={filePercent}
        />
        <StatCard
          label="Database"
          icon={Database}
          value={formatMegabytes(usedMb)}
          footnote={`${formatPercent(dbPercent)} of ${formatMegabytes(maxMb)}`}
          progress={dbPercent}
        />
      </div>

      <div className="grid items-start gap-6 lg:grid-cols-5">
        <div className="space-y-6 lg:col-span-3">
          {canSeeActivity ? <RecentActivity /> : null}
          <ContentOverview query={contentTypes} />
        </div>
        <div className="space-y-6 lg:col-span-2">
          <QuickActions />
          <UploadLimits fileStorage={fileStorage} />
        </div>
      </div>
    </div>
  );
}

function StatCard({
  label,
  icon: Icon,
  value,
  footnote,
  progress,
}: {
  label: string;
  icon: LucideIcon;
  value: string;
  footnote: string;
  progress?: number;
}) {
  return (
    <Card className="flex flex-col gap-3 p-5">
      <div className="flex items-center justify-between gap-3">
        <p className="text-[13px] font-medium text-muted-foreground">{label}</p>
        <span className="flex size-7 items-center justify-center rounded-lg border border-border bg-muted/60 text-muted-foreground">
          <Icon className="size-3.5" aria-hidden />
        </span>
      </div>
      <p className="font-display text-[28px] font-semibold leading-none tracking-[-0.02em] tabular-nums">{value}</p>
      {progress !== undefined ? (
        <div
          className="h-1.5 overflow-hidden rounded-full bg-muted"
          role="progressbar"
          aria-valuenow={Math.round(progress)}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`${label} usage`}
        >
          <div
            className="h-full min-w-1.5 rounded-full bg-gradient-to-r from-brand-400 to-brand-600"
            style={{ width: `${Math.min(100, Math.max(0, progress))}%` }}
          />
        </div>
      ) : null}
      <p className="text-xs text-muted-foreground">{footnote}</p>
    </Card>
  );
}

function Panel({
  title,
  description,
  action,
  children,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
}) {
  return (
    <Card className="overflow-hidden">
      <div className="flex items-start justify-between gap-3 border-b border-border px-5 py-4">
        <div className="min-w-0">
          <h2 className="font-display text-[15px] font-semibold leading-6 tracking-tight">{title}</h2>
          {description ? <p className="text-[13px] text-muted-foreground">{description}</p> : null}
        </div>
        {action}
      </div>
      {children}
    </Card>
  );
}

function PanelLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link
      to={to}
      className="inline-flex shrink-0 items-center gap-1 rounded-md py-1 text-[13px] font-medium text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
    >
      {children}
      <ArrowRight className="size-3.5" aria-hidden />
    </Link>
  );
}

const ACTIVITY_ICONS: Record<string, LucideIcon> = {
  ContentItems: FileText,
  ContentTypes: Blocks,
  Users: Users,
  UserGroups: Users,
  Admins: ShieldCheck,
  Roles: ShieldCheck,
  Login: LogIn,
  Themes: LayoutTemplate,
  WebTemplates: LayoutTemplate,
  EmailTemplates: Mail,
  MediaItems: Image,
  Webhooks: Webhook,
  RaythaFunctions: SquareFunction,
  OrganizationSettings: Settings,
};

function RecentActivity() {
  const query = useQuery({
    queryKey: ["audit-logs", "dashboard"],
    queryFn: () => adminApi.auditLogs.list({ pageSize: 7, orderBy: "CreationTime desc" }),
  });

  return (
    <Panel title="Recent activity" description="The latest changes recorded in the audit log." action={<PanelLink to="/audit-log">Audit log</PanelLink>}>
      {query.isPending ? (
        <div className="space-y-4 p-5" aria-busy="true">
          {[0, 1, 2, 3].map((row) => (
            <div key={row} className="flex items-center gap-3">
              <Skeleton className="size-8 rounded-full" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-3.5 w-1/2" />
                <Skeleton className="h-3 w-1/3" />
              </div>
            </div>
          ))}
        </div>
      ) : query.isError || (query.data?.items.length ?? 0) === 0 ? (
        <EmptyState
          icon={Activity}
          title={query.isError ? "Activity is unavailable" : "No activity yet"}
          hint={query.isError ? "The audit log could not be loaded." : "Changes made in the admin will show up here."}
          className="rounded-none border-0"
        />
      ) : (
        <ol className="divide-y divide-border">
          {query.data?.items.map((item) => <ActivityRow key={item.id} item={item} />)}
        </ol>
      )}
    </Panel>
  );
}

function ActivityRow({ item }: { item: EntityRef }) {
  const fields = entityFields(item);
  const category = readString(fields, "category");
  const area = category.split(".")[0] ?? "";
  const Icon = ACTIVITY_ICONS[area] ?? Activity;
  const who = readString(fields, "userEmail") || "System";
  const when = readString(fields, "creationTime");

  return (
    <li className="flex items-center gap-3 px-5 py-3">
      <span className="flex size-8 shrink-0 items-center justify-center rounded-full border border-border bg-muted/60 text-muted-foreground">
        <Icon className="size-3.5" aria-hidden />
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate text-[13px] font-medium text-foreground">{humanizeAuditCategory(category) || "Change"}</p>
        <p className="truncate text-xs text-muted-foreground">
          {who}
          {area ? ` · ${humanizeArea(area)}` : ""}
        </p>
      </div>
      {when ? (
        <time dateTime={when} title={new Date(when).toLocaleString()} className="shrink-0 whitespace-nowrap text-xs tabular-nums text-muted-foreground">
          {relativeTime(when)}
        </time>
      ) : null}
    </li>
  );
}

function ContentOverview({ query }: { query: { data?: { items: EntityRef[] }; isPending: boolean } }) {
  const types = (query.data?.items ?? [])
    .map((entity) => {
      const fields = entityFields(entity);
      return {
        developerName: readString(fields, "developerName"),
        label: readString(fields, "labelPlural", "labelSingular"),
        description: readString(fields, "description"),
      };
    })
    .filter((type) => type.developerName && hasContentTypePermission(type.developerName, "read"));

  return (
    <Panel
      title="Content"
      description="Jump into the content types you edit most."
      action={hasPermission(platformPermissions.contentTypes) ? <PanelLink to="/content-types">All types</PanelLink> : undefined}
    >
      {query.isPending ? (
        <div className="grid gap-3 p-5 sm:grid-cols-2">
          <Skeleton className="h-14" />
          <Skeleton className="h-14" />
        </div>
      ) : types.length === 0 ? (
        <EmptyState
          icon={Blocks}
          title="No content types yet"
          hint="Content types define the structure of what you publish."
          className="rounded-none border-0"
          action={
            hasPermission(platformPermissions.contentTypes) ? (
              <Link to="/content-types/new" className={buttonVariants({ size: "sm" })}>
                New content type
              </Link>
            ) : undefined
          }
        />
      ) : (
        <ul className="grid gap-2 p-3 sm:grid-cols-2">
          {types.map((type) => (
            <li key={type.developerName}>
              <Link
                to="/content/$developerName"
                params={{ developerName: type.developerName }}
                className="group flex items-center gap-3 rounded-lg px-2.5 py-2.5 transition-colors hover:bg-muted/70 focus-visible:outline-2 focus-visible:outline-ring"
              >
                <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-[13px] font-semibold uppercase text-brand-700 ring-1 ring-inset ring-brand-100">
                  {(type.label || type.developerName).charAt(0)}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-[13px] font-medium capitalize text-foreground">{type.label || type.developerName}</span>
                  <span className="block truncate font-mono text-xs text-muted-foreground">{type.developerName}</span>
                </span>
                <ChevronRight className="size-4 shrink-0 text-muted-foreground/50 transition-transform group-hover:translate-x-0.5 group-hover:text-muted-foreground" aria-hidden />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </Panel>
  );
}

interface QuickAction {
  to: string;
  title: string;
  hint: string;
  icon: LucideIcon;
  permission: string;
}

const QUICK_ACTIONS: QuickAction[] = [
  { to: "/site-pages/new", title: "Create a site page", hint: "Compose a page from widgets", icon: FileText, permission: platformPermissions.sitePages },
  { to: "/media", title: "Upload media", hint: "Images, documents, and files", icon: Image, permission: platformPermissions.media },
  { to: "/content-types/new", title: "Add a content type", hint: "Model new structured content", icon: Blocks, permission: platformPermissions.contentTypes },
  { to: "/users/new", title: "Add a user", hint: "Give someone a site account", icon: UserPlus, permission: platformPermissions.users },
  { to: "/themes", title: "Customize the theme", hint: "Edit templates and layouts", icon: LayoutTemplate, permission: platformPermissions.templates },
];

function QuickActions() {
  const actions = QUICK_ACTIONS.filter((action) => hasPermission(action.permission));
  if (actions.length === 0) {
    return null;
  }
  return (
    <Panel title="Quick actions">
      <ul className="p-2">
        {actions.map((action) => (
          <li key={action.to}>
            <Link
              to={action.to}
              className="group flex items-center gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-muted/70 focus-visible:outline-2 focus-visible:outline-ring"
            >
              <span className="flex size-8 shrink-0 items-center justify-center rounded-lg border border-border bg-card text-brand-600 shadow-xs">
                <action.icon className="size-4" aria-hidden />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-[13px] font-medium text-foreground">{action.title}</span>
                <span className="block truncate text-xs text-muted-foreground">{action.hint}</span>
              </span>
              <ChevronRight className="size-4 shrink-0 text-muted-foreground/50 transition-transform group-hover:translate-x-0.5 group-hover:text-muted-foreground" aria-hidden />
            </Link>
          </li>
        ))}
      </ul>
    </Panel>
  );
}

function UploadLimits({ fileStorage }: { fileStorage: JsonObject }) {
  const maxFileBytes = jsonNumber(fileStorage, "maxFileSizeBytes");
  const mimeTypes = jsonString(fileStorage, "allowedMimeTypes")
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean);

  return (
    <Panel title="Uploads">
      <dl className="divide-y divide-border text-[13px]">
        <div className="flex items-center justify-between gap-3 px-5 py-3">
          <dt className="text-muted-foreground">Largest file</dt>
          <dd className="font-medium tabular-nums">{maxFileBytes === undefined ? "—" : formatBytes(maxFileBytes)}</dd>
        </div>
        <div className="flex items-center justify-between gap-3 px-5 py-3">
          <dt className="text-muted-foreground">Upload path</dt>
          <dd className="font-medium">{jsonBoolean(fileStorage, "useDirectUploadToCloud") ? "Direct to cloud" : "Through the server"}</dd>
        </div>
        <div className="space-y-2 px-5 py-3">
          <dt className="text-muted-foreground">Allowed types</dt>
          <dd>
            {mimeTypes.length === 0 ? (
              <span className="text-muted-foreground">No restrictions reported.</span>
            ) : (
              <ul className="flex flex-wrap gap-1.5">
                {mimeTypes.map((type) => (
                  <li key={type} className="rounded-md border border-border bg-muted/60 px-1.5 py-0.5 font-mono text-xs text-foreground">
                    {type}
                  </li>
                ))}
              </ul>
            )}
          </dd>
        </div>
      </dl>
    </Panel>
  );
}

function greeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) {
    return "Good morning";
  }
  if (hour < 18) {
    return "Good afternoon";
  }
  return "Good evening";
}

function humanizeArea(area: string): string {
  const spaced = area.replace(/([a-z])([A-Z])/g, "$1 $2");
  return spaced.charAt(0) + spaced.slice(1).toLowerCase();
}

const RELATIVE_STEPS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["second", 60],
  ["minute", 60],
  ["hour", 24],
  ["day", 7],
  ["week", 4.35],
  ["month", 12],
  ["year", Number.POSITIVE_INFINITY],
];

function relativeTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  let value = (date.getTime() - Date.now()) / 1000;
  if (Math.abs(value) < 45) {
    return "just now";
  }
  const format = new Intl.RelativeTimeFormat(undefined, { numeric: "auto", style: "short" });
  for (const [unit, size] of RELATIVE_STEPS) {
    if (Math.abs(value) < size) {
      return format.format(Math.round(value), unit);
    }
    value /= size;
  }
  return date.toLocaleDateString();
}

function formatCount(value: number | undefined): string {
  return value === undefined ? "—" : new Intl.NumberFormat().format(value);
}

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return "—";
  }
  if (bytes < 1000) {
    return `${bytes} B`;
  }
  const units = ["KB", "MB", "GB", "TB"];
  let value = bytes;
  let unitIndex = -1;
  while (value >= 1000 && unitIndex < units.length - 1) {
    value /= 1000;
    unitIndex += 1;
  }
  const digits = value >= 100 ? 0 : value >= 10 ? 1 : 2;
  return `${value.toFixed(digits)} ${units[unitIndex]}`;
}

function formatMegabytes(mb: number): string {
  if (!Number.isFinite(mb)) {
    return "—";
  }
  if (mb >= 1000) {
    return formatGigabytes(mb / 1000);
  }
  return `${trimNumber(mb)} MB`;
}

function formatGigabytes(gb: number): string {
  if (!Number.isFinite(gb)) {
    return "—";
  }
  return `${trimNumber(gb)} GB`;
}

function formatPercent(value: number): string {
  if (!Number.isFinite(value)) {
    return "0%";
  }
  return `${trimNumber(value)}%`;
}

function trimNumber(value: number): string {
  if (Math.abs(value) >= 100) {
    return value.toFixed(0);
  }
  if (Math.abs(value) >= 10) {
    return value.toFixed(1).replace(/\.0$/, "");
  }
  return value.toFixed(2).replace(/\.?0+$/, "") || "0";
}

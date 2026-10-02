import { adminApi } from "@raytha/api";
import { Badge, buttonVariants, cn, type BadgeProps } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { Clock, ExternalLink, Eye, FileCode2, Home, Link2, Sparkles } from "lucide-react";
import type { ReactNode } from "react";
import { jsonString } from "../entity";
import type { AuditStamp, SitePageDetail, SitePageStatus } from "./models";

const STATUS: Record<SitePageStatus, { label: string; variant: BadgeProps["variant"]; hint: string }> = {
  draft: {
    label: "Draft",
    variant: "secondary",
    hint: "Not on the public site yet. Preview shows the draft.",
  },
  published: {
    label: "Published",
    variant: "success",
    hint: "Live on the public site.",
  },
  "published-with-changes": {
    label: "Published · unpublished changes",
    variant: "warning",
    hint: "Visitors see the published version. Publish to ship the draft.",
  },
};

export function useIsHomePage(id: string): boolean {
  const configurationQuery = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });
  const data = configurationQuery.data ?? {};
  return jsonString(data, "homePageType") === "SitePage" && jsonString(data, "homePageId") === id;
}

export function publicPageUrl(routePath: string, preview = false): string {
  const pathBase = window.location.pathname.split("/raytha")[0] ?? "";
  const path = `${pathBase}/${routePath.replace(/^\/+/, "")}`;
  return preview ? `${path}?previewDraft=true` : path;
}

export function statusHint(status: SitePageStatus): string {
  return STATUS[status].hint;
}

export function SitePageStatusBadge({ status }: { status: SitePageStatus }) {
  const { label, variant, hint } = STATUS[status];
  return (
    <Badge variant={variant} title={hint}>
      {label}
    </Badge>
  );
}

export function SitePageMeta({ page, isHome }: { page: SitePageDetail; isHome: boolean }) {
  return (
    <>
      <SitePageStatusBadge status={page.status} />
      {isHome ? (
        <Badge variant="info">
          <Home className="size-3" aria-hidden />
          Home page
        </Badge>
      ) : null}
      <MetaItem icon={Link2} label="Route">
        {page.isPublished ? (
          <a
            href={publicPageUrl(page.routePath)}
            target="_blank"
            rel="noopener noreferrer"
            className="font-mono text-foreground hover:text-primary hover:underline"
          >
            /{page.routePath}
          </a>
        ) : (
          <span className="font-mono text-foreground">/{page.routePath}</span>
        )}
      </MetaItem>
      {page.templateLabel ? (
        <MetaItem icon={FileCode2} label="Template">
          <span className="text-foreground">{page.templateLabel}</span>
        </MetaItem>
      ) : null}
      <MetaItem icon={Clock} label="Last modified">
        <Stamp verb="Updated" stamp={page.modified} />
      </MetaItem>
      <MetaItem icon={Sparkles} label="Created">
        <Stamp verb="Created" stamp={page.created} />
      </MetaItem>
    </>
  );
}

export function PreviewActions({ page }: { page: SitePageDetail }) {
  return (
    <>
      <a
        href={publicPageUrl(page.routePath, true)}
        target="_blank"
        rel="noopener noreferrer"
        className={buttonVariants({ variant: "outline" })}
        title="Open the draft on the public site in a new tab"
      >
        <Eye aria-hidden />
        Preview
      </a>
      {page.isPublished ? (
        <a
          href={publicPageUrl(page.routePath)}
          target="_blank"
          rel="noopener noreferrer"
          className={buttonVariants({ variant: "outline" })}
        >
          <ExternalLink aria-hidden />
          View live
        </a>
      ) : null}
    </>
  );
}

export function SitePageTabs({ id, active }: { id: string; active: "settings" | "layout" }) {
  const tab = (isActive: boolean) =>
    cn(
      "-mb-px inline-flex h-10 items-center border-b-2 px-3 text-sm font-medium transition-colors",
      isActive
        ? "border-primary text-foreground"
        : "border-transparent text-muted-foreground hover:border-border-strong hover:text-foreground",
    );
  return (
    <nav className="flex gap-1" aria-label="Site page sections">
      <Link
        to="/site-pages/$id"
        params={{ id }}
        activeOptions={{ exact: true }}
        className={tab(active === "settings")}
        aria-current={active === "settings" ? "page" : undefined}
      >
        Settings
      </Link>
      <Link
        to="/site-pages/$id/layout"
        params={{ id }}
        className={tab(active === "layout")}
        aria-current={active === "layout" ? "page" : undefined}
      >
        Layout
      </Link>
    </nav>
  );
}

function MetaItem({
  icon: Icon,
  label,
  children,
}: {
  icon: typeof Clock;
  label: string;
  children: ReactNode;
}) {
  return (
    <span className="inline-flex items-center gap-1.5 text-muted-foreground">
      <Icon className="size-3.5 shrink-0" aria-hidden />
      <span className="sr-only">{label}:</span>
      {children}
    </span>
  );
}

function Stamp({ verb, stamp }: { verb: string; stamp: AuditStamp }) {
  if (!stamp.at) {
    return <span>{verb} —</span>;
  }
  const date = new Date(stamp.at);
  return (
    <span>
      {verb}{" "}
      <time dateTime={stamp.at} title={date.toLocaleString()} className="text-foreground">
        {relativeTime(date)}
      </time>
      {stamp.by ? <> by <span className="text-foreground">{stamp.by}</span></> : null}
    </span>
  );
}

const RELATIVE_UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ["year", 31_536_000],
  ["month", 2_592_000],
  ["week", 604_800],
  ["day", 86_400],
  ["hour", 3_600],
  ["minute", 60],
];

const relativeFormat = new Intl.RelativeTimeFormat(undefined, { numeric: "auto" });

export function relativeTime(date: Date, now = Date.now()): string {
  const seconds = Math.round((date.getTime() - now) / 1000);
  for (const [unit, size] of RELATIVE_UNITS) {
    if (Math.abs(seconds) >= size) {
      return relativeFormat.format(Math.round(seconds / size), unit);
    }
  }
  return "just now";
}

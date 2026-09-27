import { adminApi } from "@raytha/api";
import { Badge, cn, PageHeader } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { Blocks, Images, LayoutTemplate, Settings2, type LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { entityFields, readBoolean, readString } from "../entity";

export type ThemeSection = "settings" | "web" | "widget" | "assets";

const sections = [
  { id: "settings", label: "Settings", to: "/themes/$themeId", icon: Settings2 },
  { id: "web", label: "Web templates", to: "/themes/$themeId/web-templates", icon: LayoutTemplate },
  { id: "widget", label: "Widget templates", to: "/themes/$themeId/widget-templates", icon: Blocks },
  { id: "assets", label: "Assets", to: "/themes/$themeId/assets", icon: Images },
] as const satisfies readonly { id: ThemeSection; label: string; to: string; icon: LucideIcon }[];

export interface ThemeSummary {
  title: string;
  developerName: string;
  isActive: boolean;
}

export function useThemeSummary(themeId: string): ThemeSummary | null {
  const theme = useQuery({
    queryKey: ["themes", themeId],
    queryFn: () => adminApi.themes.get(themeId),
    enabled: themeId.length > 0,
  });
  const configuration = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });
  if (!theme.data) {
    return null;
  }
  const fields = entityFields(theme.data);
  return {
    title: readString(fields, "title"),
    developerName: readString(fields, "developerName"),
    isActive: configuration.data?.activeThemeId === theme.data.id || readBoolean(fields, "isActive"),
  };
}

export function ThemeSectionHeader({
  themeId,
  active,
  actions,
}: {
  themeId: string;
  active: ThemeSection;
  actions?: ReactNode;
}) {
  const theme = useThemeSummary(themeId);
  return (
    <PageHeader
      back={<ListBackLink to="/themes" listKey="themes" label="themes" />}
      title={
        <span className="flex min-h-9 flex-wrap items-center gap-3">
          <span>{theme?.title || "Theme"}</span>
          {theme?.isActive ? <Badge variant="success">Active</Badge> : null}
        </span>
      }
      meta={theme?.developerName ? <code>{theme.developerName}</code> : undefined}
      actions={actions}
      tabs={
        <div role="tablist" aria-label="Theme sections">
          {sections.map((section) => {
            const selected = section.id === active;
            const Icon = section.icon;
            return (
              <Link
                key={section.id}
                to={section.to}
                params={{ themeId }}
                role="tab"
                aria-selected={selected}
                aria-current={selected ? "page" : undefined}
                className="inline-flex items-center gap-2"
              >
                <Icon className={cn("size-4", selected && "text-primary")} aria-hidden />
                {section.label}
              </Link>
            );
          })}
        </div>
      }
    />
  );
}

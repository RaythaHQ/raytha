import { adminApi, formatError } from "@raytha/api";
import type { SaveSitePageWidgetsInput } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { GridStack, type GridStackNode } from "gridstack";
import { useEffect, useRef, useState } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { listHref } from "../../lib/list-query";
import {
  parseSitePage,
  settingsJson,
  widgetSummary,
  type SitePageSection,
  type SitePageWidget,
} from "./models";

import "gridstack/dist/gridstack.min.css";

export function SitePageLayoutPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Layout", "Site page"]);

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Layout" />
        <p className="text-sm text-muted-foreground">Missing page id.</p>
      </div>
    );
  }

  return <SitePageLayout id={id} />;
}

function SitePageLayout({ id }: { id: string }) {
  const queryClient = useQueryClient();
  const [draft, setDraft] = useState<SitePageSection[] | null>(null);
  const [addSectionName, setAddSectionName] = useState("");

  const pageQuery = useQuery({
    queryKey: ["site-pages", id],
    queryFn: () => adminApi.sitePages.get(id),
  });

  const page = pageQuery.data ? parseSitePage(pageQuery.data) : null;
  const sections = draft ?? page?.widgets ?? [];

  const save = useMutation({
    mutationFn: async () => {
      const currentNames = new Set(sections.map((section) => section.name));
      const removed = (page?.widgets ?? [])
        .map((section) => section.name)
        .filter((name) => !currentNames.has(name));
      const payloads: SaveSitePageWidgetsInput[] = [
        ...sections.map((section) => ({
          sectionName: section.name,
          widgets: section.widgets.map((widget) => ({
            ...(widget.id ? { id: widget.id } : {}),
            widgetType: widget.widgetType,
            settingsJson: settingsJson(widget.settings),
            row: widget.row,
            column: widget.column,
            columnSpan: widget.columnSpan,
            cssClass: widget.cssClass,
            htmlId: widget.htmlId,
            customAttributes: widget.customAttributes,
          })),
        })),
        ...removed.map((sectionName) => ({ sectionName, widgets: [] })),
      ];
      for (const body of payloads) {
        await adminApi.sitePages.saveWidgets(id, body);
      }
    },
    onSuccess: () => {
      toast.success("Layout saved as draft");
      setDraft(null);
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const updateSections = (next: SitePageSection[]) => {
    setDraft(next);
  };

  const updateSection = (name: string, widgets: SitePageWidget[]) => {
    updateSections(sections.map((section) => (section.name === name ? { ...section, widgets } : section)));
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={page ? `${page.title || "Site page"} layout` : "Layout"}
        description={page?.isDraft ? "Editing draft widgets." : "Changes save as a draft until you publish."}
        actions={
          <>
            <Link
              to="/site-pages/$id"
              params={{ id }}
              className="inline-flex h-10 items-center rounded-lg border border-input bg-card px-4 text-sm font-medium shadow-card hover:bg-brand-50"
            >
              Settings
            </Link>
            <Button type="button" loading={save.isPending} onClick={() => save.mutate()}>
              Save layout
            </Button>
          </>
        }
      />
      <ListBackLink to="/site-pages" listKey="site-pages" label="site pages" />
      <QueryGate query={pageQuery}>
        {() => (
          <div className="space-y-4">
            {sections.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                This page has no sections yet. Add one that matches a <code>render_section</code> name in the
                template.
              </p>
            ) : null}
            {sections.map((section) => (
              <SectionEditor
                key={section.name}
                pageId={id}
                section={section}
                onChange={(widgets) => updateSection(section.name, widgets)}
                onRemoveSection={() => updateSections(sections.filter((item) => item.name !== section.name))}
              />
            ))}
            <Card>
              <CardHeader>
                <CardTitle>Add section</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-wrap items-end gap-3">
                <FormField label="Section name" htmlFor="new-section-name">
                  {(control) => (
                    <Input
                      {...control}
                      value={addSectionName}
                      onChange={(event) => setAddSectionName(event.target.value)}
                      placeholder="main"
                    />
                  )}
                </FormField>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    const name = addSectionName.trim();
                    if (!name) {
                      toast.error("Section name is required.");
                      return;
                    }
                    if (sections.some((section) => section.name === name)) {
                      toast.error("That section already exists.");
                      return;
                    }
                    updateSections([...sections, { name, widgets: [] }]);
                    setAddSectionName("");
                  }}
                >
                  Add section
                </Button>
              </CardContent>
            </Card>
          </div>
        )}
      </QueryGate>
    </div>
  );
}

function SectionEditor({
  pageId,
  section,
  onChange,
  onRemoveSection,
}: {
  pageId: string;
  section: SitePageSection;
  onChange: (widgets: SitePageWidget[]) => void;
  onRemoveSection: () => void;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-3 space-y-0">
        <div>
          <CardTitle>{section.name}</CardTitle>
          <p className="text-sm text-muted-foreground">
            {section.widgets.length === 1 ? "1 widget" : `${section.widgets.length} widgets`}
          </p>
        </div>
        <div className="flex gap-2">
          <a
            href={listHref("/site-pages/$id/layout/widgets/new", { id: pageId }, { section: section.name })}
            className="inline-flex h-8 items-center rounded-lg border border-input bg-card px-3 text-sm font-medium shadow-card hover:bg-brand-50"
          >
            Add widget
          </a>
          {section.widgets.length === 0 ? (
            <Button type="button" variant="ghost" size="sm" onClick={onRemoveSection}>
              Remove section
            </Button>
          ) : null}
        </div>
      </CardHeader>
      <CardContent>
        {section.widgets.length === 0 ? (
          <p className="text-sm text-muted-foreground">No widgets in this section.</p>
        ) : (
          <SectionGrid pageId={pageId} widgets={section.widgets} onChange={onChange} />
        )}
      </CardContent>
    </Card>
  );
}

function SectionGrid({
  pageId,
  widgets,
  onChange,
}: {
  pageId: string;
  widgets: SitePageWidget[];
  onChange: (widgets: SitePageWidget[]) => void;
}) {
  const containerRef = useRef<HTMLDivElement>(null);
  const widgetsRef = useRef(widgets);
  widgetsRef.current = widgets;
  const ids = widgets.map((widget) => widget.clientKey).join(",");

  useEffect(() => {
    const element = containerRef.current;
    if (!element) {
      return;
    }
    const grid = GridStack.init(
      {
        column: 12,
        cellHeight: 100,
        margin: 10,
        float: false,
        columnOpts: { columnMax: 12 },
        resizable: { handles: "e,w" },
      },
      element,
    );
    if (!grid) {
      return;
    }
    const handleChange = (_event: Event, items?: GridStackNode[]) => {
      if (!items || items.length === 0) {
        return;
      }
      const next = widgetsRef.current.map((widget) => {
        const item = items.find((node) => node.id === widget.clientKey);
        if (!item) {
          return widget;
        }
        return {
          ...widget,
          column: item.x ?? widget.column,
          row: item.y ?? widget.row,
          columnSpan: item.w ?? widget.columnSpan,
        };
      });
      onChange(next);
    };
    grid.on("change", handleChange);
    return () => {
      grid.off("change");
      grid.destroy(false);
    };
  }, [ids, onChange]);

  return (
    <div ref={containerRef} className="grid-stack">
      {widgets.map((widget) => (
        <div
          key={widget.clientKey}
          className="grid-stack-item"
          gs-id={widget.clientKey}
          gs-x={widget.column}
          gs-y={widget.row}
          gs-w={widget.columnSpan}
          gs-h={1}
        >
          <div className="grid-stack-item-content flex flex-col justify-between rounded-lg border border-border bg-card p-3">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-medium">{widget.widgetType}</span>
                <Badge variant="secondary">{widget.columnSpan} cols</Badge>
              </div>
              <p className="truncate text-sm text-muted-foreground">{widgetSummary(widget)}</p>
            </div>
            <div className="mt-2 flex flex-wrap gap-1">
              <Link
                to="/site-pages/$id/layout/widgets/$widgetId"
                params={{ id: pageId, widgetId: widget.id || widget.clientKey }}
                className="inline-flex h-8 items-center rounded-lg px-3 text-sm hover:bg-accent"
              >
                Edit
              </Link>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => onChange(widgets.filter((item) => item.clientKey !== widget.clientKey))}
              >
                Remove
              </Button>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

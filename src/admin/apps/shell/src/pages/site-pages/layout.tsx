import { adminApi, formatError } from "@raytha/api";
import type { SaveSitePageWidgetsInput } from "@raytha/api";
import {
  Badge,
  Button,
  buttonVariants,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  ConfirmDialog,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { GridStack, type GridStackNode } from "gridstack";
import { AlertTriangle, GripVertical, Pencil, Plus, Trash2 } from "lucide-react";
import { useEffect, useEffectEvent, useLayoutEffect, useRef, useState } from "react";
import { ListBackLink, AppLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { listHref } from "../../lib/list-query";
import {
  parseSitePage,
  settingsJson,
  widgetSummary,
  type SitePageSection,
  type SitePageWidget,
} from "./models";
import { PreviewActions, SitePageMeta, SitePageTabs, statusHint, useIsHomePage } from "./page-meta";

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
  const [removing, setRemoving] = useState<SitePageSection | null>(null);
  const isHome = useIsHomePage(id);
  const [saveState, setSaveState] = useState<"idle" | "dirty" | "saving" | "saved">("idle");
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const readyRef = useRef(false);
  const sectionsRef = useRef<SitePageSection[]>([]);
  const pageWidgetsRef = useRef<SitePageSection[]>([]);

  const pageQuery = useQuery({
    queryKey: ["site-pages", id],
    queryFn: () => adminApi.sitePages.get(id),
  });

  const page = pageQuery.data ? parseSitePage(pageQuery.data) : null;
  const sections = draft ?? page?.widgets ?? [];

  useLayoutEffect(() => {
    sectionsRef.current = sections;
    pageWidgetsRef.current = page?.widgets ?? [];
  });

  const persistDraft = async (sectionsToSave: SitePageSection[]) => {
    const currentNames = new Set(sectionsToSave.map((section) => section.name));
    const removed = pageWidgetsRef.current
      .map((section) => section.name)
      .filter((name) => !currentNames.has(name));
    const payloads: SaveSitePageWidgetsInput[] = [
      ...removed.map((sectionName) => ({ sectionName, widgets: [] as SaveSitePageWidgetsInput["widgets"] })),
      ...sectionsToSave.map((section) => ({
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
    ];
    for (const body of payloads) {
      await adminApi.sitePages.saveWidgets(id, body);
    }
  };

  const save = useMutation({
    mutationFn: async (sectionsToSave: SitePageSection[]) => {
      setSaveState("saving");
      await persistDraft(sectionsToSave);
    },
    onSuccess: () => {
      setDraft(null);
      setSaveState("saved");
      void queryClient.invalidateQueries({ queryKey: ["site-pages", id] });
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
    },
    onError: (error) => {
      setSaveState("dirty");
      toast.error(formatError(error));
    },
  });

  const discard = useMutation({
    mutationFn: () => adminApi.sitePages.discardDraft(id),
    onSuccess: () => {
      toast.success("Draft cleared");
      setDraft(null);
      setSaveState("idle");
      void queryClient.invalidateQueries({ queryKey: ["site-pages", id] });
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const publish = useMutation({
    mutationFn: async () => {
      const latest = sectionsRef.current;
      if (draft) {
        await persistDraft(latest);
      }
      return adminApi.sitePages.publish(id);
    },
    onSuccess: () => {
      toast.success("Layout published");
      setDraft(null);
      setSaveState("idle");
      void queryClient.invalidateQueries({ queryKey: ["site-pages", id] });
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  useEffect(() => {
    if (pageQuery.isSuccess) {
      readyRef.current = true;
    }
  }, [pageQuery.isSuccess]);

  useEffect(() => {
    return () => {
      if (saveTimerRef.current) {
        clearTimeout(saveTimerRef.current);
      }
    };
  }, []);

  const scheduleAutoSave = () => {
    if (!readyRef.current) {
      return;
    }
    setSaveState("dirty");
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current);
    }
    saveTimerRef.current = setTimeout(() => {
      save.mutate(sectionsRef.current);
    }, 1000);
  };

  const updateSections = (next: SitePageSection[] | ((current: SitePageSection[]) => SitePageSection[])) => {
    setDraft((current) => {
      const base = current ?? pageWidgetsRef.current;
      const resolved = typeof next === "function" ? next(base) : next;
      sectionsRef.current = resolved;
      return resolved;
    });
    scheduleAutoSave();
  };

  const updateSection = (name: string, widgets: SitePageWidget[]) => {
    updateSections((current) =>
      current.map((section) => {
        if (section.name !== name) {
          return section;
        }
        const ids = new Set(section.widgets.map((widget) => widget.clientKey));
        return { ...section, widgets: widgets.filter((widget) => ids.has(widget.clientKey)) };
      }),
    );
  };

  const transferWidget = (
    targetSection: string,
    clientKey: string,
    position: Pick<SitePageWidget, "column" | "row" | "columnSpan">,
  ) => {
    updateSections((current) => {
      let moved: SitePageWidget | undefined;
      const stripped = current.map((section) => {
        const found = section.widgets.find((widget) => widget.clientKey === clientKey);
        if (!found) {
          return section;
        }
        moved = { ...found, ...position };
        return {
          ...section,
          widgets: section.widgets.filter((widget) => widget.clientKey !== clientKey),
        };
      });
      if (!moved) {
        return current;
      }
      const widget = moved;
      return stripped.map((section) =>
        section.name === targetSection ? { ...section, widgets: [...section.widgets, widget] } : section,
      );
    });
  };

  const removeSection = (name: string) => {
    if (saveTimerRef.current) {
      clearTimeout(saveTimerRef.current);
    }
    const next = sectionsRef.current.filter((section) => section.name !== name);
    sectionsRef.current = next;
    setDraft(next);
    setRemoving(null);
    save.mutate(next);
  };

  const saveLabel =
    saveState === "saving"
      ? "Saving draft…"
      : saveState === "dirty"
        ? "Unsaved changes"
        : saveState === "saved"
          ? "Draft saved"
          : null;
  const hasDraft = Boolean(page?.isDraft || draft);

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/site-pages" listKey="site-pages" label="site pages" />}
        title={page?.title || "Layout"}
        description={
          page ? (
            <>
              {statusHint(page.status)}
              {saveLabel ? <span className="ml-2 font-medium text-foreground">{saveLabel}</span> : null}
            </>
          ) : undefined
        }
        meta={page ? <SitePageMeta page={page} isHome={isHome} /> : undefined}
        tabs={<SitePageTabs id={id} active="layout" />}
        actions={
          page ? (
            <>
              <PreviewActions page={page} />
              {hasDraft ? (
                <Button type="button" variant="outline" loading={discard.isPending} onClick={() => discard.mutate()}>
                  Clear draft
                </Button>
              ) : null}
              <Button
                type="button"
                loading={publish.isPending}
                disabled={page.status === "published" && !draft}
                title={page.status === "published" && !draft ? "Nothing to publish. The live page matches." : undefined}
                onClick={() => publish.mutate()}
              >
                Publish
              </Button>
            </>
          ) : undefined
        }
      />
      <QueryGate query={pageQuery}>
        {() => (
          <div className="space-y-4">
            {sections.map((section) => (
              <SectionEditor
                key={section.name}
                pageId={id}
                section={section}
                onChange={(widgets) => updateSection(section.name, widgets)}
                onReceive={(clientKey, position) => transferWidget(section.name, clientKey, position)}
                onRemoveSection={() => setRemoving(section)}
              />
            ))}
            <p className="text-sm text-muted-foreground">
              Sections come from the page template&rsquo;s <code>render_section</code> and <code>get_section</code>{" "}
              calls. To add one, edit the {page?.templateLabel ? <strong>{page.templateLabel}</strong> : "page"} template.
            </p>
          </div>
        )}
      </QueryGate>
      <ConfirmDialog
        open={removing !== null}
        onOpenChange={(open) => {
          if (!open) setRemoving(null);
        }}
        title={`Remove the “${removing?.name ?? ""}” section?`}
        body={
          removing
            ? `This deletes its ${removing.widgets.length} ${removing.widgets.length === 1 ? "widget" : "widgets"} from the draft. Drag any you want to keep into a template section first.`
            : undefined
        }
        confirmLabel="Remove section"
        onConfirm={() => {
          if (removing) removeSection(removing.name);
        }}
        pending={save.isPending}
      />
    </div>
  );
}

function SectionEditor({
  pageId,
  section,
  onChange,
  onReceive,
  onRemoveSection,
}: {
  pageId: string;
  section: SitePageSection;
  onChange: (widgets: SitePageWidget[]) => void;
  onReceive: (clientKey: string, position: Pick<SitePageWidget, "column" | "row" | "columnSpan">) => void;
  onRemoveSection: () => void;
}) {
  const orphaned = section.kind === "orphaned";
  return (
    <Card className={orphaned ? "border-warning-border" : undefined}>
      <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-3 space-y-0">
        <div className="min-w-0 space-y-1">
          <CardTitle className="flex items-center gap-2">
            <code className="font-mono">{section.name}</code>
            {orphaned ? <Badge variant="warning">Orphaned</Badge> : null}
            <span className="text-xs font-normal text-muted-foreground">
              {section.widgets.length} {section.widgets.length === 1 ? "widget" : "widgets"}
            </span>
          </CardTitle>
          {orphaned ? (
            <p className="flex items-start gap-2 text-sm text-warning">
              <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden />
              The page template no longer renders this section, so these widgets never appear on the public site.
              Drag them into a section above to keep them, or remove the section.
            </p>
          ) : (
            <p className="text-sm text-muted-foreground">
              Drag widgets to rearrange them, including into another section.
            </p>
          )}
        </div>
        <div className="flex gap-2">
          {orphaned ? (
            <Button type="button" variant="outline" size="sm" onClick={onRemoveSection}>
              <Trash2 aria-hidden />
              Remove section
            </Button>
          ) : (
            <AppLink
              href={listHref("/site-pages/$id/layout/widgets/new", { id: pageId }, { section: section.name })}
              className={buttonVariants({ variant: "outline", size: "sm" })}
            >
              <Plus aria-hidden />
              Add widget
            </AppLink>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {section.widgets.length === 0 ? (
          <p className="mb-2 text-sm text-muted-foreground">
            Empty. Add a widget, or drag one here from another section.
          </p>
        ) : null}
        <SectionGrid pageId={pageId} widgets={section.widgets} onChange={onChange} onReceive={onReceive} />
      </CardContent>
    </Card>
  );
}

function SectionGrid({
  pageId,
  widgets,
  onChange,
  onReceive,
}: {
  pageId: string;
  widgets: SitePageWidget[];
  onChange: (widgets: SitePageWidget[]) => void;
  onReceive: (clientKey: string, position: Pick<SitePageWidget, "column" | "row" | "columnSpan">) => void;
}) {
  const containerRef = useRef<HTMLDivElement>(null);
  const applyGridChange = useEffectEvent((items: GridStackNode[]) => {
    onChange(
      widgets.map((widget) => {
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
      }),
    );
  });
  const receiveWidget = useEffectEvent(onReceive);
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
        minRow: 1,
        acceptWidgets: true,
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
      applyGridChange(items);
    };
    const handleDropped = (_event: Event, previous: GridStackNode | undefined, node: GridStackNode) => {
      const clientKey = typeof node.id === "string" ? node.id : "";
      if (!clientKey || !node.el) {
        return;
      }
      const home = previous?.grid?.el;
      if (home) {
        home.appendChild(node.el);
      }
      receiveWidget(clientKey, {
        column: node.x ?? 0,
        row: node.y ?? 0,
        columnSpan: node.w ?? 1,
      });
    };
    grid.on("change", handleChange);
    grid.on("dropped", handleDropped);
    return () => {
      grid.off("change");
      grid.off("dropped");
      grid.destroy(false);
    };
  }, [ids]);

  return (
    <div ref={containerRef} className="grid-stack min-h-[120px]">
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
          <div className="grid-stack-item-content overflow-hidden rounded-lg border border-border bg-card shadow-card">
            <div className="flex items-center gap-2 border-b border-border bg-muted/60 px-2.5 py-1.5">
              <GripVertical className="size-3.5 shrink-0 text-muted-foreground" aria-hidden />
              <span className="min-w-0 flex-1 truncate text-xs font-semibold tracking-wide">
                {widget.widgetType}
              </span>
              <Badge variant="secondary" className="shrink-0 text-[10px]">
                {widget.columnSpan} cols
              </Badge>
              <div className="flex shrink-0 items-center gap-0.5">
                <Link
                  to="/site-pages/$id/layout/widgets/$widgetId"
                  params={{ id: pageId, widgetId: widget.id || widget.clientKey }}
                  title="Edit widget"
                  aria-label={`Edit ${widget.widgetType}`}
                  className="inline-flex size-7 items-center justify-center rounded-md text-foreground/80 transition-colors hover:bg-card hover:text-foreground"
                >
                  <Pencil className="size-3.5" aria-hidden />
                </Link>
                <button
                  type="button"
                  title="Remove widget"
                  aria-label={`Remove ${widget.widgetType}`}
                  className="inline-flex size-7 items-center justify-center rounded-md text-destructive/80 transition-colors hover:bg-card hover:text-destructive"
                  onClick={() => onChange(widgets.filter((item) => item.clientKey !== widget.clientKey))}
                >
                  <Trash2 className="size-3.5" aria-hidden />
                </button>
              </div>
            </div>
            <p className="truncate px-3 py-2 text-sm text-muted-foreground">{widgetSummary(widget)}</p>
          </div>
        </div>
      ))}
    </div>
  );
}

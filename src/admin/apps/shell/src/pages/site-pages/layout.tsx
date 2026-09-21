import { adminApi, formatError } from "@raytha/api";
import type { SaveSitePageWidgetsInput, SitePageWidgetDefinition } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { useState } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import {
  newWidget,
  parseSitePage,
  settingsJson,
  widgetSummary,
  type SitePageSection,
  type SitePageWidget,
  type WidgetSettings,
} from "./models";
import { WidgetSettingsForm } from "./widget-form";

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
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [addSectionName, setAddSectionName] = useState("");
  const [addToSection, setAddToSection] = useState<string | null>(null);

  const pageQuery = useQuery({
    queryKey: ["site-pages", id],
    queryFn: () => adminApi.sitePages.get(id),
  });
  const definitionsQuery = useQuery({
    queryKey: ["site-pages", "widget-definitions"],
    queryFn: () => adminApi.sitePages.widgetDefinitions(),
  });

  const page = pageQuery.data ? parseSitePage(pageQuery.data) : null;
  const sections = draft ?? page?.widgets ?? [];
  const definitions = definitionsQuery.data ?? [];

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
                section={section}
                definitions={definitions}
                selectedKey={selectedKey}
                onSelect={setSelectedKey}
                onChange={(widgets) => updateSection(section.name, widgets)}
                onAddWidget={() => setAddToSection(section.name)}
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

      <AddWidgetDialog
        open={addToSection !== null}
        definitions={definitions}
        onOpenChange={(open) => {
          if (!open) {
            setAddToSection(null);
          }
        }}
        onPick={(widgetType) => {
          const sectionName = addToSection;
          if (!sectionName) {
            return;
          }
          const section = sections.find((item) => item.name === sectionName);
          const maxRow = section?.widgets.reduce((max, widget) => Math.max(max, widget.row), -1) ?? -1;
          const widget = newWidget(widgetType, maxRow + 1);
          updateSection(sectionName, [...(section?.widgets ?? []), widget]);
          setSelectedKey(widget.clientKey);
          setAddToSection(null);
        }}
      />
    </div>
  );
}

function SectionEditor({
  section,
  definitions,
  selectedKey,
  onSelect,
  onChange,
  onAddWidget,
  onRemoveSection,
}: {
  section: SitePageSection;
  definitions: SitePageWidgetDefinition[];
  selectedKey: string | null;
  onSelect: (key: string | null) => void;
  onChange: (widgets: SitePageWidget[]) => void;
  onAddWidget: () => void;
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
          <Button type="button" variant="outline" size="sm" onClick={onAddWidget}>
            Add widget
          </Button>
          {section.widgets.length === 0 ? (
            <Button type="button" variant="ghost" size="sm" onClick={onRemoveSection}>
              Remove section
            </Button>
          ) : null}
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        {section.widgets.length === 0 ? (
          <p className="text-sm text-muted-foreground">No widgets in this section.</p>
        ) : (
          section.widgets.map((widget, index) => (
            <WidgetCard
              key={widget.clientKey}
              widget={widget}
              definition={definitions.find((item) => item.developerName === widget.widgetType)}
              expanded={selectedKey === widget.clientKey}
              canMoveUp={index > 0}
              canMoveDown={index < section.widgets.length - 1}
              onToggle={() => onSelect(selectedKey === widget.clientKey ? null : widget.clientKey)}
              onMove={(direction) => onChange(moveWidget(section.widgets, index, direction))}
              onRemove={() => onChange(section.widgets.filter((item) => item.clientKey !== widget.clientKey))}
              onChange={(next) =>
                onChange(section.widgets.map((item) => (item.clientKey === widget.clientKey ? next : item)))
              }
            />
          ))
        )}
      </CardContent>
    </Card>
  );
}

function WidgetCard({
  widget,
  definition,
  expanded,
  canMoveUp,
  canMoveDown,
  onToggle,
  onMove,
  onRemove,
  onChange,
}: {
  widget: SitePageWidget;
  definition: SitePageWidgetDefinition | undefined;
  expanded: boolean;
  canMoveUp: boolean;
  canMoveDown: boolean;
  onToggle: () => void;
  onMove: (direction: -1 | 1) => void;
  onRemove: () => void;
  onChange: (widget: SitePageWidget) => void;
}) {
  return (
    <div className="rounded-lg border border-border">
      <div className="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
        <button type="button" className="min-w-0 text-left" onClick={onToggle}>
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-medium">{definition?.displayName ?? widget.widgetType}</span>
            <Badge variant="secondary">{widget.widgetType}</Badge>
          </div>
          <p className="truncate text-sm text-muted-foreground">{widgetSummary(widget)}</p>
        </button>
        <div className="flex flex-wrap gap-1">
          <Button type="button" variant="ghost" size="sm" disabled={!canMoveUp} onClick={() => onMove(-1)}>
            Up
          </Button>
          <Button type="button" variant="ghost" size="sm" disabled={!canMoveDown} onClick={() => onMove(1)}>
            Down
          </Button>
          <Button type="button" variant="ghost" size="sm" onClick={onToggle}>
            {expanded ? "Close" : "Edit"}
          </Button>
          <Button type="button" variant="ghost" size="sm" onClick={onRemove}>
            Remove
          </Button>
        </div>
      </div>
      {expanded ? (
        <div className="space-y-4 border-t border-border p-3">
          <WidgetSettingsForm
            key={widget.clientKey}
            settings={widget.settings}
            onChange={(settings: WidgetSettings) => onChange({ ...widget, settings })}
          />
          <div className="grid gap-3 md:grid-cols-2">
            <FormField label="Column span" htmlFor={`${widget.clientKey}-span`}>
              {(control) => (
                <Input
                  {...control}
                  type="number"
                  min={1}
                  max={12}
                  value={String(widget.columnSpan)}
                  onChange={(event) => {
                    const parsed = Number(event.target.value);
                    onChange({
                      ...widget,
                      columnSpan: Number.isFinite(parsed) ? Math.min(12, Math.max(1, parsed)) : 12,
                    });
                  }}
                />
              )}
            </FormField>
            <FormField label="Column" htmlFor={`${widget.clientKey}-col`}>
              {(control) => (
                <Input
                  {...control}
                  type="number"
                  min={0}
                  max={11}
                  value={String(widget.column)}
                  onChange={(event) => {
                    const parsed = Number(event.target.value);
                    onChange({
                      ...widget,
                      column: Number.isFinite(parsed) ? Math.min(11, Math.max(0, parsed)) : 0,
                    });
                  }}
                />
              )}
            </FormField>
            <FormField label="CSS class" htmlFor={`${widget.clientKey}-css`}>
              {(control) => (
                <Input
                  {...control}
                  value={widget.cssClass}
                  onChange={(event) => onChange({ ...widget, cssClass: event.target.value })}
                />
              )}
            </FormField>
            <FormField label="HTML id" htmlFor={`${widget.clientKey}-html-id`}>
              {(control) => (
                <Input
                  {...control}
                  value={widget.htmlId}
                  onChange={(event) => onChange({ ...widget, htmlId: event.target.value })}
                />
              )}
            </FormField>
            <FormField label="Custom attributes" htmlFor={`${widget.clientKey}-attrs`}>
              {(control) => (
                <Input
                  {...control}
                  value={widget.customAttributes}
                  onChange={(event) => onChange({ ...widget, customAttributes: event.target.value })}
                />
              )}
            </FormField>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function AddWidgetDialog({
  open,
  definitions,
  onOpenChange,
  onPick,
}: {
  open: boolean;
  definitions: SitePageWidgetDefinition[];
  onOpenChange: (open: boolean) => void;
  onPick: (widgetType: string) => void;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogHeader>
        <DialogTitle>Add widget</DialogTitle>
      </DialogHeader>
      <DialogContent className="space-y-2">
        {definitions.length === 0 ? (
          <p className="text-sm text-muted-foreground">No widget types are available.</p>
        ) : (
          definitions.map((definition) => (
            <button
              key={definition.developerName}
              type="button"
              className="flex w-full flex-col rounded-lg border border-border px-3 py-2 text-left hover:bg-accent"
              onClick={() => onPick(definition.developerName)}
            >
              <span className="font-medium">{definition.displayName}</span>
              <span className="text-sm text-muted-foreground">{definition.description}</span>
            </button>
          ))
        )}
      </DialogContent>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
          Cancel
        </Button>
      </DialogFooter>
    </Dialog>
  );
}

function moveWidget(widgets: SitePageWidget[], index: number, direction: -1 | 1): SitePageWidget[] {
  const nextIndex = index + direction;
  const current = widgets[index];
  const neighbor = widgets[nextIndex];
  if (!current || !neighbor) {
    return widgets;
  }
  const swapped = widgets.map((widget, widgetIndex) => {
    if (widgetIndex === index) {
      return { ...current, row: neighbor.row, column: neighbor.column };
    }
    if (widgetIndex === nextIndex) {
      return { ...neighbor, row: current.row, column: current.column };
    }
    return widget;
  });
  return swapped.sort((left, right) => left.row - right.row || left.column - right.column);
}

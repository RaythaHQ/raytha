import { adminApi, formatError } from "@raytha/api";
import type { SaveSitePageWidgetsInput } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation, useNavigate, useParams } from "@tanstack/react-router";
import { useEffect, useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import {
  defaultSettingsForType,
  newWidget,
  parseSitePage,
  settingsJson,
  type SitePageSection,
  type SitePageWidget,
  type WidgetSettings,
} from "./models";
import { WidgetSettingsForm } from "./widget-form";

export function NewSitePageWidgetPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  const location = useLocation();
  const section = new URLSearchParams(
    location.searchStr.startsWith("?") ? location.searchStr.slice(1) : location.searchStr,
  ).get("section");
  return <WidgetEditorPage pageId={id} widgetId={null} initialSection={section ?? ""} />;
}

export function EditSitePageWidgetPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  const widgetId = typeof params.widgetId === "string" ? params.widgetId : "";
  return <WidgetEditorPage pageId={id} widgetId={widgetId || null} initialSection="" />;
}

function WidgetEditorPage({
  pageId,
  widgetId,
  initialSection,
}: {
  pageId: string;
  widgetId: string | null;
  initialSection: string;
}) {
  const isNew = widgetId === null;
  useDocumentTitle([isNew ? "New widget" : "Edit widget", "Site page"]);

  if (!pageId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Widget" />
        <p className="text-sm text-muted-foreground">Missing page id.</p>
      </div>
    );
  }

  return <WidgetEditor pageId={pageId} widgetId={widgetId} initialSection={initialSection} />;
}

function WidgetEditor({
  pageId,
  widgetId,
  initialSection,
}: {
  pageId: string;
  widgetId: string | null;
  initialSection: string;
}) {
  const isNew = widgetId === null;
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [sectionName, setSectionName] = useState(initialSection);
  const [widgetType, setWidgetType] = useState("");
  const [draft, setDraft] = useState<SitePageWidget | null>(null);
  const [hydrated, setHydrated] = useState(false);

  const pageQuery = useQuery({
    queryKey: ["site-pages", pageId],
    queryFn: () => adminApi.sitePages.get(pageId),
  });
  const definitionsQuery = useQuery({
    queryKey: ["site-pages", "widget-definitions"],
    queryFn: () => adminApi.sitePages.widgetDefinitions(),
  });

  const page = pageQuery.data ? parseSitePage(pageQuery.data) : null;
  const sections = page?.widgets ?? [];
  const definitions = definitionsQuery.data ?? [];

  useEffect(() => {
    if (!page || hydrated) {
      return;
    }
    if (isNew) {
      setSectionName(initialSection || page.widgets[0]?.name || "");
      setHydrated(true);
      return;
    }
    if (!widgetId) {
      setHydrated(true);
      return;
    }
    const found = findWidget(page.widgets, widgetId);
    if (found) {
      setSectionName(found.section);
      setWidgetType(found.widget.widgetType);
      setDraft(found.widget);
    }
    setHydrated(true);
  }, [hydrated, initialSection, isNew, page, widgetId]);

  const applyType = (nextType: string) => {
    setWidgetType(nextType);
    const section = sections.find((item) => item.name === sectionName);
    const maxRow = section?.widgets.reduce((max, widget) => Math.max(max, widget.row), -1) ?? -1;
    setDraft({
      ...newWidget(nextType, maxRow + 1),
      settings: defaultSettingsForType(nextType),
    });
  };

  const save = useMutation({
    mutationFn: async () => {
      if (!draft || !sectionName) {
        throw new Error("Pick a section and widget type.");
      }
      const section = sections.find((item) => item.name === sectionName);
      const widgets = section?.widgets ?? [];
      const nextWidgets = isNew
        ? [...widgets, draft]
        : widgets.map((item) => (item.id === widgetId || item.clientKey === widgetId ? draft : item));
      const body: SaveSitePageWidgetsInput = {
        sectionName,
        widgets: nextWidgets.map((widget) => ({
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
      };
      await adminApi.sitePages.saveWidgets(pageId, body);
    },
    onSuccess: () => {
      toast.success(isNew ? "Widget added" : "Widget saved");
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
      void navigate({ to: "/site-pages/$id/layout", params: { id: pageId } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const settings: WidgetSettings | null = draft?.settings ?? null;

  return (
    <div className="space-y-6">
      <PageHeader title={isNew ? "New widget" : "Edit widget"} />
      <ListBackLink
        to="/site-pages/$id/layout"
        params={{ id: pageId }}
        listKey={`site-page-layout:${pageId}`}
        label="layout"
      />
      <QueryGate query={pageQuery}>
        {() =>
          !isNew && !draft ? (
            <p className="text-sm text-muted-foreground">That widget was not found on this page.</p>
          ) : (
            <Card>
              <CardContent className="space-y-4 pt-6">
                <form
                  className="space-y-4"
                  onSubmit={(event: FormEvent) => {
                    event.preventDefault();
                    save.mutate();
                  }}
                >
                  <FormField label="Section" required htmlFor="widget-section">
                    {(control) => (
                      <Select
                        {...control}
                        value={sectionName}
                        disabled={!isNew}
                        onChange={(event) => setSectionName(event.target.value)}
                      >
                        <option value="">Select a section</option>
                        {sections.map((section) => (
                          <option key={section.name} value={section.name}>
                            {section.name}
                          </option>
                        ))}
                      </Select>
                    )}
                  </FormField>
                  {isNew ? (
                    <FormField label="Widget type" required htmlFor="widget-type">
                      {(control) => (
                        <Select
                          {...control}
                          value={widgetType}
                          onChange={(event) => applyType(event.target.value)}
                        >
                          <option value="">Select a type</option>
                          {definitions.map((definition) => (
                            <option key={definition.developerName} value={definition.developerName}>
                              {definition.displayName}
                            </option>
                          ))}
                        </Select>
                      )}
                    </FormField>
                  ) : null}
                  {draft && settings ? (
                    <>
                      <WidgetSettingsForm
                        settings={settings}
                        onChange={(next) => setDraft({ ...draft, settings: next })}
                      />
                      <div className="grid gap-3 md:grid-cols-2">
                        <FormField label="CSS class" htmlFor="widget-css">
                          {(control) => (
                            <Input
                              {...control}
                              value={draft.cssClass}
                              onChange={(event) => setDraft({ ...draft, cssClass: event.target.value })}
                            />
                          )}
                        </FormField>
                        <FormField label="HTML id" htmlFor="widget-html-id">
                          {(control) => (
                            <Input
                              {...control}
                              value={draft.htmlId}
                              onChange={(event) => setDraft({ ...draft, htmlId: event.target.value })}
                            />
                          )}
                        </FormField>
                        <FormField label="Custom attributes" htmlFor="widget-attrs">
                          {(control) => (
                            <Input
                              {...control}
                              value={draft.customAttributes}
                              onChange={(event) => setDraft({ ...draft, customAttributes: event.target.value })}
                            />
                          )}
                        </FormField>
                      </div>
                    </>
                  ) : null}
                  <Button type="submit" loading={save.isPending} disabled={!draft || !sectionName}>
                    {isNew ? "Add widget" : "Save"}
                  </Button>
                </form>
              </CardContent>
            </Card>
          )
        }
      </QueryGate>
    </div>
  );
}

function findWidget(
  sections: SitePageSection[],
  widgetId: string,
): { section: string; widget: SitePageWidget } | null {
  for (const section of sections) {
    for (const widget of section.widgets) {
      if (widget.id === widgetId || widget.clientKey === widgetId) {
        return { section: section.name, widget };
      }
    }
  }
  return null;
}

import { adminApi, formatError, problemFieldErrors } from "@raytha/api";
import type { SaveSitePageWidgetsInput, SitePageWidgetDefinition } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  cn,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation, useNavigate, useParams } from "@tanstack/react-router";
import { TriangleAlert } from "lucide-react";
import { useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import {
  newWidget,
  parseSitePage,
  settingsJson,
  type SitePageSection,
  type SitePageWidget,
} from "./models";
import { WidgetSettingsForm } from "./widget-settings-form";

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
  const pageQuery = useQuery({
    queryKey: ["site-pages", pageId],
    queryFn: () => adminApi.sitePages.get(pageId),
  });
  const definitionsQuery = useQuery({
    queryKey: ["site-pages", "widget-definitions"],
    queryFn: () => adminApi.sitePages.widgetDefinitions(),
  });

  return (
    <QueryGate query={pageQuery}>
      {(pageEntity) => (
        <QueryGate query={definitionsQuery}>
          {(definitions) => (
            <WidgetForm
              pageId={pageId}
              widgetId={widgetId}
              initialSection={initialSection}
              sections={parseSitePage(pageEntity).widgets}
              definitions={definitions}
            />
          )}
        </QueryGate>
      )}
    </QueryGate>
  );
}

function WidgetForm({
  pageId,
  widgetId,
  initialSection,
  sections,
  definitions,
}: {
  pageId: string;
  widgetId: string | null;
  initialSection: string;
  sections: SitePageSection[];
  definitions: SitePageWidgetDefinition[];
}) {
  const isNew = widgetId === null;
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [loaded] = useState(() => (widgetId ? findWidget(sections, widgetId) : null));
  const [sectionName, setSectionName] = useState(
    () => loaded?.section ?? (initialSection || sections.find((section) => section.kind === "template")?.name || ""),
  );
  const [draft, setDraft] = useState<SitePageWidget | null>(() => loaded?.widget ?? null);
  const [errors, setErrors] = useState<string[]>([]);

  const section = sections.find((item) => item.name === sectionName);
  const siblings = section?.widgets ?? [];
  const definition = draft ? definitions.find((item) => item.developerName === draft.widgetType) : undefined;

  const pickType = (widgetType: string) => {
    const picked = definitions.find((item) => item.developerName === widgetType);
    const maxRow = siblings.reduce((max, widget) => Math.max(max, widget.row), -1);
    setDraft(picked ? newWidget(picked.developerName, picked.fields, maxRow + 1) : null);
    setErrors([]);
  };

  const save = useMutation({
    mutationFn: async (widget: SitePageWidget) => {
      const widgets = isNew
        ? [...siblings, widget]
        : siblings.map((item) => (item.clientKey === widget.clientKey ? widget : item));
      const body: SaveSitePageWidgetsInput = {
        sectionName,
        widgets: widgets.map((item) => ({
          ...(item.id ? { id: item.id } : {}),
          widgetType: item.widgetType,
          settingsJson: settingsJson(item.settings),
          row: item.row,
          column: item.column,
          columnSpan: item.columnSpan,
          cssClass: item.cssClass,
          htmlId: item.htmlId,
          customAttributes: item.customAttributes,
        })),
      };
      await adminApi.sitePages.saveWidgets(pageId, body);
    },
    onSuccess: () => {
      toast.success(isNew ? "Widget added" : "Widget saved");
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
      void navigate({ to: "/site-pages/$id/layout", params: { id: pageId } });
    },
    onError: (error) => {
      const index = isNew ? siblings.length : siblings.findIndex((item) => item.clientKey === draft?.clientKey);
      const own = Object.entries(problemFieldErrors(error))
        .filter(([key]) => key === "" || key.startsWith(`Widgets[${index}].`))
        .map(([, message]) => message);
      setErrors(own);
      toast.error(own.length > 0 ? "Fix the highlighted settings and save again." : formatError(error));
    },
  });

  const title = isNew ? "New widget" : "Edit widget";
  const typeLabel = definition?.displayName ?? draft?.widgetType ?? "";

  if (!isNew && !draft) {
    return (
      <div className="space-y-6">
        <PageHeader back={<LayoutBackLink pageId={pageId} />} title={title} />
        <p className="text-sm text-muted-foreground">That widget was not found on this page.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        back={<LayoutBackLink pageId={pageId} />}
        title={isNew ? title : typeLabel || title}
        description={
          isNew ? "Pick where the widget goes and what it is, then fill in its settings." : `In section ${sectionName}.`
        }
        meta={
          !isNew && draft ? (
            <>
              <code>{draft.widgetType}</code>
              {definition && !definition.isBuiltInTemplate ? <Badge variant="secondary">Custom</Badge> : null}
            </>
          ) : null
        }
        actions={
          <Button type="submit" form="widget-form" loading={save.isPending} disabled={!draft || !sectionName}>
            {isNew ? "Add widget" : "Save"}
          </Button>
        }
      />
      <form
        id="widget-form"
        className="space-y-6"
        noValidate
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          if (draft && sectionName) {
            save.mutate(draft);
          }
        }}
      >
        {isNew ? (
          <Card>
            <CardHeader>
              <CardTitle>Placement</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <FormField label="Section" required htmlFor="widget-section">
                {(control) => (
                  <Select
                    {...control}
                    value={sectionName}
                    onChange={(event) => setSectionName(event.target.value)}
                    className="max-w-sm"
                  >
                    <option value="">Select a section</option>
                    {sections
                      .filter((item) => item.kind === "template")
                      .map((item) => (
                        <option key={item.name} value={item.name}>
                          {item.name}
                        </option>
                      ))}
                  </Select>
                )}
              </FormField>
              <WidgetTypePicker
                definitions={definitions}
                value={draft?.widgetType ?? ""}
                onChange={pickType}
              />
            </CardContent>
          </Card>
        ) : null}

        {draft ? (
          <>
            <Card>
              <CardHeader>
                <CardTitle>{isNew && typeLabel ? `${typeLabel} settings` : "Settings"}</CardTitle>
                {definition?.description ? <CardDescription>{definition.description}</CardDescription> : null}
              </CardHeader>
              <CardContent className="space-y-5">
                {errors.length > 0 ? <SaveErrors messages={errors} /> : null}
                <WidgetSettingsForm
                  widgetType={draft.widgetType}
                  definition={definition}
                  settings={draft.settings}
                  onChange={(settings) => setDraft({ ...draft, settings })}
                />
              </CardContent>
            </Card>
            <AdvancedCard widget={draft} onChange={setDraft} />
          </>
        ) : null}

        <div className="flex justify-end">
          <Button type="submit" loading={save.isPending} disabled={!draft || !sectionName}>
            {isNew ? "Add widget" : "Save"}
          </Button>
        </div>
      </form>
    </div>
  );
}

function LayoutBackLink({ pageId }: { pageId: string }) {
  return (
    <ListBackLink
      to="/site-pages/$id/layout"
      params={{ id: pageId }}
      listKey={`site-page-layout:${pageId}`}
      label="layout"
    />
  );
}

function WidgetTypePicker({
  definitions,
  value,
  onChange,
}: {
  definitions: SitePageWidgetDefinition[];
  value: string;
  onChange: (widgetType: string) => void;
}) {
  return (
    <fieldset className="space-y-3">
      <legend className="text-sm font-medium">
        Widget type
        <span aria-hidden className="text-destructive">
          {" *"}
        </span>
      </legend>
      {definitions.length === 0 ? (
        <p className="text-sm text-muted-foreground">The active theme has no widget templates.</p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {definitions.map((definition) => {
            const checked = definition.developerName === value;
            return (
              <label
                key={definition.developerName}
                className={cn(
                  "relative flex cursor-pointer flex-col gap-1 rounded-xl border bg-card p-4 shadow-xs transition-[border-color,box-shadow] hover:border-border-strong focus-within:ring-[3px] focus-within:ring-brand-500/25",
                  checked ? "border-brand-500 ring-1 ring-brand-500" : "border-border",
                )}
              >
                <input
                  type="radio"
                  name="widget-type"
                  value={definition.developerName}
                  checked={checked}
                  onChange={() => onChange(definition.developerName)}
                  className="sr-only"
                />
                <span className="flex items-center gap-2">
                  <span className="text-sm font-semibold">{definition.displayName}</span>
                  {definition.isBuiltInTemplate ? null : <Badge variant="secondary">Custom</Badge>}
                </span>
                <span className="text-xs leading-5 text-muted-foreground">
                  {definition.description ||
                    `${definition.fields.length} ${definition.fields.length === 1 ? "field" : "fields"}`}
                </span>
              </label>
            );
          })}
        </div>
      )}
    </fieldset>
  );
}

function SaveErrors({ messages }: { messages: string[] }) {
  return (
    <div role="alert" className="flex gap-2 rounded-lg border border-destructive-border bg-destructive-soft/60 px-3 py-2">
      <TriangleAlert className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden />
      <ul className="space-y-1 text-sm text-destructive-soft-foreground">
        {messages.map((message) => (
          <li key={message}>{message}</li>
        ))}
      </ul>
    </div>
  );
}

function AdvancedCard({
  widget,
  onChange,
}: {
  widget: SitePageWidget;
  onChange: (next: SitePageWidget) => void;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Advanced</CardTitle>
        <CardDescription>
          Read in the template as <code>widget.css_class</code>, <code>widget.html_id</code>, and so on. Position is
          also set by dragging on the layout.
        </CardDescription>
      </CardHeader>
      <CardContent className="grid gap-5 md:grid-cols-3">
        <FormField label="CSS class" htmlFor="widget-css">
          {(control) => (
            <Input {...control} value={widget.cssClass} onChange={(event) => onChange({ ...widget, cssClass: event.target.value })} />
          )}
        </FormField>
        <FormField label="HTML id" htmlFor="widget-html-id">
          {(control) => (
            <Input {...control} value={widget.htmlId} onChange={(event) => onChange({ ...widget, htmlId: event.target.value })} />
          )}
        </FormField>
        <FormField label="Custom attributes" htmlFor="widget-attrs" hint={'Like data-theme="dark".'}>
          {(control) => (
            <Input
              {...control}
              className="font-mono"
              value={widget.customAttributes}
              onChange={(event) => onChange({ ...widget, customAttributes: event.target.value })}
            />
          )}
        </FormField>
        <PositionField
          id="widget-row"
          label="Row"
          value={widget.row}
          min={0}
          onChange={(row) => onChange({ ...widget, row })}
        />
        <PositionField
          id="widget-column"
          label="Column"
          hint="0 to 11."
          value={widget.column}
          min={0}
          max={11}
          onChange={(column) => onChange({ ...widget, column })}
        />
        <PositionField
          id="widget-column-span"
          label="Column span"
          hint="1 to 12."
          value={widget.columnSpan}
          min={1}
          max={12}
          onChange={(columnSpan) => onChange({ ...widget, columnSpan })}
        />
      </CardContent>
    </Card>
  );
}

function PositionField({
  id,
  label,
  hint,
  value,
  min,
  max,
  onChange,
}: {
  id: string;
  label: string;
  hint?: string;
  value: number;
  min: number;
  max?: number;
  onChange: (value: number) => void;
}) {
  return (
    <FormField label={label} hint={hint} htmlFor={id}>
      {(control) => (
        <Input
          {...control}
          type="number"
          inputMode="numeric"
          min={min}
          max={max}
          step={1}
          value={String(value)}
          onChange={(event) => {
            const parsed = Number.parseInt(event.target.value, 10);
            if (Number.isFinite(parsed)) {
              onChange(Math.min(max ?? Number.MAX_SAFE_INTEGER, Math.max(min, parsed)));
            }
          }}
        />
      )}
    </FormField>
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

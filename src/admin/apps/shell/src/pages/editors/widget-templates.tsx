import { adminApi, formatError, hasPermission, platformPermissions, problemFieldErrors } from "@raytha/api";
import type { TemplateRevision, WidgetField, WidgetFieldTypeOption, WidgetTemplateDetail } from "@raytha/api";
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Button,
  DangerZone,
  EmptyState,
  FormField,
  Input,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { Inbox, Plus, TriangleAlert } from "lucide-react";
import { useState, type FormEvent, type ReactNode } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { ListBackLink } from "../../components/list-back-link";
import { toDeveloperName } from "../entity";
import { defaultSettings, type WidgetSettings } from "../site-pages/models";
import { WidgetSettingsForm } from "../site-pages/widget-settings-form";
import { widgetTemplateVariables } from "./liquid-catalog";
import { RevisionsPanel } from "./revisions-panel";
import { TemplateWorkbench } from "./template-workbench";
import { ThemeSectionHeader, useThemeSummary } from "./theme-section-header";
import { WidgetFieldsEditor } from "./widget-fields-editor";
import { emptyFieldDraft, fieldDrafts, fieldsPayload, withLabel, type WidgetFieldDraft } from "./widget-fields-model";

const STARTER_CONTENT = `<section class="py-5{% if widget.css_class %} {{ widget.css_class }}{% endif %}"{% if widget.html_id %} id="{{ widget.html_id }}"{% endif %} {{ widget.custom_attributes }}>
  <div class="container">
    <h2>{{ widget.settings.headline }}</h2>
  </div>
</section>
`;

type EditorTab = "template" | "fields";

function useThemeParam(): string {
  const params = useParams({ strict: false });
  return "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
}

function useWidgetFieldTypes() {
  return useQuery({
    queryKey: ["widget-field-types"],
    queryFn: () => adminApi.themes.widgetFieldTypes(),
    staleTime: Number.POSITIVE_INFINITY,
  });
}

export function WidgetTemplatesListPage() {
  const themeId = useThemeParam();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const theme = useThemeSummary(themeId);
  const canEdit = hasPermission(platformPermissions.templates);

  const query = useQuery({
    queryKey: ["widget-templates", themeId, search],
    queryFn: () => adminApi.widgetTemplates(themeId).list({ search: search || undefined, pageSize: 50 }),
    enabled: themeId.length > 0,
    placeholderData: keepPreviousData,
  });

  const reset = useMutation({
    mutationFn: () => adminApi.widgetTemplates(themeId).reset(),
    onSuccess: () => {
      toast.success("Widget templates reset to defaults");
      void queryClient.invalidateQueries({ queryKey: ["widget-templates", themeId] });
      void queryClient.invalidateQueries({ queryKey: ["widget-template", themeId] });
      void queryClient.invalidateQueries({ queryKey: ["site-pages", "widget-definitions"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  useDocumentTitle(["Widget templates", theme?.title ?? "Theme"]);

  return (
    <div className="space-y-6">
      <ThemeSectionHeader
        themeId={themeId}
        active="widget"
        actions={
          canEdit ? (
            <Link
              to="/themes/$themeId/widget-templates/new"
              params={{ themeId }}
              className="inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              <Plus className="size-4" aria-hidden />
              New widget template
            </Link>
          ) : null
        }
      />
      <QueryGate query={query}>
        {(data) => (
          <ListPanel
            toolbar={
              <>
                <ListSearch
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search templates"
                  aria-label="Search widget templates"
                />
                <ListStatus total={data.totalCount} page={data.pageNumber} noun="templates" />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState
                icon={Inbox}
                title="No widget templates"
                hint={search ? "Nothing matches that search." : "Create one to give site pages a new kind of widget."}
              />
            ) : (
              <Table flush aria-label="Widget templates">
                <TableHeader>
                  <TableRow>
                    <TableHead>Label</TableHead>
                    <TableHead>Developer name</TableHead>
                    <TableHead>Fields</TableHead>
                    <TableHead>Type</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <Link
                          to="/themes/$themeId/widget-templates/$id"
                          params={{ themeId, id: item.id }}
                          className="font-medium text-primary hover:underline"
                        >
                          {item.label || item.id}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <code className="text-xs text-muted-foreground">{item.developerName || "—"}</code>
                      </TableCell>
                      <TableCell className="tabular-nums text-muted-foreground">{item.fields.length}</TableCell>
                      <TableCell>
                        {item.isBuiltInTemplate ? (
                          <Badge variant="secondary">Built-in</Badge>
                        ) : (
                          <span className="text-sm text-muted-foreground">Custom</span>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>
      {canEdit ? (
        <DangerZone
          description="Replace the markup and fields of every built-in widget template in this theme with the defaults. Custom templates are untouched, and each built-in keeps a revision you can revert to."
          actionLabel="Reset to defaults"
          confirmTitle="Reset widget templates?"
          confirmBody="This replaces each built-in widget template's Liquid and its fields with the defaults, and keeps a revision of each. Settings already saved on widgets are not changed."
          confirmLabel="Reset"
          onConfirm={() => reset.mutate()}
          pending={reset.isPending}
        />
      ) : null}
    </div>
  );
}

function WidgetTemplatesBackLink({ themeId }: { themeId: string }) {
  return (
    <ListBackLink
      to="/themes/$themeId/widget-templates"
      params={{ themeId }}
      listKey={`widget-templates:${themeId}`}
      label="widget templates"
    />
  );
}

export function WidgetTemplateEditorPage() {
  const themeId = useThemeParam();
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["widget-template", themeId, id],
    queryFn: () => adminApi.widgetTemplates(themeId).get(id),
    enabled: themeId.length > 0 && id.length > 0,
  });
  const revisions = useQuery({
    queryKey: ["widget-template-revisions", themeId, id],
    queryFn: () => adminApi.widgetTemplates(themeId).revisions(id, { pageSize: 50 }),
    enabled: themeId.length > 0 && id.length > 0,
  });
  const typeOptions = useWidgetFieldTypes();

  useDocumentTitle([query.data?.label ?? "Widget template"]);

  if (!themeId || !id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Widget template" />
        <p className="text-sm text-muted-foreground">Pick a template from the list.</p>
      </div>
    );
  }

  return (
    <QueryGate query={query}>
      {(template) => (
        <WidgetTemplateEditor
          themeId={themeId}
          template={template}
          revisions={revisions.data?.items ?? []}
          typeOptions={typeOptions.data ?? []}
        />
      )}
    </QueryGate>
  );
}

function WidgetTemplateEditor({
  themeId,
  template,
  revisions,
  typeOptions,
}: {
  themeId: string;
  template: WidgetTemplateDetail;
  revisions: TemplateRevision[];
  typeOptions: WidgetFieldTypeOption[];
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [label, setLabel] = useState(template.label);
  const [content, setContent] = useState(template.content);
  const [fields, setFields] = useState(() => fieldDrafts(template.fields));
  const [tab, setTab] = useState<EditorTab>("template");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [deleteError, setDeleteError] = useState("");

  const [synced, setSynced] = useState(template);
  if (synced !== template) {
    setSynced(template);
    setLabel(template.label);
    setContent(template.content);
    setFields(fieldDrafts(template.fields));
  }

  const payload = fieldsPayload(fields, typeOptions);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["widget-template", themeId, template.id] });
    void queryClient.invalidateQueries({ queryKey: ["widget-template-revisions", themeId, template.id] });
    void queryClient.invalidateQueries({ queryKey: ["widget-templates", themeId] });
    void queryClient.invalidateQueries({ queryKey: ["site-pages", "widget-definitions"] });
  };

  const save = useMutation({
    mutationFn: () => adminApi.widgetTemplates(themeId).update(template.id, { label, content, fields: payload }),
    onSuccess: () => {
      setErrors({});
      toast.success("Widget template saved");
      invalidate();
    },
    onError: (error) => {
      const found = problemFieldErrors(error);
      setErrors(found);
      if (found.Fields && !found.Content && !found.Label) {
        setTab("fields");
      }
      toast.error(formatError(error));
    },
  });

  const revert = useMutation({
    mutationFn: (revisionId: string) => adminApi.widgetTemplates(themeId).revert(revisionId),
    onSuccess: () => {
      toast.success("Reverted to that revision, fields included");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const remove = useMutation({
    mutationFn: () => adminApi.widgetTemplates(themeId).remove(template.id),
    onSuccess: () => {
      toast.success("Widget template deleted");
      void queryClient.invalidateQueries({ queryKey: ["widget-templates", themeId] });
      void queryClient.invalidateQueries({ queryKey: ["site-pages", "widget-definitions"] });
      void navigate({ to: "/themes/$themeId/widget-templates", params: { themeId } });
    },
    onError: (error) => {
      const message = formatError(error);
      setDeleteError(message);
      toast.error(message);
    },
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    save.mutate();
  };

  return (
    <div className="space-y-6">
      <PageHeader
        back={<WidgetTemplatesBackLink themeId={themeId} />}
        title={template.label || template.developerName || "Widget template"}
        meta={
          <>
            {template.developerName ? <code>{template.developerName}</code> : null}
            {template.isBuiltInTemplate ? <Badge variant="secondary">Built-in</Badge> : <Badge variant="secondary">Custom</Badge>}
          </>
        }
        actions={
          <Button type="submit" form="widget-template-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <WidgetTemplateWorkspace
        tab={tab}
        onTab={setTab}
        title={template.developerName || "Widget"}
        content={content}
        onContent={setContent}
        fields={fields}
        onFields={setFields}
        payload={payload}
        typeOptions={typeOptions}
        themeId={themeId}
        errors={errors}
        onSave={() => save.mutate()}
      />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <Card>
          <CardHeader>
            <CardTitle>Settings</CardTitle>
          </CardHeader>
          <CardContent>
            <form id="widget-template-form" className="space-y-5" onSubmit={handleSubmit}>
              <FormField label="Label" required htmlFor="widget-label" error={errors.Label}>
                {(control) => <Input {...control} value={label} onChange={(event) => setLabel(event.target.value)} />}
              </FormField>
            </form>
          </CardContent>
        </Card>
        <RevisionsPanel
          revisions={revisions}
          pendingId={revert.isPending ? (revert.variables ?? null) : null}
          onRevert={(revisionId) => revert.mutate(revisionId)}
        />
      </div>
      {!template.isBuiltInTemplate && hasPermission(platformPermissions.templates) ? (
        <div className="space-y-3">
          <DangerZone
            description="Delete this widget template. Pages that still use it must have those widgets removed first."
            actionLabel="Delete template"
            confirmTitle={`Delete ${template.label || template.developerName}?`}
            confirmBody="The template, its fields, and its revisions are removed. This cannot be undone."
            confirmLabel="Delete"
            onConfirm={() => {
              setDeleteError("");
              remove.mutate();
            }}
            pending={remove.isPending}
          />
          {deleteError ? <Problem>{deleteError}</Problem> : null}
        </div>
      ) : null}
    </div>
  );
}

export function NewWidgetTemplatePage() {
  const themeId = useThemeParam();
  useDocumentTitle(["New widget template"]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const typeOptions = useWidgetFieldTypes();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [content, setContent] = useState(STARTER_CONTENT);
  const [fields, setFields] = useState<WidgetFieldDraft[]>(() => [withLabel(emptyFieldDraft(), "Headline")]);
  const [tab, setTab] = useState<EditorTab>("fields");
  const [errors, setErrors] = useState<Record<string, string>>({});

  const options = typeOptions.data ?? [];
  const payload = fieldsPayload(fields, options);

  const create = useMutation({
    mutationFn: () => adminApi.widgetTemplates(themeId).create({ label, developerName, content, fields: payload }),
    onSuccess: (result) => {
      toast.success("Widget template created");
      void queryClient.invalidateQueries({ queryKey: ["widget-templates", themeId] });
      void queryClient.invalidateQueries({ queryKey: ["site-pages", "widget-definitions"] });
      void navigate({ to: "/themes/$themeId/widget-templates/$id", params: { themeId, id: result.id } });
    },
    onError: (error) => {
      const found = problemFieldErrors(error);
      setErrors(found);
      if (found.Fields && !found.Content) {
        setTab("fields");
      }
      toast.error(formatError(error));
    },
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={<WidgetTemplatesBackLink themeId={themeId} />}
        title="New widget template"
        description="A widget template is a Liquid snippet plus the fields editors fill in when they add it to a page."
        actions={
          <Button type="submit" form="new-widget-template-form" loading={create.isPending}>
            Create
          </Button>
        }
      />
      <Card>
        <CardHeader>
          <CardTitle>Settings</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            id="new-widget-template-form"
            className="grid gap-5 sm:grid-cols-2"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              create.mutate();
            }}
          >
            <FormField label="Label" required htmlFor="new-widget-label" error={errors.Label}>
              {(control) => (
                <Input
                  {...control}
                  value={label}
                  onChange={(event) => {
                    setLabel(event.target.value);
                    if (!developerTouched) {
                      setDeveloperName(toDeveloperName(event.target.value));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField
              label="Developer name"
              required
              htmlFor="new-widget-developer-name"
              hint="The widget type pages store. Lowercase with underscores; it cannot change later."
              error={errors.DeveloperName}
            >
              {(control) => (
                <Input
                  {...control}
                  className="font-mono"
                  value={developerName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setDeveloperName(event.target.value);
                  }}
                />
              )}
            </FormField>
          </form>
        </CardContent>
      </Card>
      <WidgetTemplateWorkspace
        tab={tab}
        onTab={setTab}
        title={developerName || "New widget"}
        content={content}
        onContent={setContent}
        fields={fields}
        onFields={setFields}
        payload={payload}
        typeOptions={options}
        themeId={themeId}
        errors={errors}
        onSave={() => create.mutate()}
      />
    </div>
  );
}

function WidgetTemplateWorkspace({
  tab,
  onTab,
  title,
  content,
  onContent,
  fields,
  onFields,
  payload,
  typeOptions,
  themeId,
  errors,
  onSave,
}: {
  tab: EditorTab;
  onTab: (tab: EditorTab) => void;
  title: string;
  content: string;
  onContent: (content: string) => void;
  fields: WidgetFieldDraft[];
  onFields: (fields: WidgetFieldDraft[]) => void;
  payload: WidgetField[];
  typeOptions: WidgetFieldTypeOption[];
  themeId: string;
  errors: Record<string, string>;
  onSave: () => void;
}) {
  return (
    <Tabs value={tab} onValueChange={(next) => onTab(next === "fields" ? "fields" : "template")} className="space-y-4">
      <TabsList aria-label="Widget template">
        <TabsTrigger value="template">Liquid</TabsTrigger>
        <TabsTrigger value="fields">
          Fields
          <span className="ml-1.5 rounded-full bg-muted px-1.5 text-[11px] tabular-nums text-muted-foreground">
            {fields.length}
          </span>
          {errors.Fields ? <span className="ml-1.5 size-1.5 rounded-full bg-destructive" aria-label="has errors" /> : null}
        </TabsTrigger>
      </TabsList>
      <TabsContent value="template" className="space-y-3">
        {errors.Content ? <Problem>{errors.Content}</Problem> : null}
        <TemplateWorkbench
          title={title}
          value={content}
          onChange={onContent}
          ariaLabel="Widget template content"
          variables={widgetTemplateVariables(payload)}
          themeId={themeId}
          onSave={onSave}
        />
      </TabsContent>
      <TabsContent value="fields">
        <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_24rem]">
          <Card>
            <CardHeader>
              <CardTitle>Fields</CardTitle>
              <CardDescription>
                The settings form editors fill in, in this order. Each field is read in Liquid as{" "}
                <code>widget.settings.developerName</code>.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {errors.Fields ? <Problem>{errors.Fields}</Problem> : null}
              <WidgetFieldsEditor fields={fields} onChange={onFields} typeOptions={typeOptions} />
            </CardContent>
          </Card>
          <Card className="xl:sticky xl:top-4">
            <CardHeader>
              <CardTitle>Form preview</CardTitle>
              <CardDescription>What editors see when they add this widget, defaults filled in.</CardDescription>
            </CardHeader>
            <CardContent>
              <FormPreview key={JSON.stringify(payload)} fields={payload} />
            </CardContent>
          </Card>
        </div>
      </TabsContent>
    </Tabs>
  );
}

function FormPreview({ fields }: { fields: WidgetField[] }) {
  const [settings, setSettings] = useState<WidgetSettings>(() => defaultSettings(fields));
  return (
    <WidgetSettingsForm
      widgetType="preview"
      definition={{
        developerName: "preview",
        displayName: "Preview",
        description: "",
        iconClass: "",
        isBuiltInTemplate: false,
        fields: fields.filter((field) => field.developerName !== ""),
      }}
      settings={settings}
      onChange={setSettings}
      idPrefix="preview-"
    />
  );
}

function Problem({ children }: { children: ReactNode }) {
  return (
    <p
      role="alert"
      className="flex gap-2 rounded-lg border border-destructive-border bg-destructive-soft/60 px-3 py-2 text-sm text-destructive-soft-foreground"
    >
      <TriangleAlert className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden />
      <span>{children}</span>
    </p>
  );
}

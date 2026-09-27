import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { TemplateRevision, WidgetTemplateDetail } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
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
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { ListBackLink } from "../../components/list-back-link";
import { RevisionsPanel } from "./revisions-panel";
import { TemplateWorkbench } from "./template-workbench";
import { ThemeSectionHeader, useThemeSummary } from "./theme-section-header";

export function WidgetTemplatesListPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const theme = useThemeSummary(themeId);

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
    },
    onError: (error) => toast.error(formatError(error)),
  });

  useDocumentTitle(["Widget templates", theme?.title ?? "Theme"]);

  return (
    <div className="space-y-6">
      <ThemeSectionHeader themeId={themeId} active="widget" />
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
                hint={search ? "Nothing matches that search." : "No widget templates yet."}
              />
            ) : (
              <Table flush aria-label="Widget templates">
                <TableHeader>
                  <TableRow>
                    <TableHead>Label</TableHead>
                    <TableHead>Developer name</TableHead>
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
      {hasPermission(platformPermissions.templates) ? (
        <DangerZone
          description="Replace every built-in widget template in this theme with the default markup. Each template keeps a revision you can revert to."
          actionLabel="Reset to defaults"
          confirmTitle="Reset widget templates?"
          confirmBody="This replaces each built-in widget template with the default markup and keeps a revision."
          confirmLabel="Reset"
          onConfirm={() => reset.mutate()}
          pending={reset.isPending}
        />
      ) : null}
    </div>
  );
}

export function WidgetTemplateEditorPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
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
        <WidgetTemplateEditor themeId={themeId} template={template} revisions={revisions.data?.items ?? []} />
      )}
    </QueryGate>
  );
}

function WidgetTemplateEditor({
  themeId,
  template,
  revisions,
}: {
  themeId: string;
  template: WidgetTemplateDetail;
  revisions: TemplateRevision[];
}) {
  const queryClient = useQueryClient();
  const [label, setLabel] = useState(template.label);
  const [content, setContent] = useState(template.content);

  const [synced, setSynced] = useState(template);
  if (synced !== template) {
    setSynced(template);
    setLabel(template.label);
    setContent(template.content);
  }

  const save = useMutation({
    mutationFn: () => adminApi.widgetTemplates(themeId).update(template.id, { label, content }),
    onSuccess: () => {
      toast.success("Widget template saved");
      void queryClient.invalidateQueries({ queryKey: ["widget-template", themeId, template.id] });
      void queryClient.invalidateQueries({ queryKey: ["widget-template-revisions", themeId, template.id] });
      void queryClient.invalidateQueries({ queryKey: ["widget-templates", themeId] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const revert = useMutation({
    mutationFn: (revisionId: string) => adminApi.widgetTemplates(themeId).revert(revisionId),
    onSuccess: () => {
      toast.success("Reverted to that revision");
      void queryClient.invalidateQueries({ queryKey: ["widget-template", themeId, template.id] });
      void queryClient.invalidateQueries({ queryKey: ["widget-template-revisions", themeId, template.id] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    save.mutate();
  };

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <ListBackLink
            to="/themes/$themeId/widget-templates"
            params={{ themeId }}
            listKey={`widget-templates:${themeId}`}
            label="widget templates"
          />
        }
        title={template.label || template.developerName || "Widget template"}
        meta={
          <>
            {template.developerName ? <code>{template.developerName}</code> : null}
            {template.isBuiltInTemplate ? <Badge variant="secondary">Built-in</Badge> : null}
          </>
        }
        actions={
          <Button type="submit" form="widget-template-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <TemplateWorkbench
        title={template.developerName || "Widget"}
        value={content}
        onChange={setContent}
        ariaLabel="Widget template content"
        themeId={themeId}
        onSave={() => save.mutate()}
      />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <Card>
          <CardHeader>
            <CardTitle>Settings</CardTitle>
          </CardHeader>
          <CardContent>
            <form id="widget-template-form" className="space-y-5" onSubmit={handleSubmit}>
              <FormField label="Label" required htmlFor="widget-label">
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
    </div>
  );
}

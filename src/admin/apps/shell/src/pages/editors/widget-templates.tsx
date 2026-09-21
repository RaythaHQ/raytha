import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { TemplateRevision, WidgetTemplateDetail } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  ConfirmDialog,
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
import { useEffect, useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { CodeEditor } from "./code-editor";
import { RevisionsPanel } from "./revisions-panel";

export function WidgetTemplatesListPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [resetOpen, setResetOpen] = useState(false);

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
      setResetOpen(false);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  useDocumentTitle(["Widget templates"]);

  if (!themeId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Widget templates" />
        <p className="text-sm text-muted-foreground">Pick a theme first.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Widget templates"
        description="Built-in widget markup for this theme."
        actions={
          hasPermission(platformPermissions.templates) ? (
            <Button type="button" variant="outline" onClick={() => setResetOpen(true)}>
              Reset to defaults
            </Button>
          ) : undefined
        }
      />
      <p className="text-sm">
        <Link to="/themes" className="text-primary hover:underline">
          Back to themes
        </Link>
        {" · "}
        <Link to="/themes/$themeId/web-templates" params={{ themeId }} className="text-primary hover:underline">
          Web templates
        </Link>
      </p>
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
                    <TableHead>Built-in</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <Link
                          to="/themes/$themeId/widget-templates/$id"
                          params={{ themeId, id: item.id }}
                          className="text-primary hover:underline"
                        >
                          {item.label || item.id}
                        </Link>
                      </TableCell>
                      <TableCell>{item.developerName || "—"}</TableCell>
                      <TableCell>
                        {item.isBuiltInTemplate ? <Badge variant="secondary">Built-in</Badge> : "—"}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>
      <ConfirmDialog
        open={resetOpen}
        onOpenChange={setResetOpen}
        title="Reset widget templates?"
        body="This replaces each built-in widget template with the default markup and keeps a revision."
        confirmLabel="Reset"
        onConfirm={() => reset.mutate()}
        pending={reset.isPending}
      />
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

  useEffect(() => {
    setLabel(template.label);
    setContent(template.content);
  }, [template]);

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
        title={template.label || template.developerName || "Widget template"}
        description={template.developerName}
        actions={
          <Button type="submit" form="widget-template-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <p className="text-sm">
        <Link to="/themes/$themeId/widget-templates" params={{ themeId }} className="text-primary hover:underline">
          Back to widget templates
        </Link>
      </p>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <Card>
          <CardContent className="space-y-4 pt-6">
            <form id="widget-template-form" className="space-y-4" onSubmit={handleSubmit}>
              <FormField label="Label" required htmlFor="widget-label">
                {(control) => <Input {...control} value={label} onChange={(event) => setLabel(event.target.value)} />}
              </FormField>
              <CodeEditor value={content} onChange={setContent} language="liquid" ariaLabel="Widget template content" />
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

import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { JsonObject, TemplateRevision, WebTemplateDetail } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  ConfirmDialog,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  FormField,
  Input,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, readString, toDeveloperName } from "../entity";
import { CodeEditor } from "./code-editor";
import { RevisionsPanel } from "./revisions-panel";

export function WebTemplatesListPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["web-templates", themeId, search],
    queryFn: () => adminApi.webTemplates(themeId).list({ search: search || undefined, pageSize: 50 }),
    enabled: themeId.length > 0,
    placeholderData: keepPreviousData,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminApi.webTemplates(themeId).remove(id),
    onSuccess: () => {
      toast.success("Web template deleted");
      void queryClient.invalidateQueries({ queryKey: ["web-templates", themeId] });
      setDeleteId(null);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  useDocumentTitle(["Web templates"]);

  if (!themeId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Web templates" />
        <p className="text-sm text-muted-foreground">Pick a theme first.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Web templates"
        description="HTML + Liquid templates for this theme."
        actions={
          hasPermission(platformPermissions.templates) ? (
            <Button type="button" onClick={() => setCreateOpen(true)}>
              New template
            </Button>
          ) : undefined
        }
      />
      <p className="text-sm">
        <Link to="/themes" className="text-primary hover:underline">
          Back to themes
        </Link>
        {" · "}
        <Link to="/themes/$themeId/widget-templates" params={{ themeId }} className="text-primary hover:underline">
          Widget templates
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
                  aria-label="Search templates"
                />
                <ListStatus total={data.totalCount} page={data.pageNumber} noun="templates" />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState
                icon={Inbox}
                title="No web templates"
                hint={search ? "Nothing matches that search." : "No web templates yet."}
              />
            ) : (
              <Table flush aria-label="Web templates">
                <TableHeader>
                  <TableRow>
                    <TableHead>Label</TableHead>
                    <TableHead>Developer name</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead className="w-40">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <Link
                          to="/themes/$themeId/web-templates/$id"
                          params={{ themeId, id: item.id }}
                          className="text-primary hover:underline"
                        >
                          {item.label || item.id}
                        </Link>
                      </TableCell>
                      <TableCell>{item.developerName || "—"}</TableCell>
                      <TableCell>
                        {item.isBaseLayout ? <Badge variant="info">Base layout</Badge> : "Template"}
                      </TableCell>
                      <TableCell>
                        <Button type="button" variant="ghost" size="sm" onClick={() => setDeleteId(item.id)}>
                          Delete
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>
      <CreateWebTemplateDialog
        themeId={themeId}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={(id) => {
          void navigate({ to: "/themes/$themeId/web-templates/$id", params: { themeId, id } });
        }}
      />
      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteId(null);
          }
        }}
        title="Delete web template?"
        body="This cannot be undone."
        onConfirm={() => {
          if (deleteId) {
            deleteMutation.mutate(deleteId);
          }
        }}
        pending={deleteMutation.isPending}
      />
    </div>
  );
}

export function WebTemplateEditorPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["web-template", themeId, id],
    queryFn: () => adminApi.webTemplates(themeId).get(id),
    enabled: themeId.length > 0 && id.length > 0,
  });
  const revisions = useQuery({
    queryKey: ["web-template-revisions", themeId, id],
    queryFn: () => adminApi.webTemplates(themeId).revisions(id, { pageSize: 50 }),
    enabled: themeId.length > 0 && id.length > 0,
  });

  useDocumentTitle([query.data?.label ?? "Web template"]);

  if (!themeId || !id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Web template" />
        <p className="text-sm text-muted-foreground">Pick a template from the list.</p>
      </div>
    );
  }

  return (
    <QueryGate query={query}>
      {(template) => (
        <WebTemplateEditor themeId={themeId} template={template} revisions={revisions.data?.items ?? []} />
      )}
    </QueryGate>
  );
}

function WebTemplateEditor({
  themeId,
  template,
  revisions,
}: {
  themeId: string;
  template: WebTemplateDetail;
  revisions: TemplateRevision[];
}) {
  const queryClient = useQueryClient();
  const [label, setLabel] = useState(template.label);
  const [content, setContent] = useState(template.content);
  const [isBaseLayout, setIsBaseLayout] = useState(template.isBaseLayout);
  const [parentTemplateId, setParentTemplateId] = useState(template.parentTemplateId ?? "");
  const [allowAccessForNewContentTypes, setAllowAccessForNewContentTypes] = useState(
    template.allowAccessForNewContentTypes,
  );
  const [accessIds, setAccessIds] = useState<string[]>(template.templateAccessToModelDefinitions);

  useEffect(() => {
    setLabel(template.label);
    setContent(template.content);
    setIsBaseLayout(template.isBaseLayout);
    setParentTemplateId(template.parentTemplateId ?? "");
    setAllowAccessForNewContentTypes(template.allowAccessForNewContentTypes);
    setAccessIds(template.templateAccessToModelDefinitions);
  }, [template]);

  const layouts = useQuery({
    queryKey: ["web-templates", themeId, "base-layouts"],
    queryFn: () => adminApi.webTemplates(themeId).list({ baseLayoutsOnly: true, pageSize: 100 }),
  });
  const contentTypes = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 100 }),
  });

  const save = useMutation({
    mutationFn: () => {
      const payload: JsonObject = {
        label,
        content,
        isBaseLayout,
        parentTemplateId: parentTemplateId.length > 0 ? parentTemplateId : null,
        allowAccessForNewContentTypes,
        templateAccessToModelDefinitions: accessIds,
      };
      return adminApi.webTemplates(themeId).update(template.id, payload);
    },
    onSuccess: () => {
      toast.success("Web template saved");
      void queryClient.invalidateQueries({ queryKey: ["web-template", themeId, template.id] });
      void queryClient.invalidateQueries({ queryKey: ["web-template-revisions", themeId, template.id] });
      void queryClient.invalidateQueries({ queryKey: ["web-templates", themeId] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const revert = useMutation({
    mutationFn: (revisionId: string) => adminApi.webTemplates(themeId).revert(revisionId),
    onSuccess: () => {
      toast.success("Reverted to that revision");
      void queryClient.invalidateQueries({ queryKey: ["web-template", themeId, template.id] });
      void queryClient.invalidateQueries({ queryKey: ["web-template-revisions", themeId, template.id] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    save.mutate();
  };

  const parentOptions = (layouts.data?.items ?? []).filter((item) => item.id !== template.id);

  return (
    <div className="space-y-6">
      <PageHeader
        title={template.label || template.developerName || "Web template"}
        description={template.developerName}
        actions={
          <Button type="submit" form="web-template-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <p className="text-sm">
        <Link to="/themes/$themeId/web-templates" params={{ themeId }} className="text-primary hover:underline">
          Back to web templates
        </Link>
      </p>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <Card>
          <CardContent className="space-y-4 pt-6">
            <form id="web-template-form" className="space-y-4" onSubmit={handleSubmit}>
              <FormField label="Label" required htmlFor="web-label">
                {(control) => <Input {...control} value={label} onChange={(event) => setLabel(event.target.value)} />}
              </FormField>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="web-base-layout"
                  checked={isBaseLayout}
                  onCheckedChange={setIsBaseLayout}
                  disabled={template.isBuiltInTemplate}
                />
                <label htmlFor="web-base-layout" className="text-sm">
                  Base layout
                </label>
              </div>
              <FormField label="Parent template" htmlFor="web-parent">
                {(control) => (
                  <Select
                    {...control}
                    value={parentTemplateId}
                    onChange={(event) => setParentTemplateId(event.target.value)}
                  >
                    <option value="">None</option>
                    {parentOptions.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.label || item.developerName || item.id}
                      </option>
                    ))}
                  </Select>
                )}
              </FormField>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="web-allow-new"
                  checked={allowAccessForNewContentTypes}
                  onCheckedChange={setAllowAccessForNewContentTypes}
                />
                <label htmlFor="web-allow-new" className="text-sm">
                  Allow access for new content types
                </label>
              </div>
              <fieldset className="space-y-2">
                <legend className="text-sm font-medium">Content types</legend>
                {(contentTypes.data?.items ?? []).map((type) => {
                  const fields = entityFields(type);
                  const typeLabel = readString(fields, "labelPlural", "labelSingular", "developerName") || type.id;
                  const checked = accessIds.includes(type.id);
                  return (
                    <div key={type.id} className="flex items-center gap-2">
                      <Checkbox
                        id={`web-access-${type.id}`}
                        checked={checked}
                        onCheckedChange={(next) => {
                          setAccessIds((current) =>
                            next ? [...current, type.id] : current.filter((item) => item !== type.id),
                          );
                        }}
                      />
                      <label htmlFor={`web-access-${type.id}`} className="text-sm">
                        {typeLabel}
                      </label>
                    </div>
                  );
                })}
              </fieldset>
              <CodeEditor value={content} onChange={setContent} language="liquid" ariaLabel="Web template content" />
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

function CreateWebTemplateDialog({
  themeId,
  open,
  onOpenChange,
  onCreated,
}: {
  themeId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (id: string) => void;
}) {
  const queryClient = useQueryClient();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [content, setContent] = useState("{% renderbody %}");
  const [isBaseLayout, setIsBaseLayout] = useState(false);
  const [parentTemplateId, setParentTemplateId] = useState("");
  const [allowAccessForNewContentTypes, setAllowAccessForNewContentTypes] = useState(true);
  const [accessIds, setAccessIds] = useState<string[]>([]);

  const layouts = useQuery({
    queryKey: ["web-templates", themeId, "base-layouts"],
    queryFn: () => adminApi.webTemplates(themeId).list({ baseLayoutsOnly: true, pageSize: 100 }),
    enabled: open,
  });
  const contentTypes = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 100 }),
    enabled: open,
  });

  const reset = () => {
    setLabel("");
    setDeveloperName("");
    setDeveloperTouched(false);
    setContent("{% renderbody %}");
    setIsBaseLayout(false);
    setParentTemplateId("");
    setAllowAccessForNewContentTypes(true);
    setAccessIds([]);
  };

  const create = useMutation({
    mutationFn: () => {
      const payload: JsonObject = {
        label,
        developerName,
        content,
        isBaseLayout,
        parentTemplateId: parentTemplateId.length > 0 ? parentTemplateId : null,
        allowAccessForNewContentTypes,
        templateAccessToModelDefinitions: accessIds,
      };
      return adminApi.webTemplates(themeId).create(payload);
    },
    onSuccess: (result) => {
      toast.success("Web template created");
      void queryClient.invalidateQueries({ queryKey: ["web-templates", themeId] });
      onOpenChange(false);
      reset();
      onCreated(result.id);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    create.mutate();
  };

  return (
    <Dialog
      open={open}
      widthClassName="max-w-2xl"
      onOpenChange={(next) => {
        onOpenChange(next);
        if (!next) {
          reset();
        }
      }}
    >
      <form onSubmit={handleSubmit}>
        <DialogHeader>
          <DialogTitle>New web template</DialogTitle>
        </DialogHeader>
        <DialogContent className="space-y-4">
          <FormField label="Label" required htmlFor="new-web-label">
            {(control) => (
              <Input
                {...control}
                value={label}
                onChange={(event) => {
                  const next = event.target.value;
                  setLabel(next);
                  if (!developerTouched) {
                    setDeveloperName(toDeveloperName(next));
                  }
                }}
              />
            )}
          </FormField>
          <FormField label="Developer name" required htmlFor="new-web-developer">
            {(control) => (
              <Input
                {...control}
                value={developerName}
                onChange={(event) => {
                  setDeveloperTouched(true);
                  setDeveloperName(event.target.value);
                }}
              />
            )}
          </FormField>
          <div className="flex items-center gap-2">
            <Checkbox id="new-web-base" checked={isBaseLayout} onCheckedChange={setIsBaseLayout} />
            <label htmlFor="new-web-base" className="text-sm">
              Base layout
            </label>
          </div>
          <FormField label="Parent template" htmlFor="new-web-parent">
            {(control) => (
              <Select {...control} value={parentTemplateId} onChange={(event) => setParentTemplateId(event.target.value)}>
                <option value="">None</option>
                {(layouts.data?.items ?? []).map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.label || item.developerName || item.id}
                  </option>
                ))}
              </Select>
            )}
          </FormField>
          <div className="flex items-center gap-2">
            <Checkbox
              id="new-web-allow"
              checked={allowAccessForNewContentTypes}
              onCheckedChange={setAllowAccessForNewContentTypes}
            />
            <label htmlFor="new-web-allow" className="text-sm">
              Allow access for new content types
            </label>
          </div>
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">Content types</legend>
            {(contentTypes.data?.items ?? []).map((type) => {
              const fields = entityFields(type);
              const typeLabel = readString(fields, "labelPlural", "labelSingular", "developerName") || type.id;
              return (
                <div key={type.id} className="flex items-center gap-2">
                  <Checkbox
                    id={`new-web-access-${type.id}`}
                    checked={accessIds.includes(type.id)}
                    onCheckedChange={(next) => {
                      setAccessIds((current) =>
                        next ? [...current, type.id] : current.filter((item) => item !== type.id),
                      );
                    }}
                  />
                  <label htmlFor={`new-web-access-${type.id}`} className="text-sm">
                    {typeLabel}
                  </label>
                </div>
              );
            })}
          </fieldset>
          <CodeEditor value={content} onChange={setContent} language="liquid" ariaLabel="New web template content" />
        </DialogContent>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="submit" loading={create.isPending}>
            Create
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  );
}

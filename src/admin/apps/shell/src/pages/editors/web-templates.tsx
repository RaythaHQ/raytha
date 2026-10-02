import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { JsonObject, PagedResult, TemplateRevision, WebTemplateDetail } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  DangerZone,
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
import { Inbox, Plus, Star } from "lucide-react";
import { useState, type FormEvent, type ReactNode } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, readString, toDeveloperName } from "../entity";
import { RevisionsPanel } from "./revisions-panel";
import { TemplateWorkbench } from "./template-workbench";
import { ThemeSectionHeader, useThemeSummary } from "./theme-section-header";

export function WebTemplatesListPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  const [search, setSearch] = useState("");
  const theme = useThemeSummary(themeId);
  const queryClient = useQueryClient();

  const listKey = ["web-templates", themeId, search];
  const query = useQuery({
    queryKey: listKey,
    queryFn: () => adminApi.webTemplates(themeId).list({ search: search || undefined, pageSize: 50 }),
    enabled: themeId.length > 0,
    placeholderData: keepPreviousData,
  });
  const favorite = useFavoriteWebTemplate(themeId, {
    onMutate: async ({ id, setAsFavorite }) => {
      await queryClient.cancelQueries({ queryKey: listKey });
      const previous = queryClient.getQueryData<PagedResult<WebTemplateDetail>>(listKey);
      queryClient.setQueryData<PagedResult<WebTemplateDetail>>(listKey, (current) =>
        current
          ? {
              ...current,
              items: current.items.map((item) => (item.id === id ? { ...item, isFavorite: setAsFavorite } : item)),
            }
          : current,
      );
      return () => queryClient.setQueryData(listKey, previous);
    },
  });

  useDocumentTitle(["Web templates", theme?.title ?? "Theme"]);

  return (
    <div className="space-y-6">
      <ThemeSectionHeader
        themeId={themeId}
        active="web"
        actions={
          hasPermission(platformPermissions.templates) ? (
            <Link
              to="/themes/$themeId/web-templates/new"
              params={{ themeId }}
              className="inline-flex h-9 items-center gap-2 rounded-lg bg-primary px-3.5 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
            >
              <Plus className="size-4" aria-hidden />
              New template
            </Link>
          ) : undefined
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
                    <TableHead className="w-12 pr-0">
                      <span className="sr-only">Favorite</span>
                    </TableHead>
                    <TableHead>Label</TableHead>
                    <TableHead>Developer name</TableHead>
                    <TableHead>Type</TableHead>
                  </TableRow>
                </TableHeader>
                {favoriteGroups(data.items).map((group) => (
                  <TableBody key={group.title ?? "all"}>
                    {group.title ? (
                      <TableRow className="bg-muted/40 hover:bg-muted/40">
                        <th
                          scope="rowgroup"
                          colSpan={4}
                          className="px-4 py-1.5 text-left text-[11px] font-semibold uppercase tracking-wide text-muted-foreground"
                        >
                          {group.title}
                        </th>
                      </TableRow>
                    ) : null}
                    {group.items.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell className="w-12 py-1 pr-0">
                          <FavoriteStarButton
                            label={item.label || item.developerName || item.id}
                            isFavorite={item.isFavorite}
                            onToggle={() => favorite.mutate({ id: item.id, setAsFavorite: !item.isFavorite })}
                          />
                        </TableCell>
                        <TableCell>
                          <Link
                            to="/themes/$themeId/web-templates/$id"
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
                          <span className="flex flex-wrap gap-1.5">
                            {item.isBaseLayout ? <Badge variant="info">Base layout</Badge> : null}
                            {item.isBuiltInTemplate ? <Badge variant="secondary">Built-in</Badge> : null}
                            {!item.isBaseLayout && !item.isBuiltInTemplate ? (
                              <span className="text-sm text-muted-foreground">Custom</span>
                            ) : null}
                          </span>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                ))}
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>
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
  const navigate = useNavigate();
  const [label, setLabel] = useState(template.label);
  const [content, setContent] = useState(template.content);
  const [isBaseLayout, setIsBaseLayout] = useState(template.isBaseLayout);
  const [parentTemplateId, setParentTemplateId] = useState(template.parentTemplateId ?? "");
  const [allowAccessForNewContentTypes, setAllowAccessForNewContentTypes] = useState(
    template.allowAccessForNewContentTypes,
  );
  const [accessIds, setAccessIds] = useState<string[]>(template.templateAccessToModelDefinitions);
  const [isFavorite, setIsFavorite] = useState(template.isFavorite);

  const [synced, setSynced] = useState(template);
  if (synced !== template) {
    setSynced(template);
    setLabel(template.label);
    setContent(template.content);
    setIsBaseLayout(template.isBaseLayout);
    setParentTemplateId(template.parentTemplateId ?? "");
    setAllowAccessForNewContentTypes(template.allowAccessForNewContentTypes);
    setAccessIds(template.templateAccessToModelDefinitions);
    setIsFavorite(template.isFavorite);
  }

  const favorite = useFavoriteWebTemplate(themeId, {
    onMutate: ({ setAsFavorite }) => {
      const previous = isFavorite;
      setIsFavorite(setAsFavorite);
      return () => setIsFavorite(previous);
    },
  });

  const layouts = useQuery({
    queryKey: ["web-templates", themeId, "base-layouts"],
    queryFn: () => adminApi.webTemplates(themeId).list({ baseLayoutsOnly: true, pageSize: 100 }),
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

  const remove = useMutation({
    mutationFn: () => adminApi.webTemplates(themeId).remove(template.id),
    onSuccess: () => {
      toast.success("Web template deleted");
      void queryClient.invalidateQueries({ queryKey: ["web-templates", themeId] });
      void navigate({ to: "/themes/$themeId/web-templates", params: { themeId } });
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
        back={<WebTemplatesBackLink themeId={themeId} />}
        title={template.label || template.developerName || "Web template"}
        meta={
          <>
            {template.developerName ? <code>{template.developerName}</code> : null}
            {template.isBaseLayout ? <Badge variant="info">Base layout</Badge> : null}
            {template.isBuiltInTemplate ? <Badge variant="secondary">Built-in</Badge> : null}
          </>
        }
        actions={
          <>
            <Button
              type="button"
              variant="outline"
              aria-pressed={isFavorite}
              onClick={() => favorite.mutate({ id: template.id, setAsFavorite: !isFavorite })}
            >
              <Star className={isFavorite ? "fill-current text-warning" : ""} aria-hidden />
              {isFavorite ? "Favorited" : "Favorite"}
            </Button>
            <Button type="submit" form="web-template-form" loading={save.isPending}>
              Save
            </Button>
          </>
        }
      />
      <TemplateWorkbench
        title={template.developerName || "Template"}
        value={content}
        onChange={setContent}
        ariaLabel="Web template content"
        variables={template.availableVariables}
        themeId={themeId}
        onSave={() => save.mutate()}
      />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <Card>
          <CardHeader>
            <CardTitle>Settings</CardTitle>
          </CardHeader>
          <CardContent>
            <form id="web-template-form" className="space-y-5" onSubmit={handleSubmit}>
              <WebTemplateSettings
                idPrefix="web"
                label={label}
                onLabel={setLabel}
                isBaseLayout={isBaseLayout}
                onBaseLayout={setIsBaseLayout}
                baseLayoutLocked={template.isBuiltInTemplate}
                parentTemplateId={parentTemplateId}
                onParentTemplate={setParentTemplateId}
                parentOptions={parentOptions}
                allowAccessForNewContentTypes={allowAccessForNewContentTypes}
                onAllowAccessForNewContentTypes={setAllowAccessForNewContentTypes}
                accessIds={accessIds}
                onAccessIds={setAccessIds}
              />
            </form>
          </CardContent>
        </Card>
        <RevisionsPanel
          revisions={revisions}
          pendingId={revert.isPending ? (revert.variables ?? null) : null}
          onRevert={(revisionId) => revert.mutate(revisionId)}
        />
      </div>
      {template.isBuiltInTemplate ? null : (
        <DangerZone
          description="Delete this template. This cannot be undone."
          actionLabel="Delete template"
          confirmTitle="Delete web template?"
          confirmBody="This cannot be undone."
          onConfirm={() => remove.mutate()}
          pending={remove.isPending}
        />
      )}
    </div>
  );
}

export function NewWebTemplatePage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  useDocumentTitle(["New web template"]);
  const navigate = useNavigate();
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
    enabled: themeId.length > 0,
  });

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
      void navigate({ to: "/themes/$themeId/web-templates/$id", params: { themeId, id: result.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={<WebTemplatesBackLink themeId={themeId} />}
        title="New web template"
        description="Variables for the template appear once it is created."
        actions={
          <Button type="submit" form="new-web-template-form" loading={create.isPending}>
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
            id="new-web-template-form"
            className="space-y-5"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              create.mutate();
            }}
          >
            <WebTemplateSettings
              idPrefix="new-web"
              label={label}
              onLabel={(next) => {
                setLabel(next);
                if (!developerTouched) {
                  setDeveloperName(toDeveloperName(next));
                }
              }}
              developerNameField={
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
              }
              isBaseLayout={isBaseLayout}
              onBaseLayout={setIsBaseLayout}
              parentTemplateId={parentTemplateId}
              onParentTemplate={setParentTemplateId}
              parentOptions={layouts.data?.items ?? []}
              allowAccessForNewContentTypes={allowAccessForNewContentTypes}
              onAllowAccessForNewContentTypes={setAllowAccessForNewContentTypes}
              accessIds={accessIds}
              onAccessIds={setAccessIds}
            />
          </form>
        </CardContent>
      </Card>
      <TemplateWorkbench
        title={developerName || "New template"}
        value={content}
        onChange={setContent}
        ariaLabel="New web template content"
        themeId={themeId}
        onSave={() => create.mutate()}
      />
    </div>
  );
}

interface FavoriteInput {
  id: string;
  setAsFavorite: boolean;
}

/** `onMutate` applies the optimistic change and returns its rollback. */
function useFavoriteWebTemplate(
  themeId: string,
  { onMutate }: { onMutate: (input: FavoriteInput) => Promise<() => void> | (() => void) },
) {
  const queryClient = useQueryClient();
  const mutationKey = ["web-template-favorite", themeId];
  return useMutation({
    mutationKey,
    mutationFn: ({ id, setAsFavorite }: FavoriteInput) => adminApi.webTemplates(themeId).favorite(id, setAsFavorite),
    onMutate,
    onError: (error, _input, rollback) => {
      rollback?.();
      toast.error(formatError(error));
    },
    onSettled: (_result, _error, { id }) => {
      // A refetch while another star is in flight would overwrite that star's optimistic state.
      if (queryClient.isMutating({ mutationKey }) > 1) {
        return;
      }
      void queryClient.invalidateQueries({ queryKey: ["web-templates", themeId] });
      // The open editor resets its form whenever the template refetches, so only mark it stale.
      void queryClient.invalidateQueries({ queryKey: ["web-template", themeId, id], refetchType: "none" });
    },
  });
}

function favoriteGroups(items: WebTemplateDetail[]): { title: string | null; items: WebTemplateDetail[] }[] {
  if (!items.some((item) => item.isFavorite)) {
    return [{ title: null, items }];
  }
  return [
    { title: "Favorites", items: items.filter((item) => item.isFavorite) },
    { title: "Other templates", items: items.filter((item) => !item.isFavorite) },
  ].filter((group) => group.items.length > 0);
}

function FavoriteStarButton({
  label,
  isFavorite,
  onToggle,
}: {
  label: string;
  isFavorite: boolean;
  onToggle: () => void;
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      size="icon"
      className="size-8"
      aria-label={`Favorite ${label}`}
      aria-pressed={isFavorite}
      title={isFavorite ? "Remove from favorites" : "Add to favorites"}
      onClick={onToggle}
    >
      <Star className={isFavorite ? "fill-current text-warning" : "text-muted-foreground"} aria-hidden />
    </Button>
  );
}

function WebTemplatesBackLink({ themeId }: { themeId: string }) {
  return (
    <ListBackLink
      to="/themes/$themeId/web-templates"
      params={{ themeId }}
      listKey={`web-templates:${themeId}`}
      label="web templates"
    />
  );
}

function WebTemplateSettings({
  idPrefix,
  label,
  onLabel,
  developerNameField,
  isBaseLayout,
  onBaseLayout,
  baseLayoutLocked = false,
  parentTemplateId,
  onParentTemplate,
  parentOptions,
  allowAccessForNewContentTypes,
  onAllowAccessForNewContentTypes,
  accessIds,
  onAccessIds,
}: {
  idPrefix: string;
  label: string;
  onLabel: (value: string) => void;
  developerNameField?: ReactNode;
  isBaseLayout: boolean;
  onBaseLayout: (value: boolean) => void;
  baseLayoutLocked?: boolean;
  parentTemplateId: string;
  onParentTemplate: (value: string) => void;
  parentOptions: WebTemplateDetail[];
  allowAccessForNewContentTypes: boolean;
  onAllowAccessForNewContentTypes: (value: boolean) => void;
  accessIds: string[];
  onAccessIds: (update: (current: string[]) => string[]) => void;
}) {
  const contentTypes = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 100 }),
  });

  return (
    <>
      <div className="grid gap-5 md:grid-cols-2">
        <FormField label="Label" required htmlFor={`${idPrefix}-label`}>
          {(control) => <Input {...control} value={label} onChange={(event) => onLabel(event.target.value)} />}
        </FormField>
        {developerNameField}
        <FormField label="Parent template" htmlFor={`${idPrefix}-parent`}>
          {(control) => (
            <Select {...control} value={parentTemplateId} onChange={(event) => onParentTemplate(event.target.value)}>
              <option value="">None</option>
              {parentOptions.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.label || item.developerName || item.id}
                </option>
              ))}
            </Select>
          )}
        </FormField>
      </div>
      <div className="flex items-center gap-2">
        <Checkbox
          id={`${idPrefix}-base-layout`}
          checked={isBaseLayout}
          onCheckedChange={onBaseLayout}
          disabled={baseLayoutLocked}
        />
        <label htmlFor={`${idPrefix}-base-layout`} className="text-sm">
          Base layout that other templates can inherit from
        </label>
      </div>
      <fieldset className="space-y-2.5">
        <legend className="mb-2 text-sm font-medium">Content types that can use this template</legend>
        <div className="flex items-center gap-2">
          <Checkbox
            id={`${idPrefix}-allow-new`}
            checked={allowAccessForNewContentTypes}
            onCheckedChange={onAllowAccessForNewContentTypes}
          />
          <label htmlFor={`${idPrefix}-allow-new`} className="text-sm">
            Content types created later
          </label>
        </div>
        {(contentTypes.data?.items ?? []).map((type) => {
          const typeLabel = readString(entityFields(type), "labelPlural", "labelSingular", "developerName") || type.id;
          return (
            <div key={type.id} className="flex items-center gap-2">
              <Checkbox
                id={`${idPrefix}-access-${type.id}`}
                checked={accessIds.includes(type.id)}
                onCheckedChange={(next) =>
                  onAccessIds((current) => (next ? [...current, type.id] : current.filter((item) => item !== type.id)))
                }
              />
              <label htmlFor={`${idPrefix}-access-${type.id}`} className="text-sm">
                {typeLabel}
              </label>
            </div>
          );
        })}
      </fieldset>
    </>
  );
}

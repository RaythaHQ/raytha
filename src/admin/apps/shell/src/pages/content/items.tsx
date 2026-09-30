import {
  adminApi,
  ApiError,
  currentSession,
  formatError,
  hasContentTypePermission,
} from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  buttonVariants,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  PageHeader,
  Skeleton,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams, useSearch } from "@tanstack/react-router";
import {
  ArrowDownUp,
  Check,
  ChevronDown,
  Columns3,
  Download,
  ExternalLink,
  Eye,
  EyeOff,
  FileSpreadsheet,
  Globe,
  House,
  LayoutTemplate,
  List,
  ListFilter,
  Plus,
  Rows3,
  Settings2,
  SlidersHorizontal,
  Star,
  Trash2,
  Upload,
} from "lucide-react";
import { useEffect, useState, type ReactNode } from "react";
import { BackgroundTaskStatus } from "../../components/background-task-status";
import { CrudListPage, type ListColumn } from "../crud-list";
import { entityFields, formatWhen, isRecord, readBoolean, readString } from "../entity";
import { parseContentTypeSummary, parseNamedRefs, type ContentField } from "./fields-model";
import { describeFilter, describeSort, parseViewModel, viewColumnOptions, type ViewModel } from "./filter-model";
import { publicPath } from "./public-url";

const VIEWED_PREFIX = "raytha.contentview:";

function rememberViewedView(developerName: string, viewId: string): void {
  try {
    sessionStorage.setItem(`${VIEWED_PREFIX}${developerName}`, viewId);
  } catch {
    // sessionStorage can be unavailable
  }
}

function lastViewedViewId(developerName: string): string | null {
  try {
    return sessionStorage.getItem(`${VIEWED_PREFIX}${developerName}`);
  } catch {
    return null;
  }
}

function clearViewedView(developerName: string): void {
  try {
    sessionStorage.removeItem(`${VIEWED_PREFIX}${developerName}`);
  } catch {
    // sessionStorage can be unavailable
  }
}

/**
 * `/content/$developerName` has no view in the URL. Resolve one the way the
 * pre-2.0 admin did (last viewed, then favorite, then oldest view) and replace
 * the URL with `/content/$developerName/$viewId`.
 */
export function ContentTypeHomePage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  const navigate = useNavigate();
  const search = useSearch({ strict: false });
  const views = adminApi.views(developerName);
  const remembered = developerName ? lastViewedViewId(developerName) : null;

  const favoritesQuery = useQuery({
    queryKey: ["content-view-favorites", developerName],
    queryFn: () => views.favorites({ pageSize: 100 }),
    enabled: developerName.length > 0 && !remembered,
  });
  const firstViewQuery = useQuery({
    queryKey: ["content-views-first", developerName],
    queryFn: () => views.list({ pageSize: 1, orderBy: "CreationTime asc" }),
    enabled: developerName.length > 0 && !remembered,
  });

  useEffect(() => {
    if (!developerName) {
      return;
    }
    const goToView = (viewId: string) =>
      void navigate({
        to: "/content/$developerName/$viewId",
        params: { developerName, viewId },
        search,
        replace: true,
      });
    if (remembered) {
      goToView(remembered);
      return;
    }
    if (!favoritesQuery.data || !firstViewQuery.data) {
      return;
    }
    const target = favoritesQuery.data.items[0]?.id ?? firstViewQuery.data.items[0]?.id;
    if (target) {
      goToView(target);
    } else {
      void navigate({ to: "/content/$developerName/views", params: { developerName }, replace: true });
    }
  }, [developerName, remembered, favoritesQuery.data, firstViewQuery.data, navigate, search]);

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content items" />
        <p className="text-sm text-muted-foreground">Pick a content type from the sidebar.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-64 w-full" />
    </div>
  );
}

/** The items workspace for one view: view-driven columns plus view/settings menus. */
export function ContentViewItemsPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  const viewId = typeof params.viewId === "string" ? params.viewId : "";
  const navigate = useNavigate();
  const views = adminApi.views(developerName);
  const items = adminApi.contentItems(developerName);
  const homePageId = currentSession()?.homePageId ?? null;
  const canEdit = hasContentTypePermission(developerName, "edit");
  const canConfig = hasContentTypePermission(developerName, "config");

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const viewQuery = useQuery({
    queryKey: ["content-view", developerName, viewId],
    queryFn: () => views.get(viewId),
    enabled: developerName.length > 0 && viewId.length > 0,
  });
  const favoritesQuery = useQuery({
    queryKey: ["content-view-favorites", developerName],
    queryFn: () => views.favorites({ pageSize: 100 }),
    enabled: developerName.length > 0,
  });
  const [exportTaskId, setExportTaskId] = useState<string | null>(null);
  const exportMutation = useMutation({
    mutationFn: (exportOnlyColumnsFromView: boolean) => views.exportCsv(viewId, { exportOnlyColumnsFromView }),
    onSuccess: (result) => {
      toast.success("Export started. The file appears below when it is ready.");
      setExportTaskId(result.id);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const contentType = parseContentTypeSummary(typeQuery.data);
  const view = parseViewModel(viewQuery.data);
  const hasTemplateColumn = view?.columns.includes("Template") ?? false;
  const templatesQuery = useQuery({
    queryKey: ["content-templates", developerName],
    queryFn: () => adminApi.contentTypes.templates(developerName),
    enabled: developerName.length > 0 && hasTemplateColumn,
  });

  // A stale remembered view (deleted since the last visit) self-heals back to the resolver.
  const viewMissing = viewQuery.error instanceof ApiError && viewQuery.error.status === 404;
  useEffect(() => {
    if (!viewMissing) {
      return;
    }
    clearViewedView(developerName);
    void navigate({ to: "/content/$developerName", params: { developerName }, replace: true });
  }, [viewMissing, developerName, navigate]);

  const resolvedViewId = view?.id;
  useEffect(() => {
    if (resolvedViewId) {
      rememberViewedView(developerName, resolvedViewId);
    }
  }, [developerName, resolvedViewId]);

  if (!developerName || !viewId) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content items" />
        <p className="text-sm text-muted-foreground">Pick a content type from the sidebar.</p>
      </div>
    );
  }

  if (viewQuery.isError && !viewMissing) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content items" />
        <p className="text-sm text-destructive">{formatError(viewQuery.error)}</p>
      </div>
    );
  }

  if (!view || !contentType) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  const favorites = (favoritesQuery.data?.items ?? []).map((entity) => ({
    id: entity.id,
    label: readString(entityFields(entity), "label") || entity.id,
  }));
  const cellContext: CellContext = {
    developerName,
    homePageId,
    fields: contentType.fields,
    primaryFieldLabel: contentType.fields.find((field) => field.id === contentType.primaryFieldId)?.label ?? "",
    templateLabels: new Map(parseNamedRefs(templatesQuery.data).map((ref) => [ref.id, ref.label])),
  };
  const columns = viewTableColumns(view, cellContext);
  const primaryFieldVisible = view.columns.length === 0 || view.columns.includes("PrimaryField");

  return (
    <CrudListPage
      key={view.id}
      title={view.label}
      titleAccessory={homePageId === view.id ? <HomeBadge /> : undefined}
      description={view.description || undefined}
      queryKey={["content-items", developerName, viewId]}
      listKey={`content-items:${developerName}`}
      noun="item"
      list={(listParams) =>
        items.list({ ...listParams, orderBy: undefined, viewId, pageSize: view.defaultNumberOfItemsPerPage })
      }
      createTo={canEdit ? "/content/$developerName/new" : undefined}
      createParams={{ developerName }}
      createLabel={`New ${contentType.labelSingular || "item"}`}
      columns={columns}
      rowActions={
        primaryFieldVisible
          ? undefined
          : (entity) => [
              {
                id: "edit",
                label: "Edit",
                to: "/content/$developerName/items/$id",
                params: { developerName, id: entity.id },
              },
            ]
      }
      meta={
        <ViewSummary
          developerName={developerName}
          view={view}
          fields={contentType.fields}
          primaryFieldLabel={cellContext.primaryFieldLabel}
          canConfig={canConfig}
        />
      }
      belowHeader={exportTaskId ? <BackgroundTaskStatus taskId={exportTaskId} /> : undefined}
      actions={
        <>
          <ViewMenu
            developerName={developerName}
            view={view}
            favorites={favorites}
            homePageId={homePageId}
            canConfig={canConfig}
            canEdit={canEdit}
            exporting={exportMutation.isPending}
            onExport={(exportOnlyColumnsFromView) => exportMutation.mutate(exportOnlyColumnsFromView)}
          />
          <SettingsMenu developerName={developerName} canConfig={canConfig} />
        </>
      }
    />
  );
}

const SUMMARY_CHIP =
  "inline-flex max-w-full items-center gap-1.5 rounded-full border border-border bg-card px-3 py-1 text-xs font-medium text-muted-foreground shadow-xs transition-colors";
const SUMMARY_CHIP_LINK = "hover:border-primary/40 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";

type ViewEditorTab = "columns" | "sort" | "filter" | "public";

/** The view's configuration at a glance; each fact opens the matching view editor tab. */
function ViewSummary({
  developerName,
  view,
  fields,
  primaryFieldLabel,
  canConfig,
}: {
  developerName: string;
  view: ViewModel;
  fields: ContentField[];
  primaryFieldLabel: string;
  canConfig: boolean;
}) {
  const templateQuery = useQuery({
    queryKey: ["content-view-template", developerName, view.id],
    queryFn: () => adminApi.views(developerName).template(view.id),
    retry: false,
  });
  const template = parseNamedRefs([templateQuery.data])[0];
  const options = viewColumnOptions(fields).map((option) =>
    option.developerName === "PrimaryField" && primaryFieldLabel ? { ...option, label: primaryFieldLabel } : option,
  );
  const columnLabels = (view.columns.length > 0 ? view.columns : ["PrimaryField"]).map(
    (name) => options.find((option) => option.developerName === name)?.label ?? name,
  );
  const filter = describeFilter(view.filter, options, fields);
  const sort = describeSort(view.sort, options);

  const fact = (tab: ViewEditorTab, icon: ReactNode, label: ReactNode, title?: string) => {
    const body = (
      <>
        {icon}
        <span className="truncate">{label}</span>
      </>
    );
    return canConfig ? (
      <Link
        to="/content/$developerName/views/$viewId"
        params={{ developerName, viewId: view.id }}
        hash={tab}
        title={title}
        className={cn(SUMMARY_CHIP, SUMMARY_CHIP_LINK)}
      >
        {body}
      </Link>
    ) : (
      <span title={title} className={SUMMARY_CHIP}>
        {body}
      </span>
    );
  };

  return (
    <nav aria-label="View configuration" className="flex flex-wrap items-center gap-2">
      {fact(
        "columns",
        <Columns3 className="size-3.5 shrink-0" aria-hidden />,
        `${columnLabels.length} ${columnLabels.length === 1 ? "column" : "columns"}`,
        columnLabels.join(", "),
      )}
      {fact("sort", <ArrowDownUp className="size-3.5 shrink-0" aria-hidden />, `Sorted by ${sort}`)}
      {fact(
        "filter",
        <ListFilter className="size-3.5 shrink-0" aria-hidden />,
        filter.conditionCount === 0 ? "No filters" : <span className="max-w-[28rem] truncate">{filter.text}</span>,
        filter.text || undefined,
      )}
      {fact(
        "public",
        <Rows3 className="size-3.5 shrink-0" aria-hidden />,
        `${view.defaultNumberOfItemsPerPage} per page`,
      )}
      {view.isPublished ? (
        <span className="inline-flex max-w-full items-stretch overflow-hidden rounded-full border border-border bg-card text-xs font-medium shadow-xs">
          {canConfig ? (
            <Link
              to="/content/$developerName/views/$viewId"
              params={{ developerName, viewId: view.id }}
              hash="public"
              className="inline-flex items-center gap-1.5 py-1 pr-2 pl-3 text-success hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              <Globe className="size-3.5 shrink-0" aria-hidden />
              Public
            </Link>
          ) : (
            <span className="inline-flex items-center gap-1.5 py-1 pr-2 pl-3 text-success">
              <Globe className="size-3.5 shrink-0" aria-hidden />
              Public
            </span>
          )}
          <a
            href={publicPath(view.routePath)}
            target="_blank"
            rel="noreferrer"
            title="Open the live page"
            className="inline-flex min-w-0 items-center gap-1 border-l border-border py-1 pr-3 pl-2 font-mono text-muted-foreground hover:bg-muted/60 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <span className="truncate">{publicPath(view.routePath)}</span>
            <ExternalLink className="size-3 shrink-0" aria-hidden />
          </a>
        </span>
      ) : (
        fact("public", <EyeOff className="size-3.5 shrink-0" aria-hidden />, "Not public")
      )}
      {fact(
        "public",
        <LayoutTemplate className="size-3.5 shrink-0" aria-hidden />,
        template ? template.label : templateQuery.isPending ? "Template…" : "No template",
        "Template for the public list page",
      )}
    </nav>
  );
}

function HomeBadge() {
  return (
    <Badge variant="secondary" className="gap-1 align-middle">
      <House className="size-3" aria-hidden />
      Home page
    </Badge>
  );
}

function menuTriggerButton(label: string, icon: ReactNode) {
  return (
    <button type="button" className={buttonVariants({ variant: "outline" })}>
      {icon}
      {label}
      <ChevronDown className="size-4" aria-hidden />
    </button>
  );
}

function ViewMenu({
  developerName,
  view,
  favorites,
  homePageId,
  canConfig,
  canEdit,
  exporting,
  onExport,
}: {
  developerName: string;
  view: ViewModel;
  favorites: { id: string; label: string }[];
  homePageId: string | null;
  canConfig: boolean;
  canEdit: boolean;
  exporting: boolean;
  onExport: (exportOnlyColumnsFromView: boolean) => void;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const views = adminApi.views(developerName);
  const isFavorite = favorites.some((favorite) => favorite.id === view.id);

  const favoriteMutation = useMutation({
    mutationFn: (setAsFavorite: boolean) => views.favorite(view.id, setAsFavorite),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["content-view-favorites", developerName] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <DropdownMenu>
      <DropdownMenuTrigger>{menuTriggerButton("View", <Eye aria-hidden />)}</DropdownMenuTrigger>
      <DropdownMenuContent>
        <DropdownMenuItem onSelect={() => favoriteMutation.mutate(!isFavorite)}>
          <Star className={isFavorite ? "fill-current" : ""} />
          {isFavorite ? "Remove from favorites" : "Add to favorites"}
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={() =>
            void navigate({
              to: "/content/$developerName/views/$viewId",
              params: { developerName, viewId: view.id },
            })
          }
        >
          <SlidersHorizontal />
          Edit view
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem disabled={exporting} onSelect={() => onExport(true)}>
          <Download />
          Export view to CSV
        </DropdownMenuItem>
        <DropdownMenuItem disabled={exporting} onSelect={() => onExport(false)}>
          <FileSpreadsheet />
          Export all fields to CSV
        </DropdownMenuItem>
        {canEdit && (
          <DropdownMenuItem
            onSelect={() => void navigate({ to: "/content/$developerName/import", params: { developerName } })}
          >
            <Upload />
            Import from CSV
          </DropdownMenuItem>
        )}
        {favorites.length > 0 && (
          <>
            <DropdownMenuSeparator />
            <DropdownMenuLabel>Favorites</DropdownMenuLabel>
            {favorites.map((favorite) => (
              <DropdownMenuItem
                key={favorite.id}
                onSelect={() =>
                  void navigate({
                    to: "/content/$developerName/$viewId",
                    params: { developerName, viewId: favorite.id },
                  })
                }
              >
                <Star className="fill-current" />
                <span className="min-w-0 flex-1 truncate">{favorite.label}</span>
                {favorite.id === homePageId && <House aria-hidden />}
                {favorite.id === view.id && <Check aria-hidden />}
              </DropdownMenuItem>
            ))}
          </>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={() => void navigate({ to: "/content/$developerName/views", params: { developerName } })}
        >
          <List />
          All views
        </DropdownMenuItem>
        {canConfig && (
          <DropdownMenuItem
            onSelect={() => void navigate({ to: "/content/$developerName/views/new", params: { developerName } })}
          >
            <Plus />
            Create view
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function SettingsMenu({ developerName, canConfig }: { developerName: string; canConfig: boolean }) {
  const navigate = useNavigate();
  return (
    <DropdownMenu>
      <DropdownMenuTrigger>{menuTriggerButton("Settings", <Settings2 aria-hidden />)}</DropdownMenuTrigger>
      <DropdownMenuContent>
        {canConfig && (
          <DropdownMenuItem
            onSelect={() =>
              void navigate({ to: "/content-types/$developerName/fields", params: { developerName } })
            }
          >
            <List />
            Fields
          </DropdownMenuItem>
        )}
        {canConfig && (
          <DropdownMenuItem
            onSelect={() =>
              void navigate({ to: "/content-types/$developerName/configuration", params: { developerName } })
            }
          >
            <Settings2 />
            Configuration
          </DropdownMenuItem>
        )}
        {canConfig && <DropdownMenuSeparator />}
        <DropdownMenuItem
          onSelect={() => void navigate({ to: "/content-types/$developerName/trash", params: { developerName } })}
        >
          <Trash2 />
          Trash
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

type CellContext = {
  developerName: string;
  homePageId: string | null;
  fields: ContentField[];
  primaryFieldLabel: string;
  templateLabels: Map<string, string>;
};

/** One table column per view column, labeled and rendered by field kind. */
function viewTableColumns(view: ViewModel, ctx: CellContext): ListColumn[] {
  const options = viewColumnOptions(ctx.fields);
  const columnNames = view.columns.length > 0 ? view.columns : ["PrimaryField"];
  return columnNames.map((name) => ({
    header:
      name === "PrimaryField" && ctx.primaryFieldLabel
        ? ctx.primaryFieldLabel
        : (options.find((option) => option.developerName === name)?.label ?? name),
    cell: (entity: EntityRef) => renderCell(name, entity, ctx),
  }));
}

const BUILT_IN_CELLS: Record<string, (entity: EntityRef, ctx: CellContext) => ReactNode> = {
  Id: (entity) => <span className="font-mono text-xs">{entity.id}</span>,
  PrimaryField: (entity, ctx) => (
    <span className="inline-flex items-center gap-1.5">
      <Link
        to="/content/$developerName/items/$id"
        params={{ developerName: ctx.developerName, id: entity.id }}
        className="text-primary hover:underline"
      >
        {readString(entityFields(entity), "primaryField") || entity.id}
      </Link>
      {ctx.homePageId === entity.id && <House className="size-3.5 text-muted-foreground" aria-label="Home page" />}
    </span>
  ),
  CreationTime: (entity) => formatWhen(entityFields(entity).creationTime) || "—",
  LastModificationTime: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—",
  CreatorUser: (entity) => auditUserName(entityFields(entity).creatorUser),
  LastModifierUser: (entity) => auditUserName(entityFields(entity).lastModifierUser),
  IsPublished: (entity) =>
    readBoolean(entityFields(entity), "isPublished") ? (
      <Badge variant="success">Published</Badge>
    ) : (
      <Badge variant="secondary">Unpublished</Badge>
    ),
  IsDraft: (entity) => (readBoolean(entityFields(entity), "isDraft") ? "Yes" : "No"),
  Template: (entity, ctx) => {
    const templateId = readString(entityFields(entity), "webTemplateId");
    return (templateId && ctx.templateLabels.get(templateId)) || "—";
  },
  RoutePath: (entity) => {
    const fields = entityFields(entity);
    const routePath = readString(fields, "routePath");
    if (!routePath) {
      return "—";
    }
    return readBoolean(fields, "isPublished") ? (
      <a href={publicPath(routePath)} target="_blank" rel="noreferrer" className="font-mono text-xs text-primary hover:underline">
        {publicPath(routePath)}
      </a>
    ) : (
      <span className="font-mono text-xs">{publicPath(routePath)}</span>
    );
  },
};

function renderCell(name: string, entity: EntityRef, ctx: CellContext): ReactNode {
  const builtIn = BUILT_IN_CELLS[name];
  if (builtIn) {
    return builtIn(entity, ctx);
  }
  const content = entityFields(entity).publishedContent;
  const raw = isRecord(content) ? content[name] : undefined;
  const field = ctx.fields.find((item) => item.developerName === name);
  const text = formatFieldCell(field, raw);
  if (field?.fieldType === "color" && text) {
    return (
      <span className="inline-flex items-center gap-2 font-mono text-xs">
        <span aria-hidden className="size-4 shrink-0 rounded border border-border shadow-xs" style={{ backgroundColor: text }} />
        {text}
      </span>
    );
  }
  return text || "—";
}

function auditUserName(value: unknown): string {
  return (isRecord(value) && readString(value, "fullName")) || "—";
}

function formatFieldCell(field: ContentField | undefined, raw: unknown): string {
  const value = isRecord(raw) && "value" in raw ? raw.value : raw;
  if (value === null || value === undefined) {
    return "";
  }
  switch (field?.fieldType) {
    case "checkbox":
      return value === true || value === "true" || value === "True" ? "Yes" : "No";
    case "multiple_select": {
      const selected = Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
      return selected.map((item) => choiceLabel(field, item)).join(", ");
    }
    case "dropdown":
    case "radio":
      return typeof value === "string" ? choiceLabel(field, value) : displayText(value);
    case "one_to_one_relationship":
      return isRecord(value) ? readString(value, "primaryField") || displayText(value) : displayText(value);
    case "repeater": {
      const count = Array.isArray(value) ? value.length : 0;
      return count > 0 ? `${count} ${count === 1 ? "row" : "rows"}` : "";
    }
    default:
      return displayText(value);
  }
}

function choiceLabel(field: { choices: { label: string; developerName: string }[] }, developerName: string): string {
  return field.choices.find((choice) => choice.developerName === developerName)?.label ?? developerName;
}

function displayText(value: unknown): string {
  let text: string;
  if (typeof value === "string") {
    text = value;
  } else if (typeof value === "number" || typeof value === "boolean") {
    text = String(value);
  } else if (Array.isArray(value)) {
    text = value.map((item) => displayText(item)).filter(Boolean).join(", ");
  } else if (isRecord(value)) {
    text = readString(value, "primaryField", "label", "fullName", "value");
  } else {
    text = "";
  }
  const stripped = text.replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
  return stripped.length > 80 ? `${stripped.slice(0, 77)}…` : stripped;
}

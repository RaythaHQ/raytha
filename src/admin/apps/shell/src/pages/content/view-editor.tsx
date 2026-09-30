import { adminApi, formatError } from "@raytha/api";
import {
  Button,
  buttonVariants,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  DangerZone,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  Textarea,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useLocation, useNavigate, useParams } from "@tanstack/react-router";
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, Star, Trash2 } from "lucide-react";
import { useMemo, useState, type ReactNode } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { parseContentTypeSummary, parseNamedRefs, type ContentField, type FieldTypeName } from "./fields-model";
import { ListBackLink } from "../../components/list-back-link";
import { FilterValueInput } from "./filter-value-input";
import {
  emptyChildGroup,
  emptyCondition,
  flattenFilter,
  isSortableColumn,
  operatorNeedsValue,
  operatorsForFieldType,
  parseViewModel,
  viewColumnOptions,
  type FilterConditionNode,
  type FilterGroupNode,
  type FilterNode,
  type ViewColumnOption,
  type ViewModel,
  type ViewSortRow,
} from "./filter-model";

const EDITOR_TABS = ["columns", "sort", "filter", "public", "details"] as const;
type EditorTab = (typeof EDITOR_TABS)[number];

function parseEditorTab(hash: string): EditorTab {
  return EDITOR_TABS.find((tab) => tab === hash) ?? "columns";
}

export function ContentViewEditorPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  const viewId = typeof params.viewId === "string" ? params.viewId : "";
  useDocumentTitle(["View", developerName]);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const tab = parseEditorTab(useLocation().hash);
  const [label, setLabel] = useState("");
  const [description, setDescription] = useState("");
  const [detailsFor, setDetailsFor] = useState<string | null>(null);

  const views = adminApi.views(developerName);

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
  const templatesQuery = useQuery({
    queryKey: ["content-templates", developerName],
    queryFn: () => adminApi.contentTypes.templates(developerName),
    enabled: developerName.length > 0,
  });
  const viewTemplateQuery = useQuery({
    queryKey: ["content-view-template", developerName, viewId],
    queryFn: () => views.template(viewId),
    enabled: developerName.length > 0 && viewId.length > 0,
    retry: false,
  });

  const contentType = parseContentTypeSummary(typeQuery.data);
  const view = useMemo(() => parseViewModel(viewQuery.data), [viewQuery.data]);
  const templates = parseNamedRefs(templatesQuery.data);
  const currentTemplate = parseNamedRefs([viewTemplateQuery.data])[0];
  const currentTemplateId = currentTemplate?.id ?? "";
  const publicTemplates =
    currentTemplate && !templates.some((template) => template.id === currentTemplate.id)
      ? [currentTemplate, ...templates]
      : templates;
  const isFavorite = (favoritesQuery.data?.items ?? []).some((item) => item.id === viewId);
  const columns = viewColumnOptions(contentType?.fields ?? []);

  if (view && view.id !== detailsFor) {
    setDetailsFor(view.id);
    setLabel(view.label);
    setDescription(view.description);
  }

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-view", developerName, viewId] });
    void queryClient.invalidateQueries({ queryKey: ["content-view-template", developerName, viewId] });
    void queryClient.invalidateQueries({ queryKey: ["content-views", developerName] });
    void queryClient.invalidateQueries({ queryKey: ["content-view-favorites", developerName] });
  };

  const deleteMutation = useMutation({
    mutationFn: () => views.remove(viewId),
    onSuccess: () => {
      toast.success("View deleted");
      void queryClient.invalidateQueries({ queryKey: ["content-views", developerName] });
      void queryClient.invalidateQueries({ queryKey: ["content-view-favorites", developerName] });
      void navigate({ to: "/content/$developerName/views", params: { developerName } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const detailsMutation = useMutation({
    mutationFn: () => views.update(viewId, { label, description }),
    onSuccess: () => {
      toast.success("View saved");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const favoriteMutation = useMutation({
    mutationFn: (setAsFavorite: boolean) => views.favorite(viewId, setAsFavorite),
    onSuccess: () => invalidate(),
    onError: (error) => toast.error(formatError(error)),
  });

  if (!developerName || !viewId) {
    return (
      <div className="space-y-6">
        <PageHeader title="View" />
        <p className="text-sm text-muted-foreground">Pick a view from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <ListBackLink
            to="/content/$developerName/views"
            params={{ developerName }}
            listKey={`content-views:${developerName}`}
            label="views"
          />
        }
        title={view?.label || "View"}
        description={view?.developerName}
        actions={
          <>
            <Link
              to="/content/$developerName/$viewId"
              params={{ developerName, viewId }}
              className={buttonVariants({ variant: "outline" })}
            >
              Open items
            </Link>
            <Button
              type="button"
              variant="outline"
              onClick={() => favoriteMutation.mutate(!isFavorite)}
              aria-label={isFavorite ? "Remove favorite" : "Add favorite"}
            >
              <Star className={isFavorite ? "fill-current" : ""} />
              {isFavorite ? "Favorited" : "Favorite"}
            </Button>
          </>
        }
      />
      <QueryGate query={viewQuery}>
        {() =>
          view ? (
            <Tabs
              value={tab}
              onValueChange={(next) => void navigate({ to: ".", hash: parseEditorTab(next), replace: true })}
            >
              <TabsList>
                <TabsTrigger value="columns">Columns</TabsTrigger>
                <TabsTrigger value="sort">Sort</TabsTrigger>
                <TabsTrigger value="filter">Filter</TabsTrigger>
                <TabsTrigger value="public">Public</TabsTrigger>
                <TabsTrigger value="details">Details</TabsTrigger>
              </TabsList>
              <TabsContent value="columns" className="mt-4">
                <ColumnsEditor
                  view={view}
                  options={columns}
                  onToggle={(developer, showColumn) =>
                    views.updateColumns(viewId, { developerName: developer, showColumn }).then(invalidate, (error: unknown) => {
                      toast.error(formatError(error));
                    })
                  }
                  onReorder={(developer, newFieldOrder) =>
                    views.reorderColumns(viewId, { developerName: developer, newFieldOrder }).then(invalidate, (error: unknown) => {
                      toast.error(formatError(error));
                    })
                  }
                />
              </TabsContent>
              <TabsContent value="sort" className="mt-4">
                <SortEditor
                  view={view}
                  options={columns.filter(isSortableColumn)}
                  onAdd={(row) =>
                    views
                      .updateSort(viewId, {
                        developerName: row.developerName,
                        showColumn: true,
                        orderByDirection: row.direction,
                      })
                      .then(invalidate, (error: unknown) => toast.error(formatError(error)))
                  }
                  onRemove={(developer) =>
                    views
                      .updateSort(viewId, { developerName: developer, showColumn: false, orderByDirection: "" })
                      .then(invalidate, (error: unknown) => toast.error(formatError(error)))
                  }
                  onReorder={(developer, newFieldOrder) =>
                    views.reorderSort(viewId, { developerName: developer, newFieldOrder }).then(invalidate, (error: unknown) => {
                      toast.error(formatError(error));
                    })
                  }
                />
              </TabsContent>
              <TabsContent value="filter" className="mt-4">
                <FilterEditor
                  view={view}
                  fields={contentType?.fields ?? []}
                  options={columns}
                  onSave={(filter) =>
                    views.updateFilter(viewId, flattenFilter(filter)).then(
                      () => {
                        toast.success("Filter saved");
                        invalidate();
                      },
                      (error: unknown) => toast.error(formatError(error)),
                    )
                  }
                />
              </TabsContent>
              <TabsContent value="public" className="mt-4">
                <PublicSettingsEditor
                  key={[
                    view.isPublished,
                    view.routePath,
                    view.defaultNumberOfItemsPerPage,
                    view.maxNumberOfItemsPerPage,
                    view.ignoreClientFilterAndSortQueryParams,
                    currentTemplateId || templates[0]?.id,
                  ].join("|")}
                  view={view}
                  templates={publicTemplates}
                  currentTemplateId={currentTemplateId}
                  onSave={(input) =>
                    views.updatePublicSettings(viewId, input).then(
                      () => {
                        toast.success("Public settings saved");
                        invalidate();
                      },
                      (error: unknown) => toast.error(formatError(error)),
                    )
                  }
                />
              </TabsContent>
              <TabsContent value="details" className="mt-4 space-y-6">
                <Card>
                  <CardContent className="space-y-4 pt-6">
                    <FormField label="Label" required htmlFor="view-edit-label">
                      {(control) => (
                        <Input {...control} value={label} onChange={(event) => setLabel(event.target.value)} />
                      )}
                    </FormField>
                    <FormField label="Description" htmlFor="view-edit-description">
                      {(control) => (
                        <Textarea
                          {...control}
                          value={description}
                          onChange={(event) => setDescription(event.target.value)}
                        />
                      )}
                    </FormField>
                    <Button type="button" loading={detailsMutation.isPending} onClick={() => detailsMutation.mutate()}>
                      Save
                    </Button>
                  </CardContent>
                </Card>
                <DangerZone
                  description="Delete this view. Items are not affected, and its public route stops working."
                  actionLabel="Delete view"
                  confirmTitle={`Delete ${view.label || "this view"}?`}
                  confirmBody="Anyone who saved this view as a favorite loses it. This cannot be undone."
                  onConfirm={() => deleteMutation.mutate()}
                  pending={deleteMutation.isPending}
                />
              </TabsContent>
            </Tabs>
          ) : (
            <p className="text-sm text-muted-foreground">This view could not be read.</p>
          )
        }
      </QueryGate>
    </div>
  );
}

function ReorderList({
  ids,
  onReorder,
  children,
}: {
  ids: string[];
  onReorder: (id: string, newFieldOrder: number) => void;
  children: ReactNode;
}) {
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );
  const onDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }
    const newIndex = ids.indexOf(String(over.id));
    if (newIndex < 0) {
      return;
    }
    onReorder(String(active.id), newIndex + 1);
  };
  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
      <SortableContext items={ids} strategy={verticalListSortingStrategy}>
        <div className="space-y-2">{children}</div>
      </SortableContext>
    </DndContext>
  );
}

function SortableRow({ id, children }: { id: string; children: ReactNode }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.6 : 1,
  };
  return (
    <div ref={setNodeRef} style={style} className="flex items-center gap-2 rounded-lg border border-border px-3 py-2">
      <button
        type="button"
        className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
        aria-label={`Reorder ${id}`}
        {...attributes}
        {...listeners}
      >
        <GripVertical className="size-4" />
      </button>
      <div className="flex min-w-0 flex-1 items-center justify-between gap-2">{children}</div>
    </div>
  );
}

function ColumnsEditor({
  view,
  options,
  onToggle,
  onReorder,
}: {
  view: ViewModel;
  options: ViewColumnOption[];
  onToggle: (developerName: string, showColumn: boolean) => void;
  onReorder: (developerName: string, newFieldOrder: number) => void;
}) {
  const visible = view.columns;
  const hidden = options.filter((option) => !visible.includes(option.developerName));

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle>Visible columns</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {visible.length === 0 ? (
            <p className="text-sm text-muted-foreground">No columns selected.</p>
          ) : (
            <ReorderList
              ids={visible}
              onReorder={onReorder}
            >
              {visible.map((developer) => (
                <SortableRow key={developer} id={developer}>
                  <span className="text-sm">{labelFor(options, developer)}</span>
                  <Button type="button" size="sm" variant="ghost" onClick={() => onToggle(developer, false)}>
                    Hide
                  </Button>
                </SortableRow>
              ))}
            </ReorderList>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Hidden columns</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {hidden.map((option) => (
            <div key={option.developerName} className="flex items-center justify-between gap-2 rounded-lg border border-border px-3 py-2">
              <span className="text-sm">{option.label}</span>
              <Button type="button" size="sm" variant="outline" onClick={() => onToggle(option.developerName, true)}>
                Show
              </Button>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

function SortEditor({
  view,
  options,
  onAdd,
  onRemove,
  onReorder,
}: {
  view: ViewModel;
  options: ViewColumnOption[];
  onAdd: (row: ViewSortRow) => void;
  onRemove: (developerName: string) => void;
  onReorder: (developerName: string, newFieldOrder: number) => void;
}) {
  const used = new Set(view.sort.map((row) => row.developerName));
  const available = options.filter((option) => !used.has(option.developerName));
  const [addField, setAddField] = useState(available[0]?.developerName ?? "");
  const [direction, setDirection] = useState<"asc" | "desc">("asc");

  return (
    <Card>
      <CardHeader>
        <CardTitle>Sort</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        <ReorderList ids={view.sort.map((row) => row.developerName)} onReorder={onReorder}>
          {view.sort.map((row) => (
            <SortableRow key={row.developerName} id={row.developerName}>
              <span className="text-sm">
                {labelFor(options, row.developerName)} · {row.direction === "desc" ? "Descending" : "Ascending"}
              </span>
              <Button type="button" size="sm" variant="ghost" onClick={() => onRemove(row.developerName)}>
                Remove
              </Button>
            </SortableRow>
          ))}
        </ReorderList>
        {available.length > 0 && (
          <div className="flex flex-wrap items-end gap-2">
            <FormField label="Field" htmlFor="sort-field">
              {(control) => (
                <Select {...control} value={addField} onChange={(event) => setAddField(event.target.value)}>
                  {available.map((option) => (
                    <option key={option.developerName} value={option.developerName}>
                      {option.label}
                    </option>
                  ))}
                </Select>
              )}
            </FormField>
            <FormField label="Direction" htmlFor="sort-direction">
              {(control) => (
                <Select
                  {...control}
                  value={direction}
                  onChange={(event) => setDirection(event.target.value === "desc" ? "desc" : "asc")}
                >
                  <option value="asc">Ascending</option>
                  <option value="desc">Descending</option>
                </Select>
              )}
            </FormField>
            <Button
              type="button"
              onClick={() => {
                if (addField) {
                  onAdd({ developerName: addField, direction });
                }
              }}
            >
              Add sort
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function FilterEditor({
  view,
  fields,
  options,
  onSave,
}: {
  view: ViewModel;
  fields: ContentField[];
  options: ViewColumnOption[];
  onSave: (filter: FilterGroupNode) => void;
}) {
  const [root, setRoot] = useState<FilterGroupNode>(view.filter);
  const [syncedFilter, setSyncedFilter] = useState(view.filter);

  if (syncedFilter !== view.filter) {
    setSyncedFilter(view.filter);
    setRoot(view.filter);
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Filter</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <FilterGroupEditor node={root} options={options} fields={fields} onChange={setRoot} />
        <Button type="button" onClick={() => onSave(root)}>
          Save filter
        </Button>
      </CardContent>
    </Card>
  );
}

function FilterGroupEditor({
  node,
  options,
  fields,
  onChange,
}: {
  node: FilterGroupNode;
  options: ViewColumnOption[];
  fields: ContentField[];
  onChange: (next: FilterGroupNode) => void;
}) {
  const replaceChild = (index: number, child: FilterNode) => {
    const children = node.children.slice();
    children[index] = child;
    onChange({ ...node, children });
  };

  return (
    <div className="space-y-3 rounded-lg border border-border p-3">
      <div className="flex flex-wrap items-center gap-2">
        <Select
          aria-label="Group operator"
          value={node.groupOperator}
          onChange={(event) => {
            const value = event.target.value;
            if (value === "AND" || value === "OR" || value === "NOT") {
              onChange({ ...node, groupOperator: value });
            }
          }}
        >
          <option value="AND">AND</option>
          <option value="OR">OR</option>
          <option value="NOT">NOT</option>
        </Select>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => onChange({ ...node, children: [...node.children, emptyCondition(node.id)] })}
        >
          Add condition
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => onChange({ ...node, children: [...node.children, emptyChildGroup(node.id)] })}
        >
          Add group
        </Button>
      </div>
      {node.children.map((child, index) => (
        <div key={child.id} className="flex items-start gap-2">
          <div className="min-w-0 flex-1">
            {child.kind === "group" ? (
              <FilterGroupEditor
                node={child}
                options={options}
                fields={fields}
                onChange={(next) => replaceChild(index, next)}
              />
            ) : (
              <FilterConditionEditor
                node={child}
                options={options}
                fields={fields}
                onChange={(next) => replaceChild(index, next)}
              />
            )}
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-9 shrink-0 text-muted-foreground hover:text-destructive"
            aria-label={child.kind === "group" ? "Remove group" : "Remove condition"}
            onClick={() =>
              onChange({
                ...node,
                children: node.children.filter((item) => item.id !== child.id),
              })
            }
          >
            <Trash2 className="size-4" aria-hidden />
          </Button>
        </div>
      ))}
    </div>
  );
}

function FilterConditionEditor({
  node,
  options,
  fields,
  onChange,
}: {
  node: FilterConditionNode;
  options: ViewColumnOption[];
  fields: ContentField[];
  onChange: (next: FilterConditionNode) => void;
}) {
  const fieldType = fieldTypeFor(node.field, options, fields);
  const operators = operatorsForFieldType(fieldType);
  const needsValue = operatorNeedsValue(node.conditionOperator, fieldType);

  return (
    <div className="grid gap-2 sm:grid-cols-[1fr_1fr_1fr]">
      <Select
        aria-label="Field"
        value={node.field}
        onChange={(event) => {
          const field = event.target.value;
          const nextType = fieldTypeFor(field, options, fields);
          const nextOps = operatorsForFieldType(nextType);
          onChange({
            ...node,
            field,
            conditionOperator: nextOps[0]?.developerName ?? "eq",
            value: "",
          });
        }}
      >
        <option value="">Field</option>
        {options.map((option) => (
          <option key={option.developerName} value={option.developerName}>
            {option.label}
          </option>
        ))}
      </Select>
      <Select
        aria-label="Operator"
        value={node.conditionOperator}
        onChange={(event) => onChange({ ...node, conditionOperator: event.target.value, value: "" })}
      >
        {operators.map((operator) => (
          <option key={operator.developerName} value={operator.developerName}>
            {operator.label}
          </option>
        ))}
      </Select>
      {needsValue ? (
        <FilterValueInput
          key={`${node.field}:${node.conditionOperator}`}
          fieldType={fieldType}
          field={fields.find((item) => item.developerName === node.field)}
          value={node.value}
          onChange={(value) => onChange({ ...node, value })}
        />
      ) : (
        <span className="text-sm text-muted-foreground self-center">No value</span>
      )}
    </div>
  );
}

function PublicSettingsEditor({
  view,
  templates,
  currentTemplateId,
  onSave,
}: {
  view: ViewModel;
  templates: ReturnType<typeof parseNamedRefs>;
  currentTemplateId: string;
  onSave: (input: {
    isPublished: boolean;
    routePath: string;
    templateId: string;
    defaultNumberOfItemsPerPage: number;
    maxNumberOfItemsPerPage: number;
    ignoreClientFilterAndSortQueryParams: boolean;
  }) => void;
}) {
  const [isPublished, setIsPublished] = useState(view.isPublished);
  const [routePath, setRoutePath] = useState(view.routePath);
  const [templateId, setTemplateId] = useState(currentTemplateId || templates[0]?.id || "");
  const [defaultPage, setDefaultPage] = useState(String(view.defaultNumberOfItemsPerPage));
  const [maxPage, setMaxPage] = useState(String(view.maxNumberOfItemsPerPage));
  const [ignoreClient, setIgnoreClient] = useState(view.ignoreClientFilterAndSortQueryParams);

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div className="flex items-center gap-2">
          <Checkbox id="view-published" checked={isPublished} onCheckedChange={setIsPublished} />
          <label htmlFor="view-published" className="text-sm">
            Published
          </label>
        </div>
        <FormField label="Route path" required htmlFor="view-route">
          {(control) => (
            <Input {...control} value={routePath} onChange={(event) => setRoutePath(event.target.value)} />
          )}
        </FormField>
        <FormField label="Template" required htmlFor="view-template">
          {(control) => (
            <Select {...control} value={templateId} onChange={(event) => setTemplateId(event.target.value)}>
              <option value="">Select a template</option>
              {templates.map((template) => (
                <option key={template.id} value={template.id}>
                  {template.label || template.developerName}
                </option>
              ))}
            </Select>
          )}
        </FormField>
        <FormField label="Default items per page" required htmlFor="view-default-page">
          {(control) => (
            <Input
              {...control}
              type="number"
              value={defaultPage}
              onChange={(event) => setDefaultPage(event.target.value)}
            />
          )}
        </FormField>
        <FormField label="Max items per page" required htmlFor="view-max-page">
          {(control) => (
            <Input {...control} type="number" value={maxPage} onChange={(event) => setMaxPage(event.target.value)} />
          )}
        </FormField>
        <div className="flex items-center gap-2">
          <Checkbox id="view-ignore-client" checked={ignoreClient} onCheckedChange={setIgnoreClient} />
          <label htmlFor="view-ignore-client" className="text-sm">
            Ignore client filter and sort query params
          </label>
        </div>
        <Button
          type="button"
          onClick={() =>
            onSave({
              isPublished,
              routePath,
              templateId,
              defaultNumberOfItemsPerPage: Number(defaultPage) || 25,
              maxNumberOfItemsPerPage: Number(maxPage) || 1000,
              ignoreClientFilterAndSortQueryParams: ignoreClient,
            })
          }
        >
          Save public settings
        </Button>
      </CardContent>
    </Card>
  );
}

function labelFor(options: ViewColumnOption[], developerName: string): string {
  return options.find((option) => option.developerName === developerName)?.label ?? developerName;
}

function fieldTypeFor(
  developerName: string,
  options: ViewColumnOption[],
  fields: ContentField[],
): FieldTypeName | "id" {
  const option = options.find((item) => item.developerName === developerName);
  if (option) {
    return option.fieldType;
  }
  const field = fields.find((item) => item.developerName === developerName);
  return field?.fieldType ?? "single_line_text";
}

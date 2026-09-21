import { adminApi, formatError } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
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
import { useParams } from "@tanstack/react-router";
import { Star } from "lucide-react";
import { useEffect, useState } from "react";
import { BackgroundTaskStatus } from "../../components/background-task-status";
import { useDocumentTitle } from "../../lib/document-title";
import { parseContentTypeSummary, parseNamedRefs, type ContentField, type FieldTypeName } from "./fields-model";
import {
  emptyChildGroup,
  emptyCondition,
  flattenFilter,
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
import { ContentTypeNav } from "./nav";

export function ContentViewEditorPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  const viewId = typeof params.viewId === "string" ? params.viewId : "";
  useDocumentTitle(["View", developerName]);
  const queryClient = useQueryClient();
  const [tab, setTab] = useState("columns");
  const [label, setLabel] = useState("");
  const [description, setDescription] = useState("");
  const [hydrated, setHydrated] = useState(false);
  const [exportOnlyColumnsFromView, setExportOnlyColumnsFromView] = useState(true);
  const [exportTaskId, setExportTaskId] = useState<string | null>(null);

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

  const contentType = parseContentTypeSummary(typeQuery.data);
  const view = parseViewModel(viewQuery.data);
  const templates = parseNamedRefs(templatesQuery.data);
  const isFavorite = (favoritesQuery.data?.items ?? []).some((item) => item.id === viewId);
  const columns = viewColumnOptions(contentType?.fields ?? []);

  useEffect(() => {
    if (!view || hydrated) {
      return;
    }
    setLabel(view.label);
    setDescription(view.description);
    setHydrated(true);
  }, [view, hydrated]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-view", developerName, viewId] });
    void queryClient.invalidateQueries({ queryKey: ["content-views", developerName] });
    void queryClient.invalidateQueries({ queryKey: ["content-view-favorites", developerName] });
  };

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

  const exportMutation = useMutation({
    mutationFn: () => views.exportCsv(viewId, { exportOnlyColumnsFromView }),
    onSuccess: (result) => {
      toast.success(`Export started. Task ${result.id}`);
      setExportTaskId(result.id);
    },
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
        title={view?.label || "View"}
        description={view?.developerName}
        actions={
          <>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox
                checked={exportOnlyColumnsFromView}
                onCheckedChange={(checked) => setExportOnlyColumnsFromView(checked)}
              />
              View columns only
            </label>
            <Button
              type="button"
              variant="outline"
              loading={exportMutation.isPending}
              onClick={() => exportMutation.mutate()}
            >
              Export CSV
            </Button>
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
      <ContentTypeNav developerName={developerName} />
      {exportTaskId ? <BackgroundTaskStatus taskId={exportTaskId} /> : null}
      <QueryGate query={viewQuery}>
        {() =>
          view ? (
            <Tabs value={tab} onValueChange={setTab}>
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
                  options={columns}
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
                  view={view}
                  templates={templates}
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
              <TabsContent value="details" className="mt-4">
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
            visible.map((developer, index) => (
              <div key={developer} className="flex items-center justify-between gap-2 rounded-lg border border-border px-3 py-2">
                <span className="text-sm">{labelFor(options, developer)}</span>
                <div className="flex gap-1">
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={index === 0}
                    onClick={() => onReorder(developer, index)}
                  >
                    Up
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={index === visible.length - 1}
                    onClick={() => onReorder(developer, index + 2)}
                  >
                    Down
                  </Button>
                  <Button type="button" size="sm" variant="ghost" onClick={() => onToggle(developer, false)}>
                    Hide
                  </Button>
                </div>
              </div>
            ))
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
        {view.sort.map((row, index) => (
          <div key={row.developerName} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border px-3 py-2">
            <span className="text-sm">
              {labelFor(options, row.developerName)} · {row.direction === "desc" ? "Descending" : "Ascending"}
            </span>
            <div className="flex gap-1">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={index === 0}
                onClick={() => onReorder(row.developerName, index)}
              >
                Up
              </Button>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={index === view.sort.length - 1}
                onClick={() => onReorder(row.developerName, index + 2)}
              >
                Down
              </Button>
              <Button type="button" size="sm" variant="ghost" onClick={() => onRemove(row.developerName)}>
                Remove
              </Button>
            </div>
          </div>
        ))}
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

  useEffect(() => {
    setRoot(view.filter);
  }, [view.filter]);

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
        <div key={child.id} className="space-y-2">
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
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={() =>
              onChange({
                ...node,
                children: node.children.filter((item) => item.id !== child.id),
              })
            }
          >
            Remove
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
        <Input
          aria-label="Value"
          value={node.value}
          onChange={(event) => onChange({ ...node, value: event.target.value })}
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
  onSave,
}: {
  view: ViewModel;
  templates: ReturnType<typeof parseNamedRefs>;
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
  const [templateId, setTemplateId] = useState(templates[0]?.id ?? "");
  const [defaultPage, setDefaultPage] = useState(String(view.defaultNumberOfItemsPerPage));
  const [maxPage, setMaxPage] = useState(String(view.maxNumberOfItemsPerPage));
  const [ignoreClient, setIgnoreClient] = useState(view.ignoreClientFilterAndSortQueryParams);

  useEffect(() => {
    setIsPublished(view.isPublished);
    setRoutePath(view.routePath);
    setDefaultPage(String(view.defaultNumberOfItemsPerPage));
    setMaxPage(String(view.maxNumberOfItemsPerPage));
    setIgnoreClient(view.ignoreClientFilterAndSortQueryParams);
    if (!templateId && templates[0]) {
      setTemplateId(templates[0].id);
    }
  }, [view, templates, templateId]);

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

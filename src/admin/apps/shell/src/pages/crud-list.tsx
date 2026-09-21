import type { EntityRef, JsonObject, PagedResult } from "@raytha/api";
import { formatError, hasPermission } from "@raytha/api";
import {
  buttonVariants,
  Checkbox,
  ConfirmDialog,
  EmptyState,
  FormField,
  Input,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  RowActions,
  SortableTableHead,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
  toast,
  type RowAction,
  type SortDirection,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { useDocumentTitle } from "../lib/document-title";
import {
  compactListQuery,
  listApiParams,
  listHref,
  listQueryFromSearchString,
  listQueryKey,
  nextOrderBy,
  parseOrderBy,
  rememberListQuery,
  type ListQuery,
} from "../lib/list-query";
import { pluralize } from "./entity";

export type FormValue = string | boolean;

export type CreateField = {
  key: string;
  label: string;
  type?: "text" | "email" | "url" | "textarea" | "checkbox";
  required?: boolean;
  hint?: string;
  autoDeveloperNameFrom?: string;
};

export type ListColumn = {
  header: string;
  cell: (entity: EntityRef) => ReactNode;
  sortKey?: string;
  naturalDir?: SortDirection;
};

type ListFn = (params?: Record<string, string | number | boolean | undefined>) => Promise<PagedResult<EntityRef>>;

export function emptyForm(fields: CreateField[]): Record<string, FormValue> {
  const form: Record<string, FormValue> = {};
  for (const field of fields) {
    form[field.key] = field.type === "checkbox" ? false : "";
  }
  return form;
}

export function formToJson(form: Record<string, FormValue>): JsonObject {
  const json: JsonObject = {};
  for (const [key, value] of Object.entries(form)) {
    json[key] = value;
  }
  return json;
}

export function CrudListPage({
  title,
  description,
  queryKey,
  listKey,
  noun,
  list,
  remove,
  createPermission,
  createLabel,
  createTo,
  createParams,
  columns,
  rowActions,
  canDelete,
  emptyHint,
  actions,
}: {
  title: string;
  description?: string;
  queryKey: string[];
  listKey: string;
  noun: string;
  list: ListFn;
  remove?: (id: string) => Promise<void>;
  createPermission?: string;
  createLabel?: string;
  createTo?: string;
  createParams?: Record<string, string>;
  columns: ListColumn[];
  rowActions?: (entity: EntityRef, helpers: { requestDelete: (id: string) => void }) => RowAction[];
  canDelete?: (entity: EntityRef) => boolean;
  emptyHint?: string;
  actions?: ReactNode;
}) {
  useDocumentTitle([title]);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const applied = listQueryFromSearchString(location.searchStr);
  const [draftSearch, setDraftSearch] = useState(applied.search ?? "");
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const nounPlural = pluralize(noun);
  const appliedKey = listQueryKey(applied);

  useEffect(() => {
    setDraftSearch(applied.search ?? "");
  }, [applied.search]);

  useEffect(() => {
    rememberListQuery(listKey, applied);
  }, [listKey, appliedKey]);

  useDebouncedSearchWrite(draftSearch, applied, (next) => {
    writeListSearch(navigate, next);
  });

  const query = useQuery({
    queryKey: [...queryKey, appliedKey],
    queryFn: () => list(listApiParams(applied)),
    placeholderData: keepPreviousData,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => {
      if (!remove) {
        throw new Error("Delete is not available.");
      }
      return remove(id);
    },
    onSuccess: () => {
      toast.success(`${capitalize(noun)} deleted`);
      void queryClient.invalidateQueries({ queryKey });
      setDeleteId(null);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const canCreate = Boolean(createTo) && (createPermission ? hasPermission(createPermission) : true);
  const sort = parseOrderBy(applied.orderBy);
  const showActions = Boolean(remove || rowActions);

  return (
    <div className="space-y-6">
      <PageHeader
        title={title}
        description={description}
        actions={
          canCreate || actions ? (
            <>
              {actions}
              {canCreate && createTo ? (
                <a href={listHref(createTo, createParams)} className={buttonVariants()}>
                  {createLabel ?? `New ${noun}`}
                </a>
              ) : null}
            </>
          ) : undefined
        }
      />
      <QueryGate query={query}>
        {(data) => (
          <ListPanel
            toolbar={
              <>
                <ListSearch
                  value={draftSearch}
                  onChange={(event) => setDraftSearch(event.target.value)}
                  placeholder={`Search ${nounPlural}`}
                  aria-label={`Search ${nounPlural}`}
                />
                <ListStatus
                  total={data.totalCount}
                  page={data.pageNumber}
                  noun={data.totalCount === 1 ? noun : nounPlural}
                />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState
                icon={Inbox}
                title={`No ${nounPlural}`}
                hint={emptyHint ?? (applied.search ? "Nothing matches that search." : `No ${nounPlural} yet.`)}
              />
            ) : (
              <Table flush aria-label={title}>
                <TableHeader>
                  <TableRow>
                    {columns.map((column) =>
                      column.sortKey ? (
                        <SortableTableHead
                          key={column.header}
                          column={column.sortKey}
                          label={column.header}
                          sort={sort.column}
                          dir={sort.dir}
                          naturalDir={column.naturalDir}
                          onSort={(columnKey, naturalDir) => {
                            writeListSearch(navigate, {
                              ...applied,
                              pageNumber: 1,
                              orderBy: nextOrderBy(applied.orderBy, columnKey, naturalDir),
                            });
                          }}
                        />
                      ) : (
                        <TableHead key={column.header}>{column.header}</TableHead>
                      ),
                    )}
                    {showActions ? <TableHead className="w-12"><span className="sr-only">Actions</span></TableHead> : null}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => {
                    const extra = rowActions?.(item, { requestDelete: setDeleteId }) ?? [];
                    const allowDelete = Boolean(remove) && (canDelete ? canDelete(item) : true);
                    const actions: RowAction[] = [
                      ...extra,
                      ...(allowDelete
                        ? [
                            {
                              id: "delete",
                              label: "Delete",
                              destructive: true,
                              onSelect: () => setDeleteId(item.id),
                            } satisfies RowAction,
                          ]
                        : []),
                    ];
                    return (
                      <TableRow key={item.id}>
                        {columns.map((column) => (
                          <TableCell key={column.header}>{column.cell(item)}</TableCell>
                        ))}
                        {showActions ? (
                          <TableCell>
                            <RowActions actions={actions} />
                          </TableCell>
                        ) : null}
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>

      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteId(null);
          }
        }}
        title={`Delete ${noun}?`}
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

export function CreateFieldControl({
  field,
  value,
  onChange,
}: {
  field: CreateField;
  value: FormValue;
  onChange: (value: FormValue) => void;
}) {
  if (field.type === "checkbox") {
    return (
      <div className="flex items-center gap-2">
        <Checkbox
          id={field.key}
          checked={value === true}
          onCheckedChange={(checked) => onChange(checked)}
        />
        <label htmlFor={field.key} className="text-sm">
          {field.label}
        </label>
      </div>
    );
  }

  return (
    <FormField label={field.label} required={field.required} hint={field.hint} htmlFor={field.key}>
      {(control) =>
        field.type === "textarea" ? (
          <Textarea
            {...control}
            value={typeof value === "string" ? value : ""}
            onChange={(event) => onChange(event.target.value)}
          />
        ) : (
          <Input
            {...control}
            type={field.type === "email" || field.type === "url" ? field.type : "text"}
            value={typeof value === "string" ? value : ""}
            onChange={(event) => onChange(event.target.value)}
          />
        )
      }
    </FormField>
  );
}

function useDebouncedSearchWrite(
  draftSearch: string,
  applied: ListQuery,
  write: (next: ListQuery) => void,
) {
  const writeRef = useRef(write);
  writeRef.current = write;
  const appliedSearch = applied.search ?? "";

  useEffect(() => {
    if (draftSearch === appliedSearch) {
      return;
    }
    const handle = window.setTimeout(() => {
      writeRef.current({
        ...applied,
        search: draftSearch.trim() ? draftSearch : undefined,
        pageNumber: 1,
      });
    }, 300);
    return () => window.clearTimeout(handle);
  }, [draftSearch, appliedSearch, applied]);
}

function writeListSearch(navigate: ReturnType<typeof useNavigate>, query: ListQuery) {
  const search = compactListQuery(query);
  void navigate({
    to: ".",
    search,
    replace: true,
  });
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

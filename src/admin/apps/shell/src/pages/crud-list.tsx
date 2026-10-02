import type { EntityRef, JsonObject, PagedResult } from "@raytha/api";
import { hasPermission } from "@raytha/api";
import {
  Button,
  buttonVariants,
  Checkbox,
  EmptyState,
  FormField,
  Input,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  RowActions,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
  type RowAction,
} from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import { useEffect, useEffectEvent, useState, type ReactNode } from "react";
import { useDocumentTitle } from "../lib/document-title";
import { AppLink } from "../components/list-back-link";
import {
  compactListQuery,
  listApiParams,
  listHref,
  listQueryFromSearchString,
  listQueryKey,
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
  titleAccessory,
  description,
  queryKey,
  listKey,
  noun,
  list,
  createPermission,
  createLabel,
  createTo,
  createParams,
  columns,
  rowActions,
  emptyHint,
  actions,
  meta,
  tabs,
  belowHeader,
}: {
  title: string;
  titleAccessory?: ReactNode;
  description?: string;
  queryKey: string[];
  listKey: string;
  noun: string;
  list: ListFn;
  createPermission?: string;
  createLabel?: string;
  createTo?: string;
  createParams?: Record<string, string>;
  columns: ListColumn[];
  rowActions?: (entity: EntityRef) => RowAction[];
  emptyHint?: string;
  actions?: ReactNode;
  meta?: ReactNode;
  tabs?: ReactNode;
  belowHeader?: ReactNode;
}) {
  useDocumentTitle([title]);
  const navigate = useNavigate();
  const location = useLocation();
  const applied = listQueryFromSearchString(location.searchStr);
  const [draftSearch, setDraftSearch] = useState(applied.search ?? "");
  const [syncedSearch, setSyncedSearch] = useState(applied.search);
  const nounPlural = pluralize(noun);
  const appliedKey = listQueryKey(applied);

  if (syncedSearch !== applied.search) {
    setSyncedSearch(applied.search);
    setDraftSearch(applied.search ?? "");
  }

  const remember = useEffectEvent(() => rememberListQuery(listKey, applied));
  useEffect(() => {
    remember();
  }, [listKey, appliedKey]);

  useDebouncedSearchWrite(draftSearch, applied, (next) => {
    writeListSearch(navigate, next);
  });

  const query = useQuery({
    queryKey: [...queryKey, appliedKey],
    queryFn: () => list(listApiParams(applied)),
    placeholderData: keepPreviousData,
  });
  const pageCount = query.data ? Math.max(1, Math.ceil(query.data.totalCount / query.data.pageSize)) : 1;
  const pageNumber = applied.pageNumber ?? 1;

  const canCreate = Boolean(createTo) && (createPermission ? hasPermission(createPermission) : true);
  const showActions = Boolean(rowActions);

  return (
    <div className="space-y-6">
      <PageHeader
        title={
          titleAccessory ? (
            <span className="inline-flex flex-wrap items-center gap-2">
              {title}
              {titleAccessory}
            </span>
          ) : (
            title
          )
        }
        description={description}
        meta={meta}
        tabs={tabs}
        actions={
          canCreate || actions ? (
            <>
              {actions}
              {canCreate && createTo ? (
                <AppLink href={listHref(createTo, createParams)} className={buttonVariants()}>
                  {createLabel ?? `New ${noun}`}
                </AppLink>
              ) : null}
            </>
          ) : undefined
        }
      />
      {belowHeader}
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
                    {columns.map((column) => (
                      <TableHead key={column.header}>{column.header}</TableHead>
                    ))}
                    {showActions ? <TableHead className="w-12"><span className="sr-only">Actions</span></TableHead> : null}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => {
                    const actions = rowActions?.(item) ?? [];
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
      {pageCount > 1 ? (
        <div className="flex items-center justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            disabled={pageNumber <= 1}
            onClick={() => writeListSearch(navigate, { ...applied, pageNumber: pageNumber - 1 })}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {pageNumber} of {pageCount}
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={pageNumber >= pageCount}
            onClick={() => writeListSearch(navigate, { ...applied, pageNumber: pageNumber + 1 })}
          >
            Next
          </Button>
        </div>
      ) : null}
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
  const writeLatest = useEffectEvent(write);
  const appliedSearch = applied.search ?? "";

  useEffect(() => {
    if (draftSearch === appliedSearch) {
      return;
    }
    const handle = window.setTimeout(() => {
      writeLatest({
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

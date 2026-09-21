import type { EntityRef, JsonObject, PagedResult } from "@raytha/api";
import { formatError, hasPermission } from "@raytha/api";
import {
  Button,
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Inbox } from "lucide-react";
import { useState, type FormEvent, type ReactNode } from "react";
import { useDocumentTitle } from "../lib/document-title";

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
  description,
  queryKey,
  noun,
  list,
  create,
  remove,
  createPermission,
  createLabel,
  createFields,
  buildCreatePayload,
  extraCreateFields,
  columns,
  rowActions,
  emptyHint,
  actions,
}: {
  title: string;
  description?: string;
  queryKey: string[];
  noun: string;
  list: ListFn;
  create?: (input: JsonObject) => Promise<EntityRef>;
  remove?: (id: string) => Promise<void>;
  createPermission?: string;
  createLabel?: string;
  createFields?: CreateField[];
  buildCreatePayload?: (form: Record<string, FormValue>) => JsonObject;
  extraCreateFields?: (form: Record<string, FormValue>, setForm: (next: Record<string, FormValue>) => void) => ReactNode;
  columns: ListColumn[];
  rowActions?: (entity: EntityRef, helpers: { requestDelete: (id: string) => void }) => ReactNode;
  emptyHint?: string;
  actions?: ReactNode;
}) {
  useDocumentTitle([title]);
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState<Record<string, FormValue>>(() => emptyForm(createFields ?? []));
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const query = useQuery({
    queryKey: [...queryKey, search],
    queryFn: () => list({ search: search || undefined, pageSize: 50 }),
    placeholderData: keepPreviousData,
  });

  const createMutation = useMutation({
    mutationFn: (input: JsonObject) => {
      if (!create) {
        throw new Error("Create is not available.");
      }
      return create(input);
    },
    onSuccess: () => {
      toast.success(`${capitalize(noun)} created`);
      void queryClient.invalidateQueries({ queryKey });
      setCreateOpen(false);
      setForm(emptyForm(createFields ?? []));
    },
    onError: (error) => toast.error(formatError(error)),
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

  const canCreate = Boolean(create && createFields) && (createPermission ? hasPermission(createPermission) : true);

  const handleCreate = (event: FormEvent) => {
    event.preventDefault();
    const payload = buildCreatePayload ? buildCreatePayload(form) : formToJson(form);
    createMutation.mutate(payload);
  };

  const setField = (key: string, value: FormValue, autoFrom?: string) => {
    setForm((current) => {
      const next: Record<string, FormValue> = { ...current, [key]: value };
      if (autoFrom && typeof value === "string") {
        const existing = current[autoFrom];
        if (typeof existing === "string" && (existing === "" || existing === toAutoName(String(current[key] ?? "")))) {
          next[autoFrom] = toAutoName(value);
        }
      }
      return next;
    });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={title}
        description={description}
        actions={
          canCreate || actions ? (
            <>
              {actions}
              {canCreate ? (
                <Button type="button" onClick={() => setCreateOpen(true)}>
                  {createLabel ?? `New ${noun}`}
                </Button>
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
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder={`Search ${noun}s`}
                  aria-label={`Search ${noun}s`}
                />
                <ListStatus total={data.totalCount} page={data.pageNumber} noun={`${noun}s`} />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState
                icon={Inbox}
                title={`No ${noun}s`}
                hint={emptyHint ?? (search ? "Nothing matches that search." : `No ${noun}s yet.`)}
              />
            ) : (
              <Table flush aria-label={title}>
                <TableHeader>
                  <TableRow>
                    {columns.map((column) => (
                      <TableHead key={column.header}>{column.header}</TableHead>
                    ))}
                    {(remove || rowActions) && <TableHead className="w-40">Actions</TableHead>}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <TableRow key={item.id}>
                      {columns.map((column) => (
                        <TableCell key={column.header}>{column.cell(item)}</TableCell>
                      ))}
                      {(remove || rowActions) && (
                        <TableCell>
                          <div className="flex flex-wrap items-center gap-2">
                            {rowActions?.(item, { requestDelete: setDeleteId })}
                            {remove && (
                              <Button type="button" variant="ghost" size="sm" onClick={() => setDeleteId(item.id)}>
                                Delete
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      )}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>

      {create && createFields && (
        <Dialog
          open={createOpen}
          onOpenChange={(open) => {
            setCreateOpen(open);
            if (!open) {
              setForm(emptyForm(createFields));
            }
          }}
        >
          <form onSubmit={handleCreate}>
            <DialogHeader>
              <DialogTitle>{createLabel ?? `New ${noun}`}</DialogTitle>
            </DialogHeader>
            <DialogContent className="space-y-4">
              {createFields.map((field) => (
                <CreateFieldControl
                  key={field.key}
                  field={field}
                  value={form[field.key] ?? (field.type === "checkbox" ? false : "")}
                  onChange={(value) => setField(field.key, value, field.autoDeveloperNameFrom)}
                />
              ))}
              {extraCreateFields?.(form, setForm)}
            </DialogContent>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" loading={createMutation.isPending}>
                Create
              </Button>
            </DialogFooter>
          </form>
        </Dialog>
      )}

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

function CreateFieldControl({
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

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function toAutoName(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

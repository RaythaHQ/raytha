import { adminApi } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  EmptyState,
  FormField,
  Input,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  SortableTableHead,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { ChevronDown, Inbox } from "lucide-react";
import { useEffect, useEffectEvent, useState } from "react";
import { useDocumentTitle } from "../lib/document-title";
import {
  compactListQuery,
  nextOrderBy,
  parseOrderBy,
  type ListQuery,
} from "../lib/list-query";
import { entityFields, formatWhen, readBoolean, readString } from "./entity";

type EmailLogSearch = ListQuery & {
  toAddress?: string;
  startDate?: string;
  endDate?: string;
};

const LIST_KEY = "email-log";

export function EmailLogPage() {
  useDocumentTitle(["Email log"]);
  const navigate = useNavigate();
  const location = useLocation();
  const applied = emailLogSearchFromString(location.searchStr);
  const sort = parseOrderBy(applied.orderBy);
  const [draftSearch, setDraftSearch] = useState(applied.search ?? "");
  const [draftTo, setDraftTo] = useState(applied.toAddress ?? "");
  const [syncedSearch, setSyncedSearch] = useState(applied.search);
  const [syncedTo, setSyncedTo] = useState(applied.toAddress);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const appliedKey = JSON.stringify(compactEmailLogSearch(applied));

  if (syncedSearch !== applied.search) {
    setSyncedSearch(applied.search);
    setDraftSearch(applied.search ?? "");
  }
  if (syncedTo !== applied.toAddress) {
    setSyncedTo(applied.toAddress);
    setDraftTo(applied.toAddress ?? "");
  }

  useEffect(() => {
    try {
      sessionStorage.setItem(`raytha.list:${LIST_KEY}`, appliedKey);
    } catch {
      // sessionStorage can be unavailable
    }
  }, [appliedKey]);

  useDebouncedFieldWrite({
    draft: draftSearch,
    applied: applied.search ?? "",
    shouldWrite: alwaysWrite,
    next: (value) => ({
      ...applied,
      search: value.trim() ? value : undefined,
      pageNumber: 1,
    }),
    write: (next) => writeEmailLogSearch(navigate, next),
  });

  useDebouncedFieldWrite({
    draft: draftTo,
    applied: applied.toAddress ?? "",
    shouldWrite: shouldApplyToAddress,
    next: (value) => ({
      ...applied,
      toAddress: value.trim() ? value.trim() : undefined,
      pageNumber: 1,
    }),
    write: (next) => writeEmailLogSearch(navigate, next),
  });

  const query = useQuery({
    queryKey: ["email-log", appliedKey],
    queryFn: () =>
      adminApi.emailLog.list({
        search: applied.search,
        toAddress: applied.toAddress,
        startDate: applied.startDate,
        endDate: applied.endDate,
        pageNumber: applied.pageNumber,
        pageSize: 50,
        orderBy: applied.orderBy,
      }),
    placeholderData: keepPreviousData,
  });

  const pageCount = query.data ? Math.max(1, Math.ceil(query.data.totalCount / query.data.pageSize)) : 1;
  const pageNumber = applied.pageNumber ?? 1;

  return (
    <div className="space-y-6">
      <PageHeader title="Email log" description="Outbound mail, with secrets stripped from the body." />
      <div className="grid gap-4 rounded-xl border border-border bg-card p-4 shadow-card md:grid-cols-2 xl:grid-cols-3">
        <FormField label="Recipient" htmlFor="email-log-to">
          {(control) => (
            <Input
              {...control}
              type="email"
              value={draftTo}
              placeholder="email"
              onChange={(event) => setDraftTo(event.target.value)}
            />
          )}
        </FormField>
        <FormField label="From" htmlFor="email-log-start">
          {(control) => (
            <Input
              {...control}
              type="date"
              value={applied.startDate ?? ""}
              onChange={(event) =>
                writeEmailLogSearch(navigate, {
                  ...applied,
                  startDate: event.target.value || undefined,
                  pageNumber: 1,
                })
              }
            />
          )}
        </FormField>
        <FormField label="To" htmlFor="email-log-end">
          {(control) => (
            <Input
              {...control}
              type="date"
              value={applied.endDate ?? ""}
              onChange={(event) =>
                writeEmailLogSearch(navigate, {
                  ...applied,
                  endDate: event.target.value || undefined,
                  pageNumber: 1,
                })
              }
            />
          )}
        </FormField>
      </div>
      <QueryGate query={query}>
        {(data) => (
          <ListPanel
            toolbar={
              <>
                <ListSearch
                  value={draftSearch}
                  onChange={(event) => setDraftSearch(event.target.value)}
                  placeholder="Search subject or recipient"
                  aria-label="Search email log"
                />
                <ListStatus
                  total={data.totalCount}
                  page={data.pageNumber}
                  noun={data.totalCount === 1 ? "message" : "messages"}
                />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState icon={Inbox} title="No messages" hint="Nothing matches these filters." />
            ) : (
              <Table flush aria-label="Email log">
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-10">
                      <span className="sr-only">Body</span>
                    </TableHead>
                    <SortableTableHead
                      column="CreationTime"
                      label="When"
                      sort={sort.column}
                      dir={sort.dir}
                      naturalDir="desc"
                      onSort={(columnKey, naturalDir) => {
                        writeEmailLogSearch(navigate, {
                          ...applied,
                          pageNumber: 1,
                          orderBy: nextOrderBy(applied.orderBy, columnKey, naturalDir),
                        });
                      }}
                    />
                    <SortableTableHead
                      column="ToAddress"
                      label="To"
                      sort={sort.column}
                      dir={sort.dir}
                      onSort={(columnKey, naturalDir) => {
                        writeEmailLogSearch(navigate, {
                          ...applied,
                          pageNumber: 1,
                          orderBy: nextOrderBy(applied.orderBy, columnKey, naturalDir),
                        });
                      }}
                    />
                    <SortableTableHead
                      column="Subject"
                      label="Subject"
                      sort={sort.column}
                      dir={sort.dir}
                      onSort={(columnKey, naturalDir) => {
                        writeEmailLogSearch(navigate, {
                          ...applied,
                          pageNumber: 1,
                          orderBy: nextOrderBy(applied.orderBy, columnKey, naturalDir),
                        });
                      }}
                    />
                    <TableHead>Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <EmailLogRow
                      key={item.id}
                      item={item}
                      open={expandedId === item.id}
                      onToggle={() => setExpandedId((current) => (current === item.id ? null : item.id))}
                    />
                  ))}
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
            onClick={() => writeEmailLogSearch(navigate, { ...applied, pageNumber: pageNumber - 1 })}
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
            onClick={() => writeEmailLogSearch(navigate, { ...applied, pageNumber: pageNumber + 1 })}
          >
            Next
          </Button>
        </div>
      ) : null}
    </div>
  );
}

function EmailLogRow({ item, open, onToggle }: { item: EntityRef; open: boolean; onToggle: () => void }) {
  const fields = entityFields(item);
  const detail = useQuery({
    queryKey: ["email-log", item.id],
    queryFn: () => adminApi.emailLog.get(item.id),
    enabled: open,
  });
  const detailFields = detail.data ? entityFields(detail.data) : undefined;

  return (
    <>
      <TableRow>
        <TableCell>
          <button
            type="button"
            aria-expanded={open}
            aria-label={open ? "Hide body" : "Show body"}
            onClick={onToggle}
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <ChevronDown className={open ? "size-4 rotate-180" : "size-4"} />
          </button>
        </TableCell>
        <TableCell>{formatWhen(fields.creationTime) || "—"}</TableCell>
        <TableCell>{readString(fields, "toAddress") || "—"}</TableCell>
        <TableCell>{readString(fields, "subject") || "—"}</TableCell>
        <TableCell>
          {readBoolean(fields, "isSuccess") ? (
            <Badge variant="success">Sent</Badge>
          ) : (
            <Badge variant="destructive">Failed</Badge>
          )}
        </TableCell>
      </TableRow>
      {open ? (
        <TableRow>
          <TableCell colSpan={5}>
            <div className="space-y-2">
              <p className="text-sm font-medium">{readString(fields, "subject") || "No subject"}</p>
              {detail.isPending ? (
                <p className="text-sm text-muted-foreground">Loading body…</p>
              ) : detail.isError ? (
                <p className="text-sm text-destructive" role="alert">
                  Could not load this message.
                </p>
              ) : (
                <>
                  {detailFields && readString(detailFields, "errorMessage") ? (
                    <p className="text-sm text-destructive">{readString(detailFields, "errorMessage")}</p>
                  ) : null}
                  <pre className="max-h-80 overflow-auto whitespace-pre-wrap rounded-lg bg-muted p-3 font-mono text-xs">
                    {detailFields && readString(detailFields, "body")
                      ? readString(detailFields, "body")
                      : "No body was recorded."}
                  </pre>
                </>
              )}
            </div>
          </TableCell>
        </TableRow>
      ) : null}
    </>
  );
}

function emailLogSearchFromString(searchString: string): EmailLogSearch {
  const params = new URLSearchParams(searchString.startsWith("?") ? searchString.slice(1) : searchString);
  const query: EmailLogSearch = {};
  const search = params.get("search");
  if (search) {
    query.search = search;
  }
  const pageNumber = params.get("pageNumber");
  if (pageNumber) {
    const parsed = Number(pageNumber);
    if (Number.isFinite(parsed) && parsed > 1) {
      query.pageNumber = Math.floor(parsed);
    }
  }
  const orderBy = params.get("orderBy");
  if (orderBy) {
    query.orderBy = orderBy;
  }
  const toAddress = params.get("toAddress");
  if (toAddress) {
    query.toAddress = toAddress;
  }
  const startDate = params.get("startDate");
  if (startDate) {
    query.startDate = startDate;
  }
  const endDate = params.get("endDate");
  if (endDate) {
    query.endDate = endDate;
  }
  return query;
}

function compactEmailLogSearch(query: EmailLogSearch): Record<string, string> {
  const search = compactListQuery(query);
  if (query.toAddress) {
    search.toAddress = query.toAddress;
  }
  if (query.startDate) {
    search.startDate = query.startDate;
  }
  if (query.endDate) {
    search.endDate = query.endDate;
  }
  return search;
}

function writeEmailLogSearch(
  navigate: ReturnType<typeof useNavigate>,
  query: EmailLogSearch,
): void {
  void navigate({
    to: ".",
    search: compactEmailLogSearch(query),
    replace: true,
  });
}

function alwaysWrite(_value: string): boolean {
  return true;
}

function shouldApplyToAddress(value: string): boolean {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return true;
  }
  const at = trimmed.indexOf("@");
  return at > 0 && trimmed.indexOf(".", at) > at + 1;
}

function useDebouncedFieldWrite({
  draft,
  applied,
  shouldWrite,
  next,
  write,
}: {
  draft: string;
  applied: string;
  shouldWrite: (value: string) => boolean;
  next: (value: string) => EmailLogSearch;
  write: (query: EmailLogSearch) => void;
}) {
  const commit = useEffectEvent((value: string) => write(next(value)));

  useEffect(() => {
    if (draft === applied || !shouldWrite(draft)) {
      return;
    }
    const handle = window.setTimeout(() => {
      commit(draft);
    }, 300);
    return () => window.clearTimeout(handle);
  }, [draft, applied, shouldWrite]);
}

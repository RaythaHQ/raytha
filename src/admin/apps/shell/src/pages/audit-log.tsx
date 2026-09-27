import { adminApi } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Button,
  EmptyState,
  FormField,
  Input,
  ListPanel,
  ListSearch,
  PageHeader,
  QueryGate,
  Select,
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
import { useState } from "react";
import { useDocumentTitle } from "../lib/document-title";
import { compactListQuery, listQueryFromSearchString, nextOrderBy, parseOrderBy } from "../lib/list-query";
import { entityFields, formatWhen, humanizeAuditCategory, readString } from "./entity";

export function AuditLogPage() {
  useDocumentTitle(["Audit log"]);
  const navigate = useNavigate();
  const location = useLocation();
  const listQuery = listQueryFromSearchString(location.searchStr);
  const orderBy = listQuery.orderBy;
  const sort = parseOrderBy(orderBy);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [userEmail, setUserEmail] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const categories = useQuery({
    queryKey: ["audit-log-categories"],
    queryFn: () => adminApi.auditLogs.categories(),
  });

  const query = useQuery({
    queryKey: ["audit-logs", search, category, userEmail, startDate, endDate, pageNumber, orderBy],
    queryFn: () =>
      adminApi.auditLogs.list({
        search: search.trim() || undefined,
        category: category || undefined,
        userEmail: userEmail.trim() || undefined,
        startDate: startDate ? new Date(`${startDate}T00:00:00`).toISOString() : undefined,
        endDate: endDate ? new Date(`${endDate}T23:59:59.999`).toISOString() : undefined,
        pageNumber,
        pageSize: 50,
        orderBy,
      }),
    placeholderData: keepPreviousData,
  });

  const pageCount = query.data ? Math.max(1, Math.ceil(query.data.totalCount / query.data.pageSize)) : 1;

  return (
    <div className="space-y-6">
      <PageHeader title="Audit log" description="Who changed what, and when." />
      <div className="grid gap-4 rounded-xl border border-border bg-card p-4 shadow-card md:grid-cols-2 xl:grid-cols-4">
        <FormField label="Category" htmlFor="audit-category">
          {(control) => (
            <Select
              {...control}
              value={category}
              onChange={(event) => {
                setCategory(event.target.value);
                setPageNumber(1);
              }}
            >
              <option value="">All categories</option>
              {(categories.data ?? []).map((item) => (
                <option key={item} value={item}>
                  {humanizeAuditCategory(item)}
                </option>
              ))}
            </Select>
          )}
        </FormField>
        <FormField label="User" htmlFor="audit-user">
          {(control) => (
            <Input
              {...control}
              value={userEmail}
              placeholder="email"
              onChange={(event) => {
                setUserEmail(event.target.value);
                setPageNumber(1);
              }}
            />
          )}
        </FormField>
        <FormField label="From" htmlFor="audit-start">
          {(control) => (
            <Input
              {...control}
              type="date"
              value={startDate}
              onChange={(event) => {
                setStartDate(event.target.value);
                setPageNumber(1);
              }}
            />
          )}
        </FormField>
        <FormField label="To" htmlFor="audit-end">
          {(control) => (
            <Input
              {...control}
              type="date"
              value={endDate}
              onChange={(event) => {
                setEndDate(event.target.value);
                setPageNumber(1);
              }}
            />
          )}
        </FormField>
      </div>
      <QueryGate query={query}>
        {(data) => (
          <ListPanel
            toolbar={
              <ListSearch
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPageNumber(1);
                }}
                placeholder="Search entries"
                aria-label="Search audit log"
              />
            }
          >
            {data.items.length === 0 ? (
              <EmptyState icon={Inbox} title="No audit entries" hint="Nothing matches these filters." />
            ) : (
              <Table flush aria-label="Audit log">
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-10"><span className="sr-only">Changes</span></TableHead>
                    <TableHead>When</TableHead>
                    <SortableTableHead
                      column="Category"
                      label="Category"
                      sort={sort.column}
                      dir={sort.dir}
                      onSort={(columnKey, naturalDir) => {
                        setPageNumber(1);
                        void navigate({
                          to: ".",
                          search: compactListQuery({ orderBy: nextOrderBy(orderBy, columnKey, naturalDir) }),
                          replace: true,
                        });
                      }}
                    />
                    <TableHead>User</TableHead>
                    <TableHead>IP</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <AuditRow
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
          <Button type="button" variant="outline" disabled={pageNumber <= 1} onClick={() => setPageNumber((page) => page - 1)}>
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {pageNumber} of {pageCount}
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={pageNumber >= pageCount}
            onClick={() => setPageNumber((page) => page + 1)}
          >
            Next
          </Button>
        </div>
      ) : null}
    </div>
  );
}

function AuditRow({ item, open, onToggle }: { item: EntityRef; open: boolean; onToggle: () => void }) {
  const fields = entityFields(item);
  return (
    <>
      <TableRow>
        <TableCell>
          <button
            type="button"
            aria-expanded={open}
            aria-label={open ? "Hide changes" : "Show changes"}
            onClick={onToggle}
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <ChevronDown className={open ? "size-4 rotate-180" : "size-4"} />
          </button>
        </TableCell>
        <TableCell>{formatWhen(fields.creationTime) || "—"}</TableCell>
        <TableCell>{humanizeAuditCategory(readString(fields, "category") || "—")}</TableCell>
        <TableCell>{readString(fields, "userEmail") || "—"}</TableCell>
        <TableCell>{readString(fields, "ipAddress") || "—"}</TableCell>
      </TableRow>
      {open ? (
        <TableRow>
          <TableCell colSpan={5}>
            <pre className="max-h-80 overflow-auto whitespace-pre-wrap rounded-lg bg-muted p-3 font-mono text-xs">
              {formatRequest(fields.request)}
            </pre>
          </TableCell>
        </TableRow>
      ) : null}
    </>
  );
}

function formatRequest(value: unknown): string {
  if (typeof value !== "string" || value.trim().length === 0) {
    return "No change details were recorded.";
  }
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

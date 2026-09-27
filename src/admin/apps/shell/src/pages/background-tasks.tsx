import { adminApi, formatError, hasPermission, parseTaskMediaItem, platformPermissions } from "@raytha/api";
import type { BackgroundTaskDetail, BackgroundTaskStatusName } from "@raytha/api";
import {
  Button,
  ConfirmDialog,
  EmptyState,
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
  cn,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useLocation, useNavigate } from "@tanstack/react-router";
import { ChevronDown, ChevronLeft, Inbox } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import {
  isTaskRunning,
  TASK_STATUSES,
  taskDuration,
  taskLabel,
  TaskProgress,
  TaskStatusBadge,
} from "../components/background-task-view";
import { PermissionRequired } from "../components/permission-required";
import { useDocumentTitle } from "../lib/document-title";
import { compactListQuery, listQueryFromSearchString, listQueryKey, rememberListQuery, type ListQuery } from "../lib/list-query";
import { MAINTENANCE_QUERY_KEY } from "./maintenance";
import { relativeTime } from "./site-pages/page-meta";

const LIST_KEY = "background-tasks";
const PAGE_SIZE = 50;

type TaskListQuery = ListQuery & { status?: BackgroundTaskStatusName };

function readTaskListQuery(searchString: string): TaskListQuery {
  const status = new URLSearchParams(searchString).get("status");
  const known = TASK_STATUSES.find((item) => item.status === status)?.status;
  return { ...listQueryFromSearchString(searchString), ...(known ? { status: known } : {}) };
}

export function BackgroundTasksPage() {
  useDocumentTitle(["Background tasks"]);
  if (!hasPermission(platformPermissions.systemSettings)) {
    return <PermissionRequired title="Background tasks" permissionLabel="Manage System Settings" />;
  }
  return <BackgroundTasksContent />;
}

function BackgroundTasksContent() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const applied = readTaskListQuery(location.searchStr);
  const appliedKey = `${listQueryKey(applied)}:${applied.status ?? ""}`;
  const appliedSearch = applied.search ?? "";
  const [draftSearch, setDraftSearch] = useState(appliedSearch);
  const [syncedSearch, setSyncedSearch] = useState(appliedSearch);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [confirmingClear, setConfirmingClear] = useState(false);
  const searchTimer = useRef<number | undefined>(undefined);

  if (appliedSearch !== syncedSearch) {
    setSyncedSearch(appliedSearch);
    setDraftSearch(appliedSearch);
  }

  const write = (next: TaskListQuery) => {
    void navigate({
      to: ".",
      search: { ...compactListQuery(next), ...(next.status ? { status: next.status } : {}) },
      replace: true,
    });
  };

  const changeSearch = (value: string) => {
    setDraftSearch(value);
    window.clearTimeout(searchTimer.current);
    searchTimer.current = window.setTimeout(() => {
      write({ ...applied, search: value.trim() || undefined, pageNumber: 1 });
    }, 300);
  };

  useEffect(() => () => window.clearTimeout(searchTimer.current), []);

  const { search, pageNumber, orderBy } = applied;
  useEffect(() => {
    rememberListQuery(LIST_KEY, { search, pageNumber, orderBy });
  }, [search, pageNumber, orderBy]);

  const snapshot = useQuery({
    queryKey: MAINTENANCE_QUERY_KEY,
    queryFn: () => adminApi.maintenance.snapshot(),
  });

  const query = useQuery({
    queryKey: ["background-tasks", appliedKey],
    queryFn: () =>
      adminApi.backgroundTasks.list({
        search: applied.search,
        status: applied.status,
        pageNumber: applied.pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
    refetchInterval: (current) => (current.state.data?.items.some(isTaskRunning) ? 3000 : false),
  });

  const clear = useMutation({
    mutationFn: () => adminApi.maintenance.clearLog("background_tasks"),
    onSuccess: (result) => {
      toast.success(
        result.deleted === null ? "Cleared finished tasks" : `Deleted ${result.deleted.toLocaleString()} finished tasks`,
      );
      setConfirmingClear(false);
      void queryClient.invalidateQueries({ queryKey: ["background-tasks"] });
      void queryClient.invalidateQueries({ queryKey: MAINTENANCE_QUERY_KEY });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const counts = snapshot.data?.backgroundTasks;
  const finished = counts ? counts.complete + counts.error : null;
  const total = counts ? counts.enqueued + counts.processing + counts.complete + counts.error : null;
  const retentionDays = snapshot.data?.logs.find((log) => log.key === "background_tasks")?.retentionDays ?? null;

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <Link
            to="/maintenance"
            className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <ChevronLeft className="size-4" aria-hidden />
            Maintenance
          </Link>
        }
        title="Background tasks"
        description={
          retentionDays === null
            ? "Imports, exports, and theme jobs queued on this server."
            : retentionDays === 0
              ? "Imports, exports, and theme jobs queued on this server. Finished tasks are kept forever."
              : `Imports, exports, and theme jobs queued on this server. Finished tasks are deleted after ${retentionDays} days.`
        }
        actions={
          <Button
            type="button"
            variant="outline"
            className="text-destructive hover:border-destructive/40 hover:bg-destructive/5 hover:text-destructive"
            disabled={finished === 0}
            onClick={() => setConfirmingClear(true)}
          >
            Clear finished tasks
          </Button>
        }
      />
      <div className="flex flex-wrap gap-2" role="group" aria-label="Filter by status">
        <StatusFilterButton
          label="All"
          count={total}
          active={applied.status === undefined}
          onClick={() => write({ ...applied, status: undefined, pageNumber: 1 })}
        />
        {TASK_STATUSES.map(({ status, label }) => (
          <StatusFilterButton
            key={status}
            label={label}
            count={counts ? counts[status] : null}
            active={applied.status === status}
            onClick={() => write({ ...applied, status, pageNumber: 1 })}
          />
        ))}
      </div>
      <QueryGate query={query}>
        {(data) => {
          const pageCount = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
          return (
            <>
              <ListPanel
                toolbar={
                  <>
                    <ListSearch
                      value={draftSearch}
                      onChange={(event) => changeSearch(event.target.value)}
                      placeholder="Search by name or error"
                      aria-label="Search background tasks"
                    />
                    <ListStatus total={data.totalCount} page={data.pageNumber} noun="tasks" />
                  </>
                }
              >
                {data.items.length === 0 ? (
                  <EmptyState
                    icon={Inbox}
                    title="No background tasks"
                    hint={
                      applied.search || applied.status
                        ? "Nothing matches these filters."
                        : "Theme imports, CSV imports and exports, and duplications show up here while they run."
                    }
                  />
                ) : (
                  <Table flush aria-label="Background tasks">
                    <TableHeader>
                      <TableRow>
                        <TableHead className="w-10">
                          <span className="sr-only">Details</span>
                        </TableHead>
                        <TableHead>Task</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead>Progress</TableHead>
                        <TableHead>Started</TableHead>
                        <TableHead>Duration</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {data.items.map((task) => (
                        <TaskRow
                          key={task.id}
                          task={task}
                          open={expandedId === task.id}
                          onToggle={() => setExpandedId((current) => (current === task.id ? null : task.id))}
                        />
                      ))}
                    </TableBody>
                  </Table>
                )}
              </ListPanel>
              {pageCount > 1 ? (
                <div className="flex items-center justify-end gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    disabled={data.pageNumber <= 1}
                    onClick={() => write({ ...applied, pageNumber: data.pageNumber - 1 })}
                  >
                    Previous
                  </Button>
                  <span className="text-sm text-muted-foreground">
                    Page {data.pageNumber} of {pageCount}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    disabled={data.pageNumber >= pageCount}
                    onClick={() => write({ ...applied, pageNumber: data.pageNumber + 1 })}
                  >
                    Next
                  </Button>
                </div>
              ) : null}
            </>
          );
        }}
      </QueryGate>
      <ConfirmDialog
        open={confirmingClear}
        onOpenChange={setConfirmingClear}
        title="Clear finished tasks?"
        body={
          <div className="space-y-2">
            <p>
              This permanently deletes{" "}
              <strong className="text-foreground">
                {finished === null ? "every" : finished.toLocaleString()} completed and failed{" "}
                {finished === 1 ? "task" : "tasks"}
              </strong>
              . It cannot be undone.
            </p>
            <p>Queued and running tasks are kept.</p>
          </div>
        }
        confirmLabel={finished === null ? "Delete" : `Delete ${finished.toLocaleString()}`}
        pending={clear.isPending}
        onConfirm={() => clear.mutate()}
      />
    </div>
  );
}

function StatusFilterButton({
  label,
  count,
  active,
  onClick,
}: {
  label: string;
  count: number | null;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={cn(
        "inline-flex h-8 items-center gap-2 rounded-full border px-3 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
        active
          ? "border-primary bg-primary text-primary-foreground"
          : "border-border bg-card text-foreground hover:border-brand-300",
      )}
    >
      {label}
      {count !== null ? (
        <span className={cn("tabular-nums text-xs", active ? "text-primary-foreground/80" : "text-muted-foreground")}>
          {count.toLocaleString()}
        </span>
      ) : null}
    </button>
  );
}

function TaskRow({ task, open, onToggle }: { task: BackgroundTaskDetail; open: boolean; onToggle: () => void }) {
  const created = new Date(task.creationTime);
  const file = parseTaskMediaItem(task.statusInfo);
  return (
    <>
      <TableRow>
        <TableCell>
          <button
            type="button"
            aria-expanded={open}
            aria-label={open ? "Hide details" : "Show details"}
            onClick={onToggle}
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
          >
            <ChevronDown className={open ? "size-4 rotate-180" : "size-4"} />
          </button>
        </TableCell>
        <TableCell>
          <p className="font-medium">{taskLabel(task.name)}</p>
          {task.status === "error" && task.errorMessage ? (
            <p className="max-w-md truncate text-xs text-destructive" title={task.errorMessage}>
              {task.errorMessage}
            </p>
          ) : task.statusInfo && !file ? (
            <p className="max-w-md truncate text-xs text-muted-foreground">{task.statusInfo}</p>
          ) : null}
        </TableCell>
        <TableCell>
          <TaskStatusBadge status={task.status} />
        </TableCell>
        <TableCell>
          <TaskProgress task={task} />
        </TableCell>
        <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
          {Number.isNaN(created.getTime()) ? (
            "—"
          ) : (
            <time dateTime={task.creationTime} title={created.toLocaleString()}>
              {relativeTime(created)}
            </time>
          )}
        </TableCell>
        <TableCell className="whitespace-nowrap text-sm tabular-nums text-muted-foreground">
          {taskDuration(task) || "—"}
          {isTaskRunning(task) ? <span className="sr-only"> so far</span> : null}
        </TableCell>
      </TableRow>
      {open ? (
        <TableRow>
          <TableCell colSpan={6} className="bg-muted/30">
            <TaskDetails task={task} fileUrl={file?.downloadUrl} fileName={file?.fileName} />
          </TableCell>
        </TableRow>
      ) : null}
    </>
  );
}

function TaskDetails({ task, fileUrl, fileName }: { task: BackgroundTaskDetail; fileUrl?: string; fileName?: string }) {
  const facts: [string, string][] = [
    ["Task id", task.id],
    ["Type", task.name.split(",")[0] || "—"],
    ["Step", String(task.taskStep)],
    ["Queued", formatStamp(task.creationTime)],
    ["Last update", formatStamp(task.lastModificationTime)],
    ["Finished", formatStamp(task.completionTime)],
  ];
  return (
    <div className="space-y-3 py-1">
      <dl className="grid gap-x-6 gap-y-1 text-sm sm:grid-cols-2 lg:grid-cols-3">
        {facts.map(([label, value]) => (
          <div key={label} className="flex gap-2">
            <dt className="text-muted-foreground">{label}</dt>
            <dd className="min-w-0 truncate font-medium" title={value}>
              {value}
            </dd>
          </div>
        ))}
      </dl>
      {fileUrl ? (
        <a href={fileUrl} className="text-sm text-primary hover:underline" target="_blank" rel="noreferrer">
          Download {fileName || "file"}
        </a>
      ) : task.statusInfo ? (
        <p className="text-sm">{task.statusInfo}</p>
      ) : null}
      {task.errorMessage ? (
        <pre className="max-h-80 overflow-auto whitespace-pre-wrap rounded-lg border border-destructive/30 bg-destructive/5 p-3 font-mono text-xs text-destructive">
          {task.errorMessage}
        </pre>
      ) : null}
    </div>
  );
}

function formatStamp(value: string): string {
  const date = new Date(value);
  return value && !Number.isNaN(date.getTime()) ? date.toLocaleString() : "—";
}

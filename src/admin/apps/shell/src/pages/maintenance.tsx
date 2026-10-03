import { adminApi, formatError, hasPermission, platformPermissions, RETENTION_FIELD } from "@raytha/api";
import type { BackgroundTaskDetail, LogRetention, MaintenanceSnapshot, RetainedLogKey, RetainedLogStats, SizeUsage } from "@raytha/api";
import {
  Badge,
  Button,
  buttonVariants,
  Card,
  CardContent,
  ConfirmDialog,
  EmptyState,
  Input,
  QueryGate,
  PageHeader,
  Skeleton,
  toast,
  cn,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import type { LucideIcon } from "lucide-react";
import { ArrowRight, Database, HardDrive, Inbox, ListChecks, Mail, ScrollText, Webhook } from "lucide-react";
import { useState, type FormEvent } from "react";
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
import { relativeTime } from "./site-pages/page-meta";

type LogPresentation = {
  icon: LucideIcon;
  description: string;
  to: string;
  noun: string;
};

const LOGS: Record<RetainedLogKey, LogPresentation> = {
  audit_logs: {
    icon: ScrollText,
    description: "Admin changes, sign-ins, and setting updates.",
    to: "/audit-log",
    noun: "audit log entries",
  },
  email_logs: {
    icon: Mail,
    description: "Every outbound email and whether it was delivered.",
    to: "/email-log",
    noun: "email log entries",
  },
  webhook_deliveries: {
    icon: Webhook,
    description: "Payloads sent to webhook endpoints and their responses.",
    to: "/webhooks",
    noun: "webhook deliveries",
  },
  background_tasks: {
    icon: ListChecks,
    description: "Completed and failed imports, exports, and theme jobs.",
    to: "/background-tasks",
    noun: "finished background tasks",
  },
};

export const MAINTENANCE_QUERY_KEY = ["maintenance"] as const;

export function MaintenancePage() {
  useDocumentTitle(["Maintenance"]);
  if (!hasPermission(platformPermissions.systemSettings)) {
    return <PermissionRequired title="Maintenance" permissionLabel="Manage System Settings" />;
  }
  return <MaintenanceContent />;
}

function MaintenanceContent() {
  const query = useQuery({
    queryKey: MAINTENANCE_QUERY_KEY,
    queryFn: () => adminApi.maintenance.snapshot(),
  });

  return (
    <div className="space-y-8">
      <PageHeader
        title="Maintenance"
        description="Keep the database lean. Set how long logs are kept, clear them on demand, and watch background jobs."
        meta={
          query.data ? (
            <>
              {query.data.version ? <Badge variant="outline">v{query.data.version}</Badge> : null}
              {query.data.environment ? <Badge variant="secondary">{query.data.environment}</Badge> : null}
            </>
          ) : undefined
        }
        actions={
          <Link to="/background-tasks" className={buttonVariants({ variant: "outline" })}>
            <ListChecks aria-hidden />
            Background tasks
          </Link>
        }
      />
      <QueryGate query={query}>
        {(snapshot) => {
          const retention = retentionFrom(snapshot.logs);
          const tasksLog = snapshot.logs.find((log) => log.key === "background_tasks");
          return (
            <>
              <UsageCards snapshot={snapshot} />
              <RetentionSection logs={snapshot.logs.filter((log) => log !== tasksLog)} retention={retention} />
              <TestEmailSection />
              <BackgroundTasksSection snapshot={snapshot} tasksLog={tasksLog} retention={retention} />
            </>
          );
        }}
      </QueryGate>
    </div>
  );
}

function UsageCards({ snapshot }: { snapshot: MaintenanceSnapshot }) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <UsageCard icon={Database} title="Database" usage={snapshot.database} detail="Postgres" />
      <UsageCard
        icon={HardDrive}
        title="File storage"
        usage={snapshot.storage}
        detail={[
          snapshot.storage.provider,
          `${snapshot.storage.fileCount.toLocaleString()} ${snapshot.storage.fileCount === 1 ? "file" : "files"}`,
        ]
          .filter(Boolean)
          .join(" · ")}
      />
    </div>
  );
}

function UsageCard({ icon: Icon, title, usage, detail }: { icon: LucideIcon; title: string; usage: SizeUsage; detail: string }) {
  const percent = usage.maxBytes > 0 ? Math.min(100, Math.round((usage.sizeBytes / usage.maxBytes) * 100)) : null;
  return (
    <Card>
      <CardContent className="flex items-start gap-4 pt-6">
        <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
          <Icon className="size-5" aria-hidden />
        </span>
        <div className="min-w-0 flex-1 space-y-1">
          <p className="text-sm text-muted-foreground">{title}</p>
          <p className="font-display text-2xl font-semibold tracking-tight">{usage.sizeDisplay || "—"}</p>
          <p className="text-xs text-muted-foreground">
            {detail}
            {percent !== null ? ` · ${percent}% of ${usage.maxDisplay}` : ""}
          </p>
          {percent !== null ? (
            <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-muted" aria-hidden>
              <div
                className={cn("h-full rounded-full", percent >= 90 ? "bg-destructive" : "bg-primary")}
                style={{ width: `${percent}%` }}
              />
            </div>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}

function retentionFrom(logs: RetainedLogStats[]): LogRetention | null {
  const days = (key: RetainedLogKey) => logs.find((log) => log.key === key)?.retentionDays ?? null;
  const audit = days("audit_logs");
  const email = days("email_logs");
  const webhooks = days("webhook_deliveries");
  const tasks = days("background_tasks");
  if (audit === null || email === null || webhooks === null || tasks === null) {
    return null;
  }
  return {
    auditLogRetentionDays: audit,
    emailLogRetentionDays: email,
    webhookDeliveryRetentionDays: webhooks,
    backgroundTaskRetentionDays: tasks,
  };
}

function RetentionSection({ logs, retention }: { logs: RetainedLogStats[]; retention: LogRetention | null }) {
  return (
    <section className="space-y-3" aria-labelledby="retention-heading">
      <div className="space-y-1">
        <h2 id="retention-heading" className="font-display text-lg font-semibold tracking-tight">
          Data retention
        </h2>
        <p className="text-sm text-muted-foreground">
          Entries older than the window are deleted every day at 03:00 UTC. Set a window to 0 to keep entries forever.
        </p>
      </div>
      {retention === null ? (
        <p className="rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-sm" role="status">
          Retention settings are not available from this server yet. Restart it on the latest build to edit them.
        </p>
      ) : null}
      <Card className="divide-y divide-border overflow-hidden p-0">
        {logs.map((log) => (
          <RetentionRow key={`${log.key}:${log.retentionDays ?? "na"}`} log={log} retention={retention} />
        ))}
      </Card>
    </section>
  );
}

function ClearBody({ log }: { log: RetainedLogStats }) {
  return (
    <div className="space-y-2">
      <p>
        This permanently deletes{" "}
        <strong className="text-foreground">
          {log.rowCount.toLocaleString()} {LOGS[log.key].noun}
        </strong>
        . It cannot be undone.
      </p>
      {log.key === "background_tasks" ? <p>Queued and running tasks are kept.</p> : null}
      {log.key === "audit_logs" ? <p>The clear itself is recorded as the first entry of the new audit log.</p> : null}
    </div>
  );
}

function RetentionRow({
  log,
  retention,
  clearLabel = "Clear now",
}: {
  log: RetainedLogStats;
  retention: LogRetention | null;
  clearLabel?: string;
}) {
  const queryClient = useQueryClient();
  const [confirming, setConfirming] = useState(false);
  const presentation = LOGS[log.key];
  const Icon = presentation.icon;
  const [draft, setDraft] = useState(log.retentionDays === null ? "" : String(log.retentionDays));
  const parsed = /^\d+$/.test(draft) ? Number(draft) : Number.NaN;
  const valid = Number.isInteger(parsed) && parsed >= 0 && parsed <= 3650;
  const dirty = valid && parsed !== log.retentionDays;
  const inputId = `retention-${log.key}`;
  const hintId = `${inputId}-hint`;

  const save = useMutation({
    mutationFn: (days: number) => {
      if (!retention) {
        throw new Error("Retention settings are not available from this server yet.");
      }
      return adminApi.maintenance.updateRetention({ ...retention, [RETENTION_FIELD[log.key]]: days });
    },
    onSuccess: (_result, days) => {
      toast.success(days === 0 ? `${log.label} is now kept forever` : `${log.label} keeps ${days} days`);
      void queryClient.invalidateQueries({ queryKey: MAINTENANCE_QUERY_KEY });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const clear = useMutation({
    mutationFn: () => adminApi.maintenance.clearLog(log.key),
    onSuccess: (result) => {
      const noun = presentation.noun;
      toast.success(result.deleted === null ? `Cleared ${noun}` : `Deleted ${result.deleted.toLocaleString()} ${noun}`);
      setConfirming(false);
      void queryClient.invalidateQueries({ queryKey: MAINTENANCE_QUERY_KEY });
      void queryClient.invalidateQueries({ queryKey: ["background-tasks"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (dirty) {
      save.mutate(parsed);
    }
  };

  const oldest = log.oldestEntry ? new Date(log.oldestEntry) : null;

  return (
    <div className="grid gap-4 px-5 py-4 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_minmax(0,1.3fr)_auto] lg:items-center">
      <div className="flex min-w-0 items-start gap-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
          <Icon className="size-4" aria-hidden />
        </span>
        <div className="min-w-0">
          <Link to={presentation.to} className="font-medium hover:text-primary hover:underline">
            {log.label}
          </Link>
          <p className="text-xs text-muted-foreground">{presentation.description}</p>
        </div>
      </div>
      <dl className="grid grid-cols-2 gap-3 text-sm lg:grid-cols-1 lg:gap-1">
        <div className="flex items-baseline gap-1.5">
          <dt className="sr-only">Rows</dt>
          <dd className="font-display text-lg font-semibold tabular-nums">{log.rowCount.toLocaleString()}</dd>
          <span className="text-xs text-muted-foreground">{log.rowCount === 1 ? "row" : "rows"}</span>
        </div>
        <div className="text-xs text-muted-foreground">
          <dt className="inline">Oldest </dt>
          <dd className="inline">
            {oldest && !Number.isNaN(oldest.getTime()) ? (
              <time dateTime={log.oldestEntry ?? undefined} title={oldest.toLocaleString()} className="text-foreground">
                {relativeTime(oldest)}
              </time>
            ) : (
              "—"
            )}
          </dd>
        </div>
      </dl>
      <form className="flex flex-wrap items-center gap-2" onSubmit={handleSubmit}>
        <label htmlFor={inputId} className="sr-only">
          {log.label} retention in days
        </label>
        <div className="relative w-32">
          <Input
            id={inputId}
            type="number"
            inputMode="numeric"
            min={0}
            max={3650}
            step={1}
            value={draft}
            disabled={retention === null}
            aria-describedby={hintId}
            aria-invalid={draft !== "" && !valid}
            className="pr-12 tabular-nums"
            onChange={(event) => setDraft(event.target.value)}
          />
          <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-xs text-muted-foreground">
            days
          </span>
        </div>
        <Button type="submit" size="sm" variant={dirty ? "default" : "outline"} disabled={!dirty} loading={save.isPending}>
          Save
        </Button>
        <p id={hintId} className={cn("w-full text-xs", draft !== "" && !valid ? "text-destructive" : "text-muted-foreground")}>
          {retentionHint(draft, valid, parsed)}
        </p>
      </form>
      <div className="lg:text-right">
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="text-destructive hover:border-destructive/40 hover:bg-destructive/5 hover:text-destructive"
          disabled={log.rowCount === 0}
          onClick={() => setConfirming(true)}
        >
          {clearLabel}
        </Button>
      </div>
      <ConfirmDialog
        open={confirming}
        onOpenChange={setConfirming}
        title={`Clear ${log.label.toLowerCase()}?`}
        body={<ClearBody log={log} />}
        confirmLabel={`Delete ${log.rowCount.toLocaleString()}`}
        pending={clear.isPending}
        onConfirm={() => clear.mutate()}
      />
    </div>
  );
}

function retentionHint(draft: string, valid: boolean, days: number): string {
  if (draft === "") {
    return "Enter 0 to keep forever.";
  }
  if (!valid) {
    return "Use a whole number from 0 to 3650.";
  }
  if (days === 0) {
    return "Kept forever.";
  }
  return `Deletes entries older than ${days} ${days === 1 ? "day" : "days"}.`;
}

const EMAIL_SHAPE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Sends one message through the configured SMTP settings so an operator can prove email works. */
function TestEmailSection() {
  const [address, setAddress] = useState("");
  const [touched, setTouched] = useState(false);
  const trimmed = address.trim();
  const valid = EMAIL_SHAPE.test(trimmed);
  const showInvalid = touched && trimmed.length > 0 && !valid;

  const send = useMutation({
    mutationFn: (to: string) => adminApi.maintenance.sendTestEmail(to),
    onSuccess: (result) => toast.success(`Test email sent to ${result.emailAddress}`),
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (valid) {
      send.mutate(trimmed);
    }
  };

  return (
    <section className="space-y-3" aria-labelledby="test-email-heading">
      <div className="space-y-1">
        <h2 id="test-email-heading" className="font-display text-lg font-semibold tracking-tight">
          Test email
        </h2>
        <p className="text-sm text-muted-foreground">
          Sends one message with the SMTP settings in use. The attempt is recorded in the{" "}
          <Link to="/email-log" className="underline-offset-4 hover:underline">
            email log
          </Link>{" "}
          either way.
        </p>
      </div>
      <Card>
        <CardContent className="pt-6">
          <form className="flex flex-wrap items-start gap-2" onSubmit={handleSubmit} noValidate>
            <div className="w-full max-w-sm space-y-1">
              <label htmlFor="test-email-address" className="sr-only">
                Recipient email address
              </label>
              <Input
                id="test-email-address"
                type="email"
                inputMode="email"
                autoComplete="email"
                placeholder="you@example.com"
                value={address}
                aria-invalid={showInvalid}
                aria-describedby="test-email-hint"
                onChange={(event) => setAddress(event.target.value)}
                onBlur={() => setTouched(true)}
              />
              <p id="test-email-hint" className={cn("text-xs", showInvalid ? "text-destructive" : "text-muted-foreground")}>
                {showInvalid ? "Enter a valid email address." : "Where to send the test message."}
              </p>
            </div>
            <Button type="submit" disabled={!valid} loading={send.isPending}>
              <Mail aria-hidden />
              Send test email
            </Button>
          </form>
          {send.isError ? (
            <p className="mt-3 text-sm text-destructive" role="alert">
              {formatError(send.error)}
            </p>
          ) : send.isSuccess ? (
            <p className="mt-3 text-sm text-success" role="status">
              Sent to {send.data.emailAddress}. Check the inbox, or the email log if it does not arrive.
            </p>
          ) : null}
        </CardContent>
      </Card>
    </section>
  );
}

function BackgroundTasksSection({
  snapshot,
  tasksLog,
  retention,
}: {
  snapshot: MaintenanceSnapshot;
  tasksLog: RetainedLogStats | undefined;
  retention: LogRetention | null;
}) {
  const recent = useQuery({
    queryKey: ["background-tasks", "recent"],
    queryFn: () => adminApi.backgroundTasks.list({ pageSize: 6 }),
    refetchInterval: (query) => (query.state.data?.items.some(isTaskRunning) ? 3000 : false),
  });

  return (
    <section className="space-y-3" aria-labelledby="tasks-heading">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="space-y-1">
          <h2 id="tasks-heading" className="font-display text-lg font-semibold tracking-tight">
            Background tasks
          </h2>
          <p className="text-sm text-muted-foreground">
            Imports, exports, and theme jobs queued on this server.
          </p>
        </div>
        <Link to="/background-tasks" className={buttonVariants({ variant: "ghost", size: "sm" })}>
          View all tasks
          <ArrowRight aria-hidden />
        </Link>
      </div>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {TASK_STATUSES.map(({ status, label }) => (
          <Link
            key={status}
            to="/background-tasks"
            search={{ status }}
            className="rounded-xl border border-border bg-card px-4 py-3 shadow-card transition-colors hover:border-brand-300"
          >
            <p className="text-xs text-muted-foreground">{label}</p>
            <p className="font-display text-xl font-semibold tabular-nums">
              {snapshot.backgroundTasks[status].toLocaleString()}
            </p>
          </Link>
        ))}
      </div>
      <Card className="divide-y divide-border overflow-hidden p-0">
        {tasksLog ? <RetentionRow log={tasksLog} retention={retention} clearLabel="Clear finished" /> : null}
        {recent.isPending ? (
          <div className="space-y-2 p-4" aria-busy="true">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        ) : recent.isError ? (
          <p className="px-5 py-4 text-sm text-destructive" role="alert">
            {formatError(recent.error)}
          </p>
        ) : recent.data.items.length === 0 ? (
          <EmptyState
            icon={Inbox}
            title="No background tasks yet"
            hint="Theme imports, CSV imports and exports, and duplications show up here while they run."
            className="border-0"
          />
        ) : (
          <ul className="divide-y divide-border">
            {recent.data.items.map((task) => (
              <RecentTaskRow key={task.id} task={task} />
            ))}
          </ul>
        )}
      </Card>
    </section>
  );
}

function RecentTaskRow({ task }: { task: BackgroundTaskDetail }) {
  const created = new Date(task.creationTime);
  const running = isTaskRunning(task);
  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-1 px-5 py-3 text-sm">
      <TaskStatusBadge status={task.status} />
      <span className="min-w-0 flex-1 truncate font-medium" title={task.name}>
        {taskLabel(task.name)}
      </span>
      {running ? (
        <TaskProgress task={task} />
      ) : (
        <span className="text-xs text-muted-foreground">{taskDuration(task)}</span>
      )}
      <span className="w-28 text-right text-xs text-muted-foreground">
        {Number.isNaN(created.getTime()) ? "" : (
          <time dateTime={task.creationTime} title={created.toLocaleString()}>
            {relativeTime(created)}
          </time>
        )}
      </span>
      {task.status === "error" && task.errorMessage ? (
        <p className="w-full truncate text-xs text-destructive" title={task.errorMessage}>
          {task.errorMessage}
        </p>
      ) : null}
    </li>
  );
}

import type { BackgroundTaskDetail, BackgroundTaskStatusName } from "@raytha/api";
import { Badge, type BadgeProps, cn } from "@raytha/ui";

export const TASK_STATUSES: { status: BackgroundTaskStatusName; label: string }[] = [
  { status: "enqueued", label: "Queued" },
  { status: "processing", label: "Running" },
  { status: "complete", label: "Complete" },
  { status: "error", label: "Failed" },
];

const STATUS_VARIANT: Record<BackgroundTaskStatusName, BadgeProps["variant"]> = {
  enqueued: "secondary",
  processing: "info",
  complete: "success",
  error: "destructive",
};

export function isTaskRunning(task: BackgroundTaskDetail): boolean {
  return task.status === "enqueued" || task.status === "processing";
}

const ACRONYMS = new Set(["csv", "json", "url", "api", "html", "pdf"]);

/**
 * Tasks are named by their assembly-qualified .NET type, e.g.
 * `Raytha.Application.Themes.Commands.BeginDuplicateTheme+BackgroundTask, Raytha.Application, …`.
 * Turns that into "Duplicate theme".
 */
export function taskLabel(name: string): string {
  const typeName = name.split(",")[0]?.trim() ?? "";
  const nested = typeName.split("+");
  const owner = nested.length > 1 && nested.at(-1) === "BackgroundTask" ? nested.at(-2) : nested.at(-1);
  const shortName = (owner ?? "").split(".").at(-1)?.replace(/^Begin(?=[A-Z])/, "") ?? "";
  const words = shortName.match(/[A-Z]+(?![a-z])|[A-Z]?[a-z0-9]+/g);
  if (!words) {
    return name || "Background task";
  }
  return words
    .map((word, index) => {
      const lower = word.toLowerCase();
      if (ACRONYMS.has(lower)) {
        return lower.toUpperCase();
      }
      return index === 0 ? lower.charAt(0).toUpperCase() + lower.slice(1) : lower;
    })
    .join(" ");
}

export function TaskStatusBadge({ status }: { status: BackgroundTaskStatusName }) {
  const label = TASK_STATUSES.find((item) => item.status === status)?.label ?? status;
  return <Badge variant={STATUS_VARIANT[status]}>{label}</Badge>;
}

export function TaskProgress({ task, className }: { task: BackgroundTaskDetail; className?: string }) {
  const percent = Math.max(0, Math.min(100, task.percentComplete));
  return (
    <div className={cn("flex items-center gap-2", className)}>
      <div
        className="h-1.5 w-24 overflow-hidden rounded-full bg-muted"
        role="progressbar"
        aria-label={`${taskLabel(task.name)} progress`}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
      >
        <div
          className={cn(
            "h-full rounded-full transition-[width] duration-500",
            task.status === "error" ? "bg-destructive" : task.status === "complete" ? "bg-green-500" : "bg-primary",
          )}
          style={{ width: `${percent}%` }}
        />
      </div>
      <span className="w-9 text-right text-xs tabular-nums text-muted-foreground">{percent}%</span>
    </div>
  );
}

/** How long a task ran, or has been running so far. Empty when timestamps are missing. */
export function taskDuration(task: BackgroundTaskDetail, now = Date.now()): string {
  const started = Date.parse(task.creationTime);
  if (Number.isNaN(started)) {
    return "";
  }
  const finished = Date.parse(task.completionTime);
  const end = isTaskRunning(task) || Number.isNaN(finished) ? now : finished;
  return formatDuration(end - started);
}

export function formatDuration(milliseconds: number): string {
  const seconds = Math.max(0, Math.round(milliseconds / 1000));
  if (seconds < 1) {
    return "under 1s";
  }
  const units: [string, number][] = [
    ["d", 86_400],
    ["h", 3_600],
    ["m", 60],
    ["s", 1],
  ];
  const parts: string[] = [];
  let remaining = seconds;
  for (const [suffix, size] of units) {
    const value = Math.floor(remaining / size);
    if (value > 0 || parts.length > 0) {
      parts.push(`${value}${suffix}`);
    }
    remaining -= value * size;
    if (parts.length === 2) {
      break;
    }
  }
  return parts.filter((part) => !part.startsWith("0")).join(" ");
}

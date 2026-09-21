import { adminApi, parseTaskMediaItem } from "@raytha/api";
import type { BackgroundTaskDetail, TaskMediaItem } from "@raytha/api";
import { Badge } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";

function isRunning(status: BackgroundTaskDetail["status"]): boolean {
  return status === "enqueued" || status === "processing";
}

function mediaFromTask(task: BackgroundTaskDetail): TaskMediaItem | null {
  return parseTaskMediaItem(task.statusInfo);
}

export function BackgroundTaskStatus({ taskId }: { taskId: string }) {
  const query = useQuery({
    queryKey: ["background-task", taskId],
    queryFn: () => adminApi.backgroundTasks.get(taskId),
    refetchInterval: (query) => {
      const task = query.state.data;
      return task && isRunning(task.status) ? 2000 : false;
    },
  });

  const task = query.data;
  const file = task ? mediaFromTask(task) : null;

  return (
    <div className="rounded-lg border border-border bg-card px-4 py-3 text-sm shadow-card">
      <p className="font-medium">
        Background task{" "}
        <span className="font-mono text-xs">{taskId}</span>
      </p>
      <p className="mt-1 text-muted-foreground">
        <Link to="/background-tasks" className="text-primary hover:underline">
          Open background tasks
        </Link>
      </p>
      {query.isError ? (
        <p className="mt-2 text-destructive">Could not load task status. Use the link above to check later.</p>
      ) : null}
      {task ? (
        <div className="mt-2 space-y-1">
          <p>
            <TaskStatusBadge task={task} /> {task.percentComplete}%
            {task.name ? ` · ${task.name}` : ""}
          </p>
          {task.statusInfo && !file ? <p className="text-muted-foreground">{task.statusInfo}</p> : null}
          {task.errorMessage ? <p className="text-destructive">{task.errorMessage}</p> : null}
          {file ? (
            <p>
              <a href={file.downloadUrl} className="text-primary hover:underline" target="_blank" rel="noreferrer">
                Download {file.fileName || "file"}
              </a>
            </p>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

function TaskStatusBadge({ task }: { task: BackgroundTaskDetail }) {
  if (task.status === "complete") {
    return <Badge variant="success">{task.statusLabel}</Badge>;
  }
  if (task.status === "error") {
    return <Badge variant="destructive">{task.statusLabel}</Badge>;
  }
  if (task.status === "processing") {
    return <Badge variant="info">{task.statusLabel}</Badge>;
  }
  return <Badge variant="secondary">{task.statusLabel}</Badge>;
}

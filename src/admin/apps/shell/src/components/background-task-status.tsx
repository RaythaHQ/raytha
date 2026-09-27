import { adminApi, hasPermission, parseTaskMediaItem, platformPermissions } from "@raytha/api";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { isTaskRunning, TaskProgress, taskLabel, TaskStatusBadge } from "./background-task-view";

export function BackgroundTaskStatus({ taskId }: { taskId: string }) {
  const query = useQuery({
    queryKey: ["background-task", taskId],
    queryFn: () => adminApi.backgroundTasks.get(taskId),
    refetchInterval: (query) => {
      const task = query.state.data;
      return task && isTaskRunning(task) ? 2000 : false;
    },
  });

  const task = query.data;
  const file = task ? parseTaskMediaItem(task.statusInfo) : null;
  const canOpenTaskList = hasPermission(platformPermissions.systemSettings);

  return (
    <div className="rounded-lg border border-border bg-card px-4 py-3 text-sm shadow-card">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="font-medium">{task ? taskLabel(task.name) : "Background task"}</p>
        {canOpenTaskList ? (
          <Link to="/background-tasks" className="text-xs text-muted-foreground hover:text-foreground">
            All background tasks
          </Link>
        ) : null}
      </div>
      {query.isError ? <p className="mt-2 text-destructive">Could not load task status.</p> : null}
      {task ? (
        <div className="mt-2 space-y-1.5">
          <div className="flex items-center gap-3">
            <TaskStatusBadge status={task.status} />
            <TaskProgress task={task} />
          </div>
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

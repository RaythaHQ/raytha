import { ApiError, formatError } from "@raytha/api";
import { LockKeyhole } from "lucide-react";
import type { ReactNode } from "react";
import { EmptyState } from "./empty-state";
import { Skeleton } from "./skeleton";

/** Structural subset of TanStack Query's UseQueryResult. */
export interface QueryLike<T> {
  isPending: boolean;
  isError: boolean;
  error: unknown;
  data: T | undefined;
}

/**
 * Standard pending/error boundary for query-driven pages: skeletons while
 * loading, a "no access" empty state on 403, a formatted problem message on
 * other errors, and the children once data is available.
 */
export function QueryGate<T>({
  query,
  children,
}: {
  query: QueryLike<T>;
  children: (data: T) => ReactNode;
}) {
  if (query.isPending) {
    return (
      <div className="overflow-hidden rounded-xl border border-border bg-card shadow-card" aria-busy="true">
        <div className="border-b border-border px-4 py-3">
          <Skeleton className="h-8 w-64 max-w-full" />
        </div>
        <div className="space-y-3 p-4">
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-4 w-3/5" />
        </div>
      </div>
    );
  }

  if (query.isError) {
    const status = query.error instanceof ApiError ? query.error.status : undefined;
    if (status === 403) {
      return (
        <EmptyState
          icon={LockKeyhole}
          title="No access"
          hint="You don't have permission to view this page. Ask an administrator if you think that's wrong."
        />
      );
    }

    return (
      <p className="rounded-xl border border-destructive-border bg-destructive-soft px-4 py-3 text-sm text-destructive-soft-foreground" role="alert">
        {formatError(query.error)}
      </p>
    );
  }

  return <>{children(query.data as T)}</>;
}

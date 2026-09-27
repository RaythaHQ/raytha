import { Search } from "lucide-react";
import type { HTMLAttributes, InputHTMLAttributes, ReactNode } from "react";
import { cn } from "./cn";
import { Input } from "./input";

/**
 * Card that wraps a list toolbar + table (or empty state). Search and the
 * primary action live in the toolbar so they sit on the list, not as a
 * floating row between page chrome and the table.
 */
export function ListPanel({
  toolbar,
  className,
  children,
  ...props
}: HTMLAttributes<HTMLDivElement> & { toolbar?: ReactNode }) {
  return (
    <div
      className={cn(
        "overflow-hidden rounded-xl border border-border bg-card shadow-card",
        "[&>[data-slot=empty-state]]:rounded-none [&>[data-slot=empty-state]]:border-0",
        className,
      )}
      {...props}
    >
      {toolbar && (
        <div className="flex flex-wrap items-center gap-2 border-b border-border px-3 py-2.5">{toolbar}</div>
      )}
      {children}
    </div>
  );
}

/** Announces list result counts after sort, filter, or page changes. */
export function ListStatus({
  total,
  page,
  noun = "results",
}: {
  total: number;
  page: number;
  noun?: string;
}) {
  return (
    <p role="status" className="sr-only">
      {`${total} ${noun}, page ${page}`}
    </p>
  );
}

/** Icon search field sized for a ListPanel toolbar. */
export function ListSearch({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div className="relative min-w-0 max-w-xs flex-1">
      <Search
        className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <Input className={cn("h-8 pl-8 shadow-none", className)} {...props} />
    </div>
  );
}

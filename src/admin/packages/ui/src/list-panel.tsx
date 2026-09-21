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
      className={cn("overflow-hidden rounded-xl border border-border bg-card shadow-card", className)}
      {...props}
    >
      {toolbar && (
        <div className="flex flex-wrap items-center gap-3 border-b border-border px-4 py-3">{toolbar}</div>
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
    <div className="relative min-w-48 max-w-sm flex-1">
      <Search
        className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <Input className={cn("pl-9", className)} {...props} />
    </div>
  );
}

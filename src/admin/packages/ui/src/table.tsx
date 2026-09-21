import type { HTMLAttributes, TdHTMLAttributes, ThHTMLAttributes } from "react";
import { cn } from "./cn";

export function Table({
  className,
  flush = false,
  "aria-label": ariaLabel,
  ...props
}: HTMLAttributes<HTMLTableElement> & {
  /** Skip the outer card when the table is already inside a ListPanel. */
  flush?: boolean;
}) {
  return (
    <div
      className={cn(
        "relative w-full min-w-0 overflow-auto",
        !flush && "rounded-xl border border-border bg-card shadow-card",
      )}
      // Scrollable overflow region must be reachable from the keyboard (2.1.1).
      // eslint-disable-next-line jsx-a11y/no-noninteractive-tabindex
      tabIndex={0}
      role="region"
      aria-label={ariaLabel ? `${ariaLabel} (scrollable)` : "Scrollable table"}
    >
      <table className={cn("w-full caption-bottom text-sm", className)} aria-label={ariaLabel} {...props} />
    </div>
  );
}

export function TableHeader({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <thead className={cn("[&_tr]:border-b bg-brand-50/60 sticky top-0", className)} {...props} />;
}

export function TableBody({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <tbody className={cn("[&_tr:last-child]:border-0", className)} {...props} />;
}

export function TableRow({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) {
  return <tr className={cn("border-b border-border transition-colors hover:bg-brand-50/50", className)} {...props} />;
}

export function TableHead({ className, ...props }: ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th
      className={cn("h-10 px-4 text-left align-middle text-xs font-semibold uppercase tracking-wide text-muted-foreground", className)}
      {...props}
    />
  );
}

export function TableCell({ className, ...props }: TdHTMLAttributes<HTMLTableCellElement>) {
  return <td className={cn("px-4 py-3 align-middle", className)} {...props} />;
}

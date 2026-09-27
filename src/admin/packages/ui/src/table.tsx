import type { ComponentProps, HTMLAttributes, TdHTMLAttributes, ThHTMLAttributes } from "react";
import { cn } from "./cn";

const SHORT_CELL_MAX = 32;

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
      <table
        className={cn(
          "w-full caption-bottom text-[13px] tabular-nums",
          "[&_a.text-primary]:font-medium [&_a.text-primary]:text-foreground [&_a.text-primary]:decoration-foreground/30 [&_a.text-primary]:underline-offset-4 [&_a.text-primary:hover]:text-brand-700 [&_a.text-primary:hover]:decoration-brand-300",
          className,
        )}
        aria-label={ariaLabel}
        {...props}
      />
    </div>
  );
}

export function TableHeader({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <thead className={cn("sticky top-0 z-[1] bg-[#fafafb] [&_tr]:border-b [&_tr:hover]:bg-transparent", className)} {...props} />;
}

export function TableBody({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <tbody className={cn("[&_tr:last-child]:border-0", className)} {...props} />;
}

export function TableRow({ className, ...props }: ComponentProps<"tr">) {
  return <tr className={cn("border-b border-border transition-colors hover:bg-[#f8f8fa]", className)} {...props} />;
}

export function TableHead({ className, ...props }: ThHTMLAttributes<HTMLTableCellElement>) {
  return (
    <th
      className={cn("h-9 whitespace-nowrap px-4 text-left align-middle text-xs font-medium text-muted-foreground", className)}
      {...props}
    />
  );
}

export function TableCell({
  className,
  nowrap,
  children,
  ...props
}: TdHTMLAttributes<HTMLTableCellElement> & {
  /** Keep the cell on one line. Defaults to true for short text and numbers such as dates, sizes, and types. */
  nowrap?: boolean;
}) {
  const scalar = typeof children === "string" || typeof children === "number" ? String(children) : null;
  const keepOnOneLine = nowrap ?? (scalar !== null && scalar.length <= SHORT_CELL_MAX);
  return (
    <td
      className={cn(
        "px-4 py-2.5 align-middle",
        keepOnOneLine && "whitespace-nowrap",
        scalar === "—" && "text-muted-foreground/60",
        className,
      )}
      {...props}
    >
      {children}
    </td>
  );
}

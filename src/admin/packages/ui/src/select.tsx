import { ChevronDown } from "lucide-react";
import type { SelectHTMLAttributes } from "react";
import { cn } from "./cn";

/** Styled native select; matches the Input look, with a chevron affordance. */
export function Select({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    // `flex` (not inline-flex) so the control is block-level and sits under its label.
    <span className={cn("relative flex w-full items-center", className)}>
      <select
        className="flex h-10 w-full appearance-none rounded-lg border border-input bg-card pl-3 pr-9 py-1 text-sm shadow-card transition-colors focus-visible:border-brand-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-100 aria-invalid:border-destructive aria-invalid:ring-destructive/20 disabled:cursor-not-allowed disabled:opacity-50"
        {...props}
      >
        {children}
      </select>
      <ChevronDown
        className="pointer-events-none absolute top-1/2 right-3 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
    </span>
  );
}

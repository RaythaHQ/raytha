import { ChevronsUpDown } from "lucide-react";
import type { SelectHTMLAttributes } from "react";
import { cn } from "./cn";
import { fieldControl } from "./field-control";

/** Styled native select; matches the Input look, with a chevron affordance. */
export function Select({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    // `flex` (not inline-flex) so the control is block-level and sits under its label.
    <span className={cn("relative flex w-full items-center", className)}>
      <select className={cn("flex h-9 appearance-none py-1 pl-3 pr-9", fieldControl)} {...props}>
        {children}
      </select>
      <ChevronsUpDown
        className="pointer-events-none absolute top-1/2 right-2.5 size-3.5 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
    </span>
  );
}

import type { LabelHTMLAttributes } from "react";
import { cn } from "./cn";

export function Label({ className, ...props }: LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    // Pass-through primitive: consumers supply htmlFor (or nest the control),
    // which the static rule cannot see through the spread.
    // eslint-disable-next-line jsx-a11y/label-has-associated-control
    <label
      className={cn("text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70", className)}
      {...props}
    />
  );
}

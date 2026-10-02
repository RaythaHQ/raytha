import type { HTMLAttributes } from "react";
import { cn } from "./cn";

export function ScrollArea({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("overflow-auto", className)}
      // Scrollable overflow region must be reachable from the keyboard (2.1.1).
      // eslint-disable-next-line jsx-a11y/no-noninteractive-tabindex
      tabIndex={0}
      {...props}
    />
  );
}

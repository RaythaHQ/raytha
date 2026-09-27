import { ChevronRight } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "./cn";

export function Breadcrumb({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <nav aria-label="Breadcrumb" className={className}>
      <ol className="flex items-center gap-1 text-[13px]">{children}</ol>
    </nav>
  );
}

export function BreadcrumbItem({
  children,
  current,
  className,
}: {
  children: ReactNode;
  current?: boolean;
  className?: string;
}) {
  return (
    <li className={cn("flex min-w-0 items-center gap-1", className)}>
      <span
        aria-current={current ? "page" : undefined}
        className={cn("truncate", current ? "font-medium text-foreground" : "text-muted-foreground [&_a:hover]:text-foreground")}
      >
        {children}
      </span>
    </li>
  );
}

export function BreadcrumbSeparator() {
  return (
    <li aria-hidden className="text-muted-foreground/50">
      <ChevronRight className="size-3.5" />
    </li>
  );
}

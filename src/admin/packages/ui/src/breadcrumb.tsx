import { ChevronRight } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "./cn";

export function Breadcrumb({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <nav aria-label="Breadcrumb" className={className}>
      <ol className="flex items-center gap-1.5 text-sm">{children}</ol>
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
    <li className={cn("flex items-center gap-1.5", className)}>
      <span className={cn(current ? "font-semibold text-foreground" : "text-muted-foreground")}>{children}</span>
    </li>
  );
}

export function BreadcrumbSeparator() {
  return (
    <li aria-hidden className="text-muted-foreground/60">
      <ChevronRight className="size-3.5" />
    </li>
  );
}

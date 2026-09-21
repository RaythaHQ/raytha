import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "./cn";

/** Friendly placeholder for empty lists, 403s, and error states. */
export function EmptyState({
  icon: Icon,
  title,
  hint,
  action,
  className,
}: {
  icon?: LucideIcon;
  title: string;
  hint?: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-border px-6 py-12 text-center", className)}>
      {Icon && (
        <span className="mb-1 flex size-11 items-center justify-center rounded-xl bg-brand-50 text-brand-500">
          <Icon className="size-5" aria-hidden />
        </span>
      )}
      <p className="font-display font-semibold text-foreground">{title}</p>
      {hint && <p className="max-w-sm text-sm text-muted-foreground">{hint}</p>}
      {action && <div className="mt-3">{action}</div>}
    </div>
  );
}

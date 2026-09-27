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
    <div
      data-slot="empty-state"
      className={cn(
        "flex flex-col items-center justify-center rounded-xl border border-dashed border-border-strong bg-[radial-gradient(ellipse_at_top,var(--brand-50),transparent_70%)] px-6 py-14 text-center",
        className,
      )}
    >
      {Icon && (
        <span className="relative mb-4 flex size-12 items-center justify-center rounded-xl border border-border bg-card text-brand-600 shadow-card">
          <span className="absolute inset-1 rounded-lg bg-gradient-to-b from-brand-50 to-transparent" aria-hidden />
          <Icon className="relative size-5" aria-hidden />
        </span>
      )}
      <p className="font-display text-[15px] font-semibold text-foreground">{title}</p>
      {hint && <p className="mt-1 max-w-sm text-[13px] leading-5 text-muted-foreground">{hint}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}

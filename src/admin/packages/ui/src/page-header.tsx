import type { ReactNode } from "react";
import { cn } from "./cn";

const backSlot = cn(
  "flex",
  "[&_a]:inline-flex [&_a]:h-auto [&_a]:items-center [&_a]:gap-1 [&_a]:rounded-md [&_a]:px-0 [&_a]:py-0.5 [&_a]:text-[13px] [&_a]:font-medium [&_a]:text-muted-foreground [&_a]:shadow-none [&_a_svg]:size-3.5",
  "[&_a:hover]:bg-transparent [&_a:hover]:text-foreground [&_a:active]:bg-transparent",
);

const metaSlot = cn(
  "flex flex-wrap items-center gap-x-3 gap-y-2 text-[13px] text-muted-foreground",
  "[&_code]:rounded-md [&_code]:border [&_code]:border-border [&_code]:bg-muted/60 [&_code]:px-1.5 [&_code]:py-0.5 [&_code]:text-xs [&_code]:text-foreground",
);

// Pages hand in their own tablist markup (kit Tabs or router links with role="tab");
// the slot restyles any of them into one underline bar that closes the header.
// Selection is read from aria-selected only: TanStack Router stamps aria-current="page"
// on every prefix-matching link, so it cannot tell sibling tabs apart.
// The baseline is an inset shadow, not a border: tabs paint their underline over it without
// overflowing the strip, so the scroll container never gains a vertical scrollbar.
const tabsSlot = cn(
  "flex overflow-x-auto overflow-y-hidden shadow-[inset_0_-1px_0_var(--color-border)]",
  "[&_[role=tablist]]:flex [&_[role=tablist]]:h-auto [&_[role=tablist]]:gap-6 [&_[role=tablist]]:rounded-none [&_[role=tablist]]:border-0 [&_[role=tablist]]:bg-transparent [&_[role=tablist]]:p-0",
  "[&_[role=tab]]:h-auto [&_[role=tab]]:ring-0 [&_[role=tab]]:whitespace-nowrap [&_[role=tab]]:rounded-none [&_[role=tab]]:border-b-2 [&_[role=tab]]:border-transparent [&_[role=tab]]:bg-transparent [&_[role=tab]]:px-0.5 [&_[role=tab]]:pt-1 [&_[role=tab]]:pb-2.5 [&_[role=tab]]:text-sm [&_[role=tab]]:font-medium [&_[role=tab]]:text-muted-foreground [&_[role=tab]]:shadow-none",
  "[&_[role=tab]:hover]:border-border-strong [&_[role=tab]:hover]:text-foreground",
  "[&_[role=tab][aria-selected=true]]:border-foreground [&_[role=tab][aria-selected=true]]:text-foreground",
);

/**
 * Standard page heading. `back` sits above the title (a ListBackLink or breadcrumb),
 * `meta` is a row of status chips and facts under the description, and `tabs` is the
 * section tab bar that closes the header.
 */
export function PageHeader({
  title,
  description,
  actions,
  back,
  meta,
  tabs,
  className,
}: {
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  back?: ReactNode;
  meta?: ReactNode;
  tabs?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex flex-col gap-3", className)}>
      {back && <div className={backSlot}>{back}</div>}
      <div className="flex flex-wrap items-start justify-between gap-x-6 gap-y-3">
        <div className="min-w-0 flex-1 space-y-1">
          <h1 className="break-words font-display text-[22px] font-semibold leading-8 tracking-[-0.02em] text-foreground">
            {title}
          </h1>
          {description && <p className="max-w-3xl text-sm leading-6 text-muted-foreground">{description}</p>}
        </div>
        {actions && <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div>}
      </div>
      {meta && <div className={metaSlot}>{meta}</div>}
      {tabs && <div className={cn(tabsSlot, "mt-2")}>{tabs}</div>}
    </div>
  );
}

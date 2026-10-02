import type { ReactNode } from "react";
import { useId, useState } from "react";
import { Button } from "./button";
import { ConfirmDialog } from "./confirm-dialog";

/** Destructive action block for entity edit pages. Keeps delete off list columns. */
export function DangerZone({
  title = "Danger zone",
  description,
  actionLabel,
  confirmTitle,
  confirmBody,
  confirmLabel,
  onConfirm,
  pending,
}: {
  title?: string;
  description: ReactNode;
  actionLabel: string;
  confirmTitle: string;
  confirmBody?: ReactNode;
  confirmLabel?: string;
  onConfirm: () => void;
  pending?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const headingId = useId();

  return (
    <>
      <section
        aria-labelledby={headingId}
        className="overflow-hidden rounded-xl border border-destructive-border bg-card shadow-card"
      >
        <h2
          id={headingId}
          className="border-b border-destructive-border bg-destructive-soft/60 px-6 py-2.5 font-display text-[13px] font-semibold text-destructive-soft-foreground"
        >
          {title}
        </h2>
        <div className="flex flex-wrap items-center justify-between gap-x-6 gap-y-3 px-6 py-4">
          <div className="min-w-0 max-w-2xl space-y-0.5">
            <p className="text-sm font-medium text-foreground">{actionLabel}</p>
            <div className="text-[13px] leading-5 text-muted-foreground">{description}</div>
          </div>
          <Button
            type="button"
            variant="outline"
            className="border-destructive-border text-destructive-soft-foreground hover:border-destructive hover:bg-destructive hover:text-destructive-foreground active:bg-destructive/90"
            onClick={() => setOpen(true)}
            disabled={pending}
          >
            {actionLabel}
          </Button>
        </div>
      </section>
      <ConfirmDialog
        open={open}
        onOpenChange={setOpen}
        title={confirmTitle}
        body={confirmBody}
        confirmLabel={confirmLabel}
        onConfirm={onConfirm}
        pending={pending}
      />
    </>
  );
}

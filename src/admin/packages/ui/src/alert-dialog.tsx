import { TriangleAlert } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "./button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "./dialog";

export interface AlertDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  body?: ReactNode;
  cancelLabel?: string;
  confirmLabel?: string;
  confirmVariant?: "default" | "destructive";
  onConfirm: () => void;
  pending?: boolean;
}

/** Confirmation modal with explicit cancel / confirm actions. */
export function AlertDialog({
  open,
  onOpenChange,
  title,
  body,
  cancelLabel = "Cancel",
  confirmLabel = "Continue",
  confirmVariant = "default",
  onConfirm,
  pending,
}: AlertDialogProps) {
  const destructive = confirmVariant === "destructive";
  return (
    <Dialog open={open} onOpenChange={onOpenChange} widthClassName="max-w-md">
      <DialogHeader className="flex items-start gap-3 border-0 pt-5 pb-0">
        {destructive && (
          <span className="flex size-9 shrink-0 items-center justify-center rounded-full border border-destructive-border bg-destructive-soft text-destructive">
            <TriangleAlert className="size-4" aria-hidden />
          </span>
        )}
        <div className="min-w-0 pt-1.5">
          <DialogTitle>{title}</DialogTitle>
        </div>
      </DialogHeader>
      <DialogContent className={destructive ? "pt-2 pb-5 pl-18" : "pt-2 pb-5"}>
        {body && <p className="text-sm leading-6 text-muted-foreground">{body}</p>}
      </DialogContent>
      <DialogFooter>
        <Button variant="outline" onClick={() => onOpenChange(false)}>
          {cancelLabel}
        </Button>
        <Button variant={confirmVariant} loading={pending} onClick={onConfirm}>
          {confirmLabel}
        </Button>
      </DialogFooter>
    </Dialog>
  );
}

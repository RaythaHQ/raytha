import type { ReactNode } from "react";
import { AlertDialog } from "./alert-dialog";

/** Accessible replacement for window.confirm on destructive actions. */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  body,
  confirmLabel = "Delete",
  onConfirm,
  pending,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  body?: ReactNode;
  confirmLabel?: string;
  onConfirm: () => void;
  pending?: boolean;
}) {
  return (
    <AlertDialog
      open={open}
      onOpenChange={onOpenChange}
      title={title}
      body={body}
      confirmLabel={confirmLabel}
      confirmVariant="destructive"
      onConfirm={onConfirm}
      pending={pending}
    />
  );
}

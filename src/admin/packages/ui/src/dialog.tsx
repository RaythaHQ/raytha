import { X } from "lucide-react";
import {
  createContext,
  useContext,
  useEffect,
  useId,
  useRef,
  type HTMLAttributes,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { cn } from "./cn";

export interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  children: ReactNode;
  /** Tailwind max-width class for the panel, e.g. "max-w-3xl". */
  widthClassName?: string;
}

/** Links the panel's aria-labelledby to whatever DialogTitle renders inside it. */
const DialogTitleIdContext = createContext<string | undefined>(undefined);

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [contenteditable]:not([contenteditable="false"]), [tabindex]:not([tabindex="-1"])';

/**
 * Minimal controlled modal: overlay + centered panel. Escape and overlay-click
 * close; focus moves into the panel on open, is trapped while open, and is
 * restored on close; body scroll is locked.
 */
export function Dialog({ open, onOpenChange, children, widthClassName = "max-w-lg" }: DialogProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();

  const onOpenChangeRef = useRef(onOpenChange);
  useEffect(() => {
    onOpenChangeRef.current = onOpenChange;
  }, [onOpenChange]);

  useEffect(() => {
    if (!open) return;

    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const panel = panelRef.current;
    const focusables = () =>
      panel
        ? Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter(
            (el) => !el.hasAttribute("disabled") && el.getAttribute("aria-disabled") !== "true",
          )
        : [];

    const restoreFocusIfDisabled = () => {
      const active = document.activeElement;
      if (!(active instanceof HTMLElement) || !panel?.contains(active)) {
        return;
      }
      if (!active.hasAttribute("disabled") && active.getAttribute("aria-disabled") !== "true") {
        return;
      }
      (focusables()[0] ?? panel)?.focus();
    };

    const initial = focusables().find((el) => el.getAttribute("aria-label") !== "Close") ?? panel;
    initial?.focus();

    const observer = panel
      ? new MutationObserver(restoreFocusIfDisabled)
      : null;
    observer?.observe(panel as Node, { attributes: true, subtree: true, attributeFilter: ["disabled"] });

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onOpenChangeRef.current(false);
        return;
      }

      if (event.key !== "Tab" || !panel) {
        return;
      }

      const items = focusables();
      if (items.length === 0) {
        event.preventDefault();
        return;
      }

      const first = items[0];
      const last = items[items.length - 1];
      const active = document.activeElement;
      if (event.shiftKey && (active === first || active === panel)) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    return () => {
      observer?.disconnect();
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = previousOverflow;
      previouslyFocused?.focus();
    };
  }, [open]);

  if (!open) return null;

  return createPortal(
    <DialogTitleIdContext.Provider value={titleId}>
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4" data-raytha-overlay="dialog">
        <div className="fixed inset-0 bg-black/50" onClick={() => onOpenChange(false)} aria-hidden />
        <div
          ref={panelRef}
          role="dialog"
          aria-modal="true"
          aria-labelledby={titleId}
          tabIndex={-1}
          className={cn(
            "relative z-50 flex max-h-[85vh] w-full flex-col overflow-hidden rounded-lg border bg-background shadow-lg",
            widthClassName,
          )}
        >
          <button
            type="button"
            aria-label="Close"
            onClick={() => onOpenChange(false)}
            className="absolute right-3 top-3 rounded-sm p-1 text-muted-foreground transition-colors hover:text-foreground"
          >
            <X className="size-4" />
          </button>
          {children}
        </div>
      </div>
    </DialogTitleIdContext.Provider>,
    document.body,
  );
}

export function DialogHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("border-b px-6 py-4", className)} {...props} />;
}

export function DialogTitle({ className, ...props }: HTMLAttributes<HTMLHeadingElement>) {
  const titleId = useContext(DialogTitleIdContext);
  return (
    // Content arrives via the children in ...props; the rule cannot see through the spread.
    // eslint-disable-next-line jsx-a11y/heading-has-content
    <h2 id={titleId} className={cn("text-lg font-semibold leading-none tracking-tight", className)} {...props} />
  );
}

export function DialogContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("flex-1 overflow-y-auto px-6 py-4", className)} {...props} />;
}

export function DialogFooter({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("flex justify-end gap-2 border-t px-6 py-4", className)} {...props} />;
}

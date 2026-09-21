import { cloneElement, createContext, useContext, useEffect, useId, useRef, useState, type KeyboardEvent, type ReactElement, type ReactNode } from "react";
import { cn } from "./cn";

interface DropdownContextValue {
  open: boolean;
  setOpen: (open: boolean) => void;
  triggerId: string;
  menuId: string;
}

const DropdownContext = createContext<DropdownContextValue | null>(null);

function useDropdown(): DropdownContextValue {
  const ctx = useContext(DropdownContext);
  if (!ctx) {
    throw new Error("DropdownMenu parts must be used inside <DropdownMenu>");
  }
  return ctx;
}

/**
 * Hand-rolled dropdown with menu semantics: aria-haspopup/expanded on the
 * trigger, Escape and outside-click to close, arrow-key navigation between
 * items, and focus return to the trigger on close.
 */
export function DropdownMenu({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const triggerId = useId();
  const menuId = useId();
  return (
    <DropdownContext.Provider value={{ open, setOpen, triggerId, menuId }}>
      <div className="relative inline-block">{children}</div>
    </DropdownContext.Provider>
  );
}

export function DropdownMenuTrigger({ children }: { children: ReactElement }) {
  const { open, setOpen, triggerId, menuId } = useDropdown();
  return cloneElement(children, {
    id: triggerId,
    onClick: () => setOpen(!open),
    onKeyDown: (e: KeyboardEvent) => {
      if (e.key === "ArrowDown") {
        e.preventDefault();
        setOpen(true);
      }
    },
    "aria-haspopup": "menu",
    "aria-expanded": open,
    ...(open ? { "aria-controls": menuId } : {}),
  } as Partial<unknown>);
}

export function DropdownMenuContent({
  children,
  className,
  align = "end",
}: {
  children: ReactNode;
  className?: string;
  align?: "start" | "end";
}) {
  const { open, setOpen, triggerId, menuId } = useDropdown();
  const ref = useRef<HTMLDivElement>(null);
  const wasOpen = useRef(false);

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: MouseEvent) => {
      if (ref.current && !ref.current.parentElement?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, [open, setOpen]);

  useEffect(() => {
    if (open) {
      ref.current?.querySelector<HTMLElement>('[role="menuitem"]')?.focus();
    } else if (wasOpen.current) {
      document.getElementById(triggerId)?.focus();
    }
    wasOpen.current = open;
  }, [open, triggerId]);

  if (!open) {
    return null;
  }

  const onKeyDown = (event: KeyboardEvent) => {
    const items = Array.from(ref.current?.querySelectorAll<HTMLElement>('[role="menuitem"]:not([data-disabled])') ?? []);
    const index = items.indexOf(document.activeElement as HTMLElement);

    if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      items[(index + 1) % items.length]?.focus();
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      items[(index - 1 + items.length) % items.length]?.focus();
    } else if (event.key === "Tab") {
      setOpen(false);
    }
  };

  return (
    <div
      ref={ref}
      id={menuId}
      role="menu"
      tabIndex={-1}
      onKeyDown={onKeyDown}
      className={cn(
        "absolute z-50 mt-2 min-w-52 overflow-hidden rounded-xl border border-border bg-card p-1.5 shadow-pop",
        align === "end" ? "right-0" : "left-0",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function DropdownMenuItem({
  children,
  onSelect,
  className,
}: {
  children: ReactNode;
  onSelect?: () => void;
  className?: string;
}) {
  const { setOpen } = useDropdown();
  return (
    <button
      type="button"
      role="menuitem"
      tabIndex={-1}
      onClick={() => {
        setOpen(false);
        onSelect?.();
      }}
      className={cn(
        "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-foreground outline-none transition-colors hover:bg-accent focus-visible:bg-accent [&_svg]:size-4 [&_svg]:text-muted-foreground",
        className,
      )}
    >
      {children}
    </button>
  );
}

export function DropdownMenuLabel({ children }: { children: ReactNode }) {
  return <p className="px-3 py-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">{children}</p>;
}

export function DropdownMenuSeparator() {
  return <div role="separator" className="mx-1 my-1 border-t border-border" />;
}

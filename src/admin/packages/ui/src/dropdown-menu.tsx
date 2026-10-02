import { cloneElement, createContext, useContext, useEffect, useId, useLayoutEffect, useRef, useState, type CSSProperties, type KeyboardEvent, type ReactElement, type ReactNode, type RefObject } from "react";
import { createPortal } from "react-dom";
import { cn } from "./cn";

interface DropdownContextValue {
  open: boolean;
  setOpen: (open: boolean) => void;
  triggerId: string;
  menuId: string;
  rootRef: RefObject<HTMLDivElement | null>;
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
  const rootRef = useRef<HTMLDivElement>(null);
  return (
    <DropdownContext.Provider value={{ open, setOpen, triggerId, menuId, rootRef }}>
      <div ref={rootRef} className="relative inline-block">{children}</div>
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
  const { open, setOpen, triggerId, menuId, rootRef } = useDropdown();
  const ref = useRef<HTMLDivElement>(null);
  const wasOpen = useRef(false);
  const [menuStyle, setMenuStyle] = useState<CSSProperties>({ position: "fixed", top: 0, left: 0 });

  useLayoutEffect(() => {
    if (!open) {
      return;
    }
    const place = () => {
      const trigger = document.getElementById(triggerId);
      const menu = ref.current;
      if (!trigger || !menu) {
        return;
      }
      const triggerRect = trigger.getBoundingClientRect();
      const menuRect = menu.getBoundingClientRect();
      const gap = 8;
      let top = triggerRect.bottom + gap;
      if (top + menuRect.height > window.innerHeight - gap) {
        top = Math.max(gap, triggerRect.top - gap - menuRect.height);
      }
      let left = align === "end" ? triggerRect.right - menuRect.width : triggerRect.left;
      left = Math.min(Math.max(gap, left), Math.max(gap, window.innerWidth - menuRect.width - gap));
      setMenuStyle({ position: "fixed", top, left });
    };
    place();
    window.addEventListener("resize", place);
    window.addEventListener("scroll", place, true);
    return () => {
      window.removeEventListener("resize", place);
      window.removeEventListener("scroll", place, true);
    };
  }, [open, align, triggerId]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const onPointerDown = (event: MouseEvent) => {
      const target = event.target as Node;
      if (rootRef.current?.contains(target) || ref.current?.contains(target)) {
        return;
      }
      setOpen(false);
    };
    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, [open, setOpen, rootRef]);

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

  return createPortal(
    <div
      ref={ref}
      id={menuId}
      role="menu"
      tabIndex={-1}
      onKeyDown={onKeyDown}
      style={menuStyle}
      className={cn(
        "z-50 min-w-52 animate-pop overflow-hidden rounded-xl border border-border bg-card p-1 shadow-pop outline-none",
        className,
      )}
    >
      {children}
    </div>,
    document.body,
  );
}

export function DropdownMenuItem({
  children,
  onSelect,
  className,
  disabled,
}: {
  children: ReactNode;
  onSelect?: () => void;
  className?: string;
  disabled?: boolean;
}) {
  const { setOpen } = useDropdown();
  return (
    <button
      type="button"
      role="menuitem"
      tabIndex={-1}
      disabled={disabled}
      data-disabled={disabled ? "" : undefined}
      onClick={() => {
        if (disabled) {
          return;
        }
        setOpen(false);
        onSelect?.();
      }}
      className={cn(
        "flex h-8 w-full items-center gap-2 rounded-lg px-2.5 text-left text-[13px] text-foreground outline-none transition-colors hover:bg-accent focus-visible:bg-accent disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0 [&_svg]:text-muted-foreground",
        className,
      )}
    >
      {children}
    </button>
  );
}

export function DropdownMenuLabel({ children }: { children: ReactNode }) {
  return <p className="truncate px-2.5 pt-1.5 pb-1 text-xs font-medium text-muted-foreground">{children}</p>;
}

export function DropdownMenuSeparator() {
  return <div role="separator" className="-mx-1 my-1 border-t border-border" />;
}

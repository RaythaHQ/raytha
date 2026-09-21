import { Search } from "lucide-react";
import type { ReactNode } from "react";
import { useEffect, useId, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { cn } from "./cn";

export interface CommandItem {
  id: string;
  label: string;
  hint?: string;
  icon?: ReactNode;
  group?: string;
  keywords?: string[];
  onSelect: () => void;
}

const FOCUSABLE_SELECTOR =
  'a[href]:not([tabindex="-1"]), button:not([disabled]):not([tabindex="-1"]), textarea:not([disabled]):not([tabindex="-1"]), input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), [tabindex]:not([tabindex="-1"])';

/**
 * Cmd/Ctrl+K command palette: fuzzy-filtered actions with full keyboard
 * navigation. Mounted once by the app shell. The dialog remounts on each open
 * (keyed by openCount) so query and selection reset without effects.
 */
export function CommandPalette({
  open,
  onOpenChange,
  items,
  placeholder = "Type a command or search…",
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  items: CommandItem[];
  placeholder?: string;
}) {
  const [openCount, setOpenCount] = useState(0);
  const [wasOpen, setWasOpen] = useState(false);
  if (open !== wasOpen) {
    setWasOpen(open);
    if (open) {
      setOpenCount((c) => c + 1);
    }
  }

  if (!open) {
    return null;
  }

  return (
    <PaletteDialog
      key={openCount}
      items={items}
      placeholder={placeholder}
      onOpenChange={onOpenChange}
    />
  );
}

function PaletteDialog({
  items,
  placeholder,
  onOpenChange,
}: {
  items: CommandItem[];
  placeholder: string;
  onOpenChange: (open: boolean) => void;
}) {
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) {
      return items;
    }
    return items.filter((item) =>
      [item.label, item.hint ?? "", item.group ?? "", ...(item.keywords ?? [])]
        .join(" ")
        .toLowerCase()
        .includes(q),
    );
  }, [items, query]);

  const activeItem = filtered[activeIndex];
  const activeId = activeItem ? `${listboxId}-opt-${activeItem.id}` : undefined;

  useEffect(() => {
    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    inputRef.current?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Tab" || !panelRef.current) {
        return;
      }
      const itemsInPanel = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
      if (itemsInPanel.length === 0) {
        event.preventDefault();
        return;
      }
      const first = itemsInPanel[0];
      const last = itemsInPanel[itemsInPanel.length - 1];
      const active = document.activeElement;
      if (event.shiftKey && (active === first || active === panelRef.current)) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = previousOverflow;
      previouslyFocused?.focus();
    };
  }, []);

  useEffect(() => {
    listRef.current
      ?.querySelector(`[data-index="${activeIndex}"]`)
      ?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  const handleQueryChange = (value: string) => {
    setQuery(value);
    setActiveIndex(0);
  };

  const select = (item: CommandItem) => {
    onOpenChange(false);
    item.onSelect();
  };

  const groups = new Map<string, CommandItem[]>();
  for (const item of filtered) {
    const group = item.group ?? "";
    groups.set(group, [...(groups.get(group) ?? []), item]);
  }

  let flatIndex = -1;

  return createPortal(
    // The overlay is a click-to-dismiss backdrop; the dialog itself is the
    // interactive element and handles all keyboard input.
    // eslint-disable-next-line jsx-a11y/no-static-element-interactions, jsx-a11y/click-events-have-key-events
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/50 p-4 pt-[15vh]"
      data-raytha-overlay="command"
      onClick={() => onOpenChange(false)}
    >
      {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-interactions */}
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
        tabIndex={-1}
        className="w-full max-w-lg overflow-hidden rounded-xl border border-border bg-card shadow-pop"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => {
          if (e.key === "Escape") {
            onOpenChange(false);
          } else if (e.key === "ArrowDown") {
            e.preventDefault();
            setActiveIndex((i) => Math.min(i + 1, filtered.length - 1));
          } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setActiveIndex((i) => Math.max(i - 1, 0));
          } else if (e.key === "Enter" && filtered[activeIndex]) {
            e.preventDefault();
            select(filtered[activeIndex]);
          }
        }}
      >
        <div className="flex items-center gap-2 border-b border-border px-4">
          <Search className="size-4 shrink-0 text-muted-foreground" aria-hidden />
          <input
            ref={inputRef}
            role="combobox"
            aria-expanded
            aria-controls={listboxId}
            aria-autocomplete="list"
            aria-activedescendant={activeId}
            value={query}
            onChange={(e) => handleQueryChange(e.target.value)}
            placeholder={placeholder}
            aria-label="Search commands"
            className="h-12 w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground focus-visible:ring-0"
          />
          <kbd className="rounded border border-border bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">esc</kbd>
        </div>
        <div ref={listRef} id={listboxId} role="listbox" aria-label="Commands" className="max-h-80 overflow-y-auto p-2">
          {filtered.length === 0 && (
            <p className="px-3 py-6 text-center text-sm text-muted-foreground">No results.</p>
          )}
          {[...groups.entries()].map(([group, groupItems]) => (
            <div key={group}>
              {group && (
                <p className="px-3 pb-1 pt-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  {group}
                </p>
              )}
              {groupItems.map((item) => {
                flatIndex += 1;
                const index = flatIndex;
                return (
                  <button
                    key={item.id}
                    id={`${listboxId}-opt-${item.id}`}
                    type="button"
                    role="option"
                    tabIndex={-1}
                    aria-selected={index === activeIndex}
                    data-index={index}
                    onClick={() => select(item)}
                    onMouseMove={() => setActiveIndex(index)}
                    className={cn(
                      "flex w-full cursor-pointer items-center gap-3 rounded-lg px-3 py-2 text-left text-sm",
                      index === activeIndex ? "bg-accent text-accent-foreground" : "text-foreground",
                    )}
                  >
                    {item.icon && <span className="text-muted-foreground [&_svg]:size-4">{item.icon}</span>}
                    <span className="flex-1">{item.label}</span>
                    {item.hint && <span className="text-xs text-muted-foreground">{item.hint}</span>}
                  </button>
                );
              })}
            </div>
          ))}
        </div>
      </div>
    </div>,
    document.body,
  );
}

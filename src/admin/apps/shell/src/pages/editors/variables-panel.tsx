import type { TemplateVariable, TemplateVariableGroup } from "@raytha/api";
import { cn, Input } from "@raytha/ui";
import { ChevronRight, Copy, Search, SquareCode } from "lucide-react";
import { useMemo, useRef, useState, type KeyboardEvent, type ReactNode } from "react";
import { copyText } from "./clipboard";
import { variableSnippet } from "./liquid-catalog";

export interface SearchShortcut {
  label: string;
  aria: string;
}

export function VariablesPanel({
  groups,
  onInsert,
  searchId,
  shortcut,
  onEscape,
}: {
  groups: TemplateVariableGroup[];
  onInsert: (text: string) => void;
  searchId: string;
  shortcut: SearchShortcut;
  onEscape: () => void;
}) {
  const [query, setQuery] = useState("");
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(() => new Set());
  const listRef = useRef<HTMLDivElement>(null);
  const terms = useMemo(() => query.trim().toLowerCase().split(/\s+/).filter(Boolean), [query]);
  const visible = useMemo(() => filterGroups(groups, terms), [groups, terms]);
  const total = groups.reduce((sum, group) => sum + group.variables.length, 0);
  const matches = visible.reduce((sum, group) => sum + group.variables.length, 0);

  const toggle = (category: string) =>
    setCollapsed((current) => {
      const next = new Set(current);
      if (!next.delete(category)) {
        next.add(category);
      }
      return next;
    });

  const onSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") {
      event.preventDefault();
      const first = visible[0]?.variables[0];
      if (first) {
        onInsert(variableSnippet(first.path));
      }
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      visibleRows(listRef.current)[0]?.focus();
    } else if (event.key === "Escape") {
      event.preventDefault();
      if (query) {
        setQuery("");
      } else {
        onEscape();
      }
    }
  };

  const onRowKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
      return;
    }
    const items = visibleRows(listRef.current);
    const index = items.indexOf(event.currentTarget);
    if (index < 0) {
      return;
    }
    event.preventDefault();
    const next = event.key === "ArrowDown" ? index + 1 : index - 1;
    if (next < 0) {
      document.getElementById(searchId)?.focus();
    } else {
      items[Math.min(next, items.length - 1)]?.focus();
    }
  };

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="space-y-2 border-b border-border p-3">
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden />
          <Input
            id={searchId}
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={onSearchKeyDown}
            placeholder="Search variables"
            aria-label="Search variables"
            aria-keyshortcuts={shortcut.aria}
            className="h-8 pl-8 pr-16 text-[13px]"
          />
          <kbd className="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 rounded border border-border bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
            {shortcut.label}
          </kbd>
        </div>
        <p className="text-xs text-muted-foreground" aria-live="polite">
          {terms.length > 0
            ? `${matches} of ${total} variables. Enter inserts the first.`
            : `${total} variables. Click to insert at the cursor.`}
        </p>
      </div>
      <div ref={listRef} className="min-h-0 flex-1 overflow-y-auto">
        {visible.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">
            {total === 0 ? "This template has no variables to insert." : `No variables match “${query.trim()}”.`}
          </p>
        ) : (
          visible.map((group) => {
            const open = terms.length > 0 || !collapsed.has(group.category);
            const regionId = `${searchId}-${slug(group.category)}`;
            return (
              <section key={group.category} aria-label={group.category}>
                <button
                  type="button"
                  aria-expanded={open}
                  aria-controls={regionId}
                  onClick={() => toggle(group.category)}
                  className="sticky top-0 z-10 flex w-full items-center gap-2 border-b border-border bg-card/95 px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground backdrop-blur hover:text-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
                >
                  <ChevronRight className={cn("size-3.5 transition-transform", open && "rotate-90")} aria-hidden />
                  <span className="min-w-0 flex-1 truncate">{group.category}</span>
                  <span className="rounded-full bg-muted px-1.5 py-px text-[10px] tabular-nums text-muted-foreground">
                    {group.variables.length}
                  </span>
                </button>
                <ul id={regionId} hidden={!open} className="py-1">
                  {group.variables.map((variable) => (
                    <VariableRow
                      key={variable.path}
                      variable={variable}
                      terms={terms}
                      onInsert={onInsert}
                      onKeyDown={onRowKeyDown}
                    />
                  ))}
                </ul>
              </section>
            );
          })
        )}
      </div>
    </div>
  );
}

function VariableRow({
  variable,
  terms,
  onInsert,
  onKeyDown,
}: {
  variable: TemplateVariable;
  terms: string[];
  onInsert: (text: string) => void;
  onKeyDown: (event: KeyboardEvent<HTMLButtonElement>) => void;
}) {
  const dot = variable.path.lastIndexOf(".");
  const root = dot >= 0 ? variable.path.slice(0, dot + 1) : "";
  const leaf = dot >= 0 ? variable.path.slice(dot + 1) : variable.path;
  const snippet = variableSnippet(variable.path);

  return (
    <li className="group flex items-start gap-1 pr-2 hover:bg-muted/60 focus-within:bg-muted/60">
      <button
        type="button"
        data-variable
        title={`Insert ${snippet}`}
        onClick={() => onInsert(snippet)}
        onKeyDown={onKeyDown}
        className="min-w-0 flex-1 px-3 py-1.5 text-left focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
      >
        <code className="block text-[12.5px] leading-5 break-words">
          <span className="text-muted-foreground">{highlight(root, terms)}</span>
          <wbr />
          <span className="font-medium text-foreground">{highlight(leaf, terms)}</span>
        </code>
        {variable.description ? (
          <span className="block text-xs leading-4 text-muted-foreground">{variable.description}</span>
        ) : null}
      </button>
      {variable.example ? (
        <RowAction label={`Insert example for ${variable.path}`} onClick={() => onInsert(variable.example ?? "")}>
          <SquareCode aria-hidden />
        </RowAction>
      ) : null}
      <RowAction label={`Copy ${snippet}`} onClick={() => void copyText(snippet, `Copied ${snippet}`)}>
        <Copy aria-hidden />
      </RowAction>
    </li>
  );
}

function RowAction({ label, onClick, children }: { label: string; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      onClick={onClick}
      className="mt-1 flex size-7 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-card hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring [&_svg]:size-3.5"
    >
      {children}
    </button>
  );
}

function filterGroups(groups: TemplateVariableGroup[], terms: string[]): TemplateVariableGroup[] {
  if (terms.length === 0) {
    return groups;
  }
  return groups
    .map((group) => ({
      category: group.category,
      variables: group.variables.filter((variable) => {
        const haystack = `${variable.path} ${variable.description ?? ""} ${group.category}`.toLowerCase();
        return terms.every((term) => haystack.includes(term));
      }),
    }))
    .filter((group) => group.variables.length > 0);
}

function highlight(text: string, terms: string[]): ReactNode {
  const term = terms.find((candidate) => text.toLowerCase().includes(candidate));
  if (!term) {
    return text;
  }
  const start = text.toLowerCase().indexOf(term);
  return (
    <>
      {text.slice(0, start)}
      <mark className="rounded-sm bg-brand-100 text-brand-950">{text.slice(start, start + term.length)}</mark>
      {text.slice(start + term.length)}
    </>
  );
}

function visibleRows(list: HTMLElement | null): HTMLElement[] {
  return Array.from(list?.querySelectorAll<HTMLElement>("[data-variable]") ?? []).filter(
    (row) => !row.closest("[hidden]"),
  );
}

function slug(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, "-");
}

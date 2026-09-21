import { ChevronDown, ChevronUp, ChevronsUpDown } from "lucide-react";
import { cn } from "./cn";
import { TableHead } from "./table";

export type SortDirection = "asc" | "desc";

interface SortableTableHeadProps {
  column: string;
  label: string;
  sort: string;
  dir: string;
  /** First-click direction for this column (text: asc; "active first": desc). */
  naturalDir?: SortDirection;
  onSort: (column: string, naturalDir: SortDirection) => void;
  className?: string;
}

/** Sortable column header. Writes immediately via onSort. */
export function SortableTableHead({
  column,
  label,
  sort,
  dir,
  naturalDir = "asc",
  onSort,
  className,
}: SortableTableHeadProps) {
  const active = sort === column && (dir === "asc" || dir === "desc");
  const ariaSort = !active ? "none" : dir === "desc" ? "descending" : "ascending";
  const nextHint = !active
    ? `Sort by ${label}`
    : dir === naturalDir
      ? `Sort ${label} ${naturalDir === "asc" ? "descending" : "ascending"}`
      : `Clear ${label} sort`;

  return (
    <TableHead aria-sort={ariaSort} className={cn("p-0", className)}>
      <button
        type="button"
        className="inline-flex h-10 w-full items-center gap-1 px-4 text-left text-xs font-semibold uppercase tracking-wide text-muted-foreground hover:text-foreground"
        onClick={() => onSort(column, naturalDir)}
        aria-label={nextHint}
      >
        <span className="truncate">{label}</span>
        {active ? (
          dir === "desc" ? (
            <ChevronDown className="size-3.5 shrink-0" aria-hidden />
          ) : (
            <ChevronUp className="size-3.5 shrink-0" aria-hidden />
          )
        ) : (
          <ChevronsUpDown className="size-3.5 shrink-0 opacity-40" aria-hidden />
        )}
      </button>
    </TableHead>
  );
}

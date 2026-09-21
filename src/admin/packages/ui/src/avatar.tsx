import { cn } from "./cn";

function initialsOf(name: string): string {
  const parts = name.trim().split(/[\s@.]+/).filter(Boolean);
  const initials = parts.length >= 2
    ? parts[0]![0]! + parts[1]![0]!
    : (parts[0]?.slice(0, 2) ?? "?");
  return initials.toUpperCase();
}

/** Initials avatar with the brand gradient; used in the topbar and user lists. */
export function Avatar({
  name,
  className,
}: {
  name: string;
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex size-8 shrink-0 select-none items-center justify-center rounded-full bg-gradient-to-br from-tertiary to-primary text-xs font-semibold text-white",
        className,
      )}
      aria-hidden
    >
      {initialsOf(name)}
    </span>
  );
}

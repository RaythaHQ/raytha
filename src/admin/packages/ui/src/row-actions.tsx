import { MoreHorizontal } from "lucide-react";
import { Button } from "./button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "./dropdown-menu";

export type RowAction = {
  id: string;
  label: string;
  to?: string;
  params?: Record<string, string>;
  onSelect?: () => void;
  destructive?: boolean;
  disabled?: boolean;
};

export function RowActions({ actions, label = "Row actions" }: { actions: RowAction[]; label?: string }) {
  const regular = actions.filter((action) => !action.destructive);
  const destructive = actions.filter((action) => action.destructive);
  if (actions.length === 0) {
    return null;
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger>
        <Button type="button" variant="ghost" size="icon" aria-label={label}>
          <MoreHorizontal />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {regular.map((action) => (
          <RowActionItem key={action.id} action={action} />
        ))}
        {regular.length > 0 && destructive.length > 0 ? <DropdownMenuSeparator /> : null}
        {destructive.map((action) => (
          <RowActionItem key={action.id} action={action} />
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function RowActionItem({ action }: { action: RowAction }) {
  const href = actionHref(action);
  const className = action.destructive ? "text-destructive [&_svg]:text-destructive" : undefined;

  if (href && !action.onSelect) {
    return (
      <a
        href={href}
        role="menuitem"
        tabIndex={-1}
        aria-disabled={action.disabled || undefined}
        className={[
          "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-foreground outline-none transition-colors hover:bg-accent focus-visible:bg-accent",
          action.disabled ? "pointer-events-none opacity-50" : "",
          className ?? "",
        ].join(" ")}
      >
        {action.label}
      </a>
    );
  }

  return (
    <DropdownMenuItem
      disabled={action.disabled}
      className={className}
      onSelect={() => {
        action.onSelect?.();
        if (href) {
          window.location.assign(href);
        }
      }}
    >
      {action.label}
    </DropdownMenuItem>
  );
}

function actionHref(action: RowAction): string | undefined {
  if (!action.to) {
    return undefined;
  }
  let path = action.to;
  for (const [key, value] of Object.entries(action.params ?? {})) {
    path = path.replaceAll(`$${key}`, encodeURIComponent(value));
  }
  return `/raytha${path.startsWith("/") ? path : `/${path}`}`;
}

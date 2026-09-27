import { MoreHorizontal } from "lucide-react";
import type { MouseEvent } from "react";
import { Button } from "./button";
import { followSpaHref } from "./spa-href";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "./dropdown-menu";

export type RowAction = {
  id: string;
  label: string;
  to?: string;
  params?: Record<string, string>;
  onSelect?: () => void;
  disabled?: boolean;
};

export function RowActions({ actions, label = "Row actions" }: { actions: RowAction[]; label?: string }) {
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
        {actions.map((action) => (
          <RowActionItem key={action.id} action={action} />
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function RowActionItem({ action }: { action: RowAction }) {
  const href = actionHref(action);

  if (href && !action.onSelect) {
    return (
      <a
        href={href}
        role="menuitem"
        tabIndex={-1}
        aria-disabled={action.disabled || undefined}
        onClick={(event) => followInternalHref(event, href)}
        className={[
          "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-foreground outline-none transition-colors hover:bg-accent focus-visible:bg-accent",
          action.disabled ? "pointer-events-none opacity-50" : "",
        ].join(" ")}
      >
        {action.label}
      </a>
    );
  }

  return (
    <DropdownMenuItem
      disabled={action.disabled}
      onSelect={() => {
        action.onSelect?.();
        if (href) {
          followSpaHref(href);
        }
      }}
    >
      {action.label}
    </DropdownMenuItem>
  );
}

function followInternalHref(event: MouseEvent<HTMLAnchorElement>, href: string): void {
  if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
    return;
  }
  event.preventDefault();
  followSpaHref(href);
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

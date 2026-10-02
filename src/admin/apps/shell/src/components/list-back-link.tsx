import { buttonVariants } from "@raytha/ui";
import { useNavigate } from "@tanstack/react-router";
import { ArrowLeft } from "lucide-react";
import type { MouseEvent, ReactNode } from "react";
import { listHref, readRememberedListQuery } from "../lib/list-query";

/** In-app link. A plain anchor reloads the document because the admin app lives under `/raytha`. */
export function AppLink({
  href,
  className,
  children,
}: {
  href: string;
  className?: string;
  children: ReactNode;
}) {
  const navigate = useNavigate();
  return (
    <a
      href={href}
      className={className}
      onClick={(event) => {
        if (leaveToBrowser(event)) {
          return;
        }
        event.preventDefault();
        void navigate({ href });
      }}
    >
      {children}
    </a>
  );
}

export function ListBackLink({
  to,
  params,
  listKey,
  label,
}: {
  to: string;
  params?: Record<string, string>;
  listKey: string;
  label: string;
}) {
  const href = listHref(to, params, readRememberedListQuery(listKey));
  return (
    <AppLink href={href} className={buttonVariants({ variant: "ghost" })}>
      <ArrowLeft />
      {`Back to ${label}`}
    </AppLink>
  );
}

function leaveToBrowser(event: MouseEvent<HTMLAnchorElement>): boolean {
  return event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey;
}

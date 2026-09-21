import { buttonVariants } from "@raytha/ui";
import { ArrowLeft } from "lucide-react";
import { listHref, readRememberedListQuery } from "../lib/list-query";

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
    <a href={href} className={buttonVariants({ variant: "ghost" })}>
      <ArrowLeft />
      {`Back to ${label}`}
    </a>
  );
}

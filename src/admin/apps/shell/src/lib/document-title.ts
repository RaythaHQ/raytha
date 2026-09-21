import { useEffect } from "react";

export const APP_DOCUMENT_TITLE = "Raytha Admin";

export function formatDocumentTitle(crumbs: readonly string[]): string {
  const parts = crumbs.filter((crumb) => crumb.trim().length > 0);
  if (parts.length === 0) {
    return APP_DOCUMENT_TITLE;
  }
  return `${[...parts].reverse().join(" · ")} · ${APP_DOCUMENT_TITLE}`;
}

export function useDocumentTitle(crumbs: readonly string[]): void {
  const title = formatDocumentTitle(crumbs);
  useEffect(() => {
    document.title = title;
  }, [title]);
}

/** Same-origin public path for a route, so the admin cookie rides along for draft previews. */
export function publicPath(routePath: string, options?: { previewDraft?: boolean }): string {
  const path = `/${routePath.replace(/^\/+/, "")}`;
  return options?.previewDraft ? `${path}?previewDraft=true` : path;
}

const ADMIN_HOME = "/raytha";

/** A same-origin path to continue to after sign-in, or null for anything that could leave the site. */
export function safeReturnUrl(value: unknown): string | null {
  if (typeof value !== "string" || !value.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) {
    return null;
  }
  return value;
}

export function afterSignInUrl(returnUrl: unknown): string {
  return safeReturnUrl(returnUrl) ?? ADMIN_HOME;
}

/** The router strips its basepath from `location.href`; the server and the browser need it back. */
export function adminHref(routerHref: string): string {
  return routerHref === "/" ? ADMIN_HOME : `${ADMIN_HOME}${routerHref}`;
}

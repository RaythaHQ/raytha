type FollowHref = (href: string) => void;

let followHref: FollowHref | null = null;

/** Shell registers this so row menus can change routes without a document reload. */
export function registerSpaNavigation(follow: FollowHref): void {
  followHref = follow;
}

export function followSpaHref(href: string): void {
  if (followHref) {
    followHref(href);
    return;
  }
  window.location.assign(href);
}

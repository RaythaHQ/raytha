import { cn } from "@raytha/ui";
import { Link, useLocation } from "@tanstack/react-router";

const LINKS = [
  { to: "/content/$developerName", label: "Items" },
  { to: "/content/$developerName/views", label: "Views" },
  { to: "/content-types/$developerName/fields", label: "Fields" },
  { to: "/content-types/$developerName/configuration", label: "Settings" },
  { to: "/content-types/$developerName/trash", label: "Trash" },
] as const;

export function ContentTypeNav({ developerName }: { developerName: string }) {
  const location = useLocation();
  const pathname = location.pathname.replace(/\/+$/, "") || "/";

  return (
    <nav aria-label="Content type" className="flex flex-wrap gap-1 rounded-lg bg-muted p-1">
      {LINKS.map((link) => {
        const href = link.to.replace("$developerName", developerName);
        const active =
          pathname === href ||
          (link.to === "/content/$developerName" &&
            (pathname.endsWith("/new") || pathname.includes("/items/")));
        return (
          <Link
            key={link.to}
            to={link.to}
            params={{ developerName }}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
              active ? "bg-card text-foreground shadow-card" : "text-muted-foreground hover:text-foreground",
            )}
          >
            {link.label}
          </Link>
        );
      })}
    </nav>
  );
}

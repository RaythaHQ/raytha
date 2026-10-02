import { cn } from "@raytha/ui";
import { Link } from "@tanstack/react-router";

const tabs = [
  { id: "users", label: "Users", to: "/users" },
  { id: "groups", label: "User groups", to: "/users/groups" },
] as const;

export function UsersSectionTabs({ active }: { active: "users" | "groups" }) {
  return (
    <div role="tablist" aria-label="Users section" className="inline-flex items-center gap-1 rounded-lg bg-muted p-1">
      {tabs.map((tab) => {
        const selected = tab.id === active;
        return (
          <Link
            key={tab.id}
            to={tab.to}
            activeOptions={{ exact: true }}
            role="tab"
            aria-selected={selected}
            className={cn(
              "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
              selected ? "bg-card text-foreground shadow-card" : "text-muted-foreground hover:text-foreground",
            )}
          >
            {tab.label}
          </Link>
        );
      })}
    </div>
  );
}

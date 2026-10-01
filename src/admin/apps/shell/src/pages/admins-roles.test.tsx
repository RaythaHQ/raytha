import type { Me } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createMemoryHistory, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { NewRolePage } from "./admins";

const session = vi.hoisted(() => ({ current: null as Me | null }));

const CATALOG = [
  { developerName: "system_settings", label: "Manage System Settings" },
  { developerName: "administrators", label: "Manage Administrators" },
  { developerName: "audit_logs", label: "Manage Audit Logs" },
  { developerName: "content_types", label: "Manage Content Types" },
  { developerName: "templates", label: "Manage Templates" },
  { developerName: "users", label: "Manage Users" },
  { developerName: "site_pages", label: "Manage Site Pages" },
  { developerName: "media_items", label: "Manage Media" },
];

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  return {
    ...actual,
    currentSession: () => session.current,
    adminApi: {
      ...actual.adminApi,
      roles: {
        ...actual.adminApi.roles,
        permissions: async () => ({ systemPermissions: CATALOG, contentTypePermissions: [] }),
      },
      contentTypes: {
        ...actual.adminApi.contentTypes,
        list: async () => ({ items: [], totalCount: 0, pageNumber: 1, pageSize: 200 }),
      },
    },
  };
});

function me(roles: string[], permissions: string[]): Me {
  return {
    id: "me",
    email: "admin@example.com",
    firstName: "Ada",
    lastName: "Admin",
    fullName: "Ada Admin",
    permissions,
    contentTypePermissions: [],
    roles,
    isAdmin: true,
    homePageId: null,
    organization: { name: "Raytha", websiteUrl: "https://example.com", timeZone: "UTC", dateFormat: "MM/dd/yyyy", pathBase: "" },
    impersonation: null,
  };
}

function renderNewRole() {
  const rootRoute = createRootRoute();
  const page = createRoute({
    getParentRoute: () => rootRoute,
    path: "/settings/roles/new",
    component: NewRolePage,
  });
  const list = createRoute({
    getParentRoute: () => rootRoute,
    path: "/settings/roles",
    component: () => <h1>Roles</h1>,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([page, list]),
    history: createMemoryHistory({ initialEntries: ["/settings/roles/new"] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

function box(name: string) {
  return screen.getByRole("checkbox", { name });
}

const implied = CATALOG.filter(
  (option) => option.developerName !== "system_settings" && option.developerName !== "administrators",
).map((option) => option.label);

describe("new role permissions", () => {
  beforeEach(() => {
    session.current = me(["super_admin"], CATALOG.map((option) => option.developerName));
  });

  it("checking Manage System Settings checks and locks every other permission", async () => {
    renderNewRole();
    fireEvent.click(await screen.findByRole("checkbox", { name: "Manage System Settings" }));

    expect(box("Manage System Settings")).toBeChecked();
    expect(box("Manage Administrators")).toBeChecked();
    expect(box("Manage Administrators")).toBeEnabled();
    for (const label of implied) {
      expect(box(label)).toBeChecked();
      expect(box(label)).toBeDisabled();
    }
    expect(screen.getAllByText("Included with Manage System Settings").length).toBe(implied.length);

    fireEvent.click(box("Manage System Settings"));
    expect(box("Manage System Settings")).not.toBeChecked();
    expect(box("Manage Administrators")).not.toBeChecked();
    for (const label of implied) {
      expect(box(label)).toBeChecked();
      expect(box(label)).toBeEnabled();
    }
  });

  it("refuses full trust when the caller lacks any permission it includes", async () => {
    session.current = me(
      ["editor"],
      CATALOG.map((option) => option.developerName).filter((name) => name !== "media_items"),
    );
    renderNewRole();

    const settings = await screen.findByRole("checkbox", { name: "Manage System Settings" });
    expect(settings).toBeDisabled();
    expect(screen.getByText("Includes every permission. You do not have Manage Media.")).toBeTruthy();
  });
});

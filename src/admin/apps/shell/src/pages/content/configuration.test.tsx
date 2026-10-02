import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  RouterProvider,
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import type { ComponentType } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ContentTypeConfigurationPage } from "./configuration";

const access = vi.hoisted(() => ({ manageContentTypes: true }));
const removeByDeveloperName = vi.hoisted(() => vi.fn(async () => undefined));

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  return {
    ...actual,
    hasPermission: (permission: string) =>
      access.manageContentTypes && permission === actual.platformPermissions.contentTypes,
    adminApi: {
      ...actual.adminApi,
      contentTypes: {
        ...actual.adminApi.contentTypes,
        byDeveloperName: async () => ({
          id: "ct-posts",
          developerName: "posts",
          labelSingular: "Post",
          labelPlural: "Posts",
          description: "",
          defaultRouteTemplate: "{ContentTypeDeveloperName}/{PrimaryField}",
          primaryFieldId: "field-title",
          contentTypeFields: [
            {
              id: "field-title",
              label: "Title",
              developerName: "title",
              fieldType: "single_line_text",
            },
          ],
        }),
        removeByDeveloperName,
      },
    },
  };
});

function renderSettings() {
  const rootRoute = createRootRoute();
  const settings = createRoute({
    getParentRoute: () => rootRoute,
    path: "/content-types/$developerName/configuration",
    component: ContentTypeConfigurationPage as ComponentType,
  });
  const items = createRoute({
    getParentRoute: () => rootRoute,
    path: "/content/$developerName",
    component: () => null,
  });
  const list = createRoute({
    getParentRoute: () => rootRoute,
    path: "/content-types",
    component: () => <h1>Content types</h1>,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([settings, items, list]),
    history: createMemoryHistory({ initialEntries: ["/content-types/posts/configuration"] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

describe("content type settings", () => {
  beforeEach(() => {
    access.manageContentTypes = true;
    removeByDeveloperName.mockClear();
  });

  it("deletes the content type from the danger zone and returns to the list", async () => {
    renderSettings();
    fireEvent.click(await screen.findByRole("button", { name: "Delete content type" }));
    expect(screen.getByRole("dialog", { name: "Delete Posts?" })).toBeTruthy();
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Delete content type" }));
    await waitFor(() => expect(removeByDeveloperName).toHaveBeenCalledWith("posts"));
    expect(await screen.findByRole("heading", { name: "Content types" })).toBeTruthy();
  });

  it("hides the danger zone from an admin who cannot manage content types", async () => {
    access.manageContentTypes = false;
    renderSettings();
    expect(await screen.findByRole("button", { name: "Save" })).toBeTruthy();
    expect(screen.queryByRole("heading", { name: "Danger zone" })).toBeNull();
  });
});

import { QueryClient, QueryClientProvider, keepPreviousData } from "@tanstack/react-query";
import { RouterProvider, createMemoryHistory, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { render, screen } from "@testing-library/react";
import type { ComponentType } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { remountOnParamChange } from "../../lib/router-options";
import { EditContentItemPage } from "./editor";

const api = vi.hoisted(() => ({
  update: vi.fn(async () => ({ id: "b" })),
  items: {} as Record<string, unknown>,
}));

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  return {
    ...actual,
    adminApi: {
      ...actual.adminApi,
      contentTypes: {
        ...actual.adminApi.contentTypes,
        byDeveloperName: async () => ({
          id: "ct-posts",
          developerName: "posts",
          labelSingular: "Post",
          labelPlural: "Posts",
          primaryFieldId: "field-title",
          contentTypeFields: [{ id: "field-title", label: "Title", developerName: "title", fieldType: "single_line_text" }],
        }),
        templates: async () => [{ id: "tpl", label: "Post", developerName: "post" }],
      },
      contentItems: () => ({
        get: (id: string) => (id in api.items ? Promise.resolve(api.items[id]) : new Promise(() => {})),
        revisions: async () => ({ items: [], totalCount: 0 }),
        update: api.update,
      }),
    },
  };
});

function renderEditor(path: string) {
  const rootRoute = createRootRoute();
  const editor = createRoute({
    getParentRoute: () => rootRoute,
    path: "/content/$developerName/items/$id",
    component: EditContentItemPage as ComponentType,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([editor]),
    history: createMemoryHistory({ initialEntries: [path] }),
    defaultRemountDeps: remountOnParamChange,
  });
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, placeholderData: keepPreviousData } },
  });
  render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return router;
}

describe("content item editor", () => {
  beforeEach(() => {
    api.update.mockClear();
    api.items = {
      a: {
        id: "a",
        primaryField: "Alpha",
        routePath: "alpha",
        isPublished: true,
        isDraft: false,
        draftContent: { title: "Alpha" },
        publishedContent: { title: "Alpha" },
      },
    };
  });

  it("offers no save while the next item loads, so the previous item's fields cannot be written to it", async () => {
    const router = renderEditor("/content/posts/items/a");
    expect(await screen.findByDisplayValue("Alpha")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Publish" })).toBeInTheDocument();

    await router.navigate({ to: "/content/$developerName/items/$id", params: { developerName: "posts", id: "b" } });

    expect(screen.queryByRole("button", { name: "Publish" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save settings" })).not.toBeInTheDocument();
    expect(screen.queryByText("Alpha")).not.toBeInTheDocument();
    expect(api.update).not.toHaveBeenCalled();
  });

  it("is mounted by an admin router that remounts a route when its params change", async () => {
    const { router } = await import("../../router");
    expect(router.options.defaultRemountDeps).toBe(remountOnParamChange);
  });
});

import type { SaveSitePageWidgetsInput } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createMemoryHistory, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { axe } from "jest-axe";
import type { ComponentType } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import fixture from "./widget-fixture.test-data.json";
import { EditSitePageWidgetPage, NewSitePageWidgetPage } from "./widget-page";

const saveWidgets = vi.fn<(pageId: string, body: SaveSitePageWidgetsInput) => Promise<void>>();

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  const page = <T,>(items: T[]) => ({ items, totalCount: items.length, pageNumber: 1, pageSize: 100 });
  return {
    ...actual,
    adminApi: {
      ...actual.adminApi,
      sitePages: {
        ...actual.adminApi.sitePages,
        get: async () => fixture.page,
        widgetDefinitions: async () => actual.parseWidgetDefinitions(fixture.definitions),
        saveWidgets: (pageId: string, body: SaveSitePageWidgetsInput) => saveWidgets(pageId, body),
      },
      contentTypes: { list: async () => page([{ id: "ct-posts", developerName: "posts", labelPlural: "Posts" }]) },
      views: () => ({ list: async () => page([{ id: "tDofqaGRJESssWjQRZOmVw", label: "All posts" }]) }),
    },
  };
});

const PAGE_ID = fixture.page.id;
const stored = fixture.page.widgets.main;
/** CardTitle is an h3 under PageHeader's h1 on every admin page; that kit-wide order is out of scope here. */
const PAGE_AXE = { rules: { "heading-order": { enabled: false } } };

function renderAt(path: string, pattern: string, component: ComponentType) {
  const rootRoute = createRootRoute();
  const route = createRoute({ getParentRoute: () => rootRoute, path: pattern, component });
  const layoutRoute = createRoute({ getParentRoute: () => rootRoute, path: "/site-pages/$id/layout", component: () => null });
  const router = createRouter({
    routeTree: rootRoute.addChildren([route, layoutRoute]),
    history: createMemoryHistory({ initialEntries: [path] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  saveWidgets.mockReset();
  saveWidgets.mockResolvedValue(undefined);
});

describe("site page widget editor", () => {
  it.each(stored.map((widget) => [widget.widgetType, widget.id] as const))(
    "saving %s %s untouched sends every widget in the section back as it was stored",
    async (_type, widgetId) => {
      renderAt(
        `/site-pages/${PAGE_ID}/layout/widgets/${widgetId}`,
        "/site-pages/$id/layout/widgets/$widgetId",
        EditSitePageWidgetPage,
      );
      const [save] = await screen.findAllByRole("button", { name: "Save" });
      fireEvent.click(save as HTMLElement);
      await waitFor(() => expect(saveWidgets).toHaveBeenCalledTimes(1));

      const [pageId, body] = saveWidgets.mock.calls[0] ?? [];
      expect(pageId).toBe(PAGE_ID);
      expect(body?.sectionName).toBe("main");
      expect(body?.widgets).toStrictEqual(
        stored.map((widget) => ({
          id: widget.id,
          widgetType: widget.widgetType,
          settingsJson: widget.settingsJson,
          row: widget.row,
          column: widget.column,
          columnSpan: widget.columnSpan,
          cssClass: widget.cssClass,
          htmlId: widget.htmlId,
          customAttributes: widget.customAttributes,
        })),
      );
    },
  );

  it("adds a new widget with only its template's defaults", async () => {
    const { container } = renderAt(
      `/site-pages/${PAGE_ID}/layout/widgets/new?section=main`,
      "/site-pages/$id/layout/widgets/new",
      NewSitePageWidgetPage,
    );
    fireEvent.click(await screen.findByRole("radio", { name: /faq/i }));
    expect(await screen.findByRole("checkbox", { name: /expand first/i })).toBeChecked();
    expect(await axe(container, PAGE_AXE)).toHaveNoViolations();

    const [save] = screen.getAllByRole("button", { name: "Add widget" });
    fireEvent.click(save as HTMLElement);
    await waitFor(() => expect(saveWidgets).toHaveBeenCalledTimes(1));
    const body = saveWidgets.mock.calls[0]?.[1];
    const added = body?.widgets.at(-1);
    expect(body?.widgets).toHaveLength(stored.length + 1);
    expect(added).toMatchObject({ widgetType: "faq", row: Math.max(...stored.map((widget) => widget.row)) + 1 });
    expect(added).not.toHaveProperty("id");
    expect(JSON.parse(added?.settingsJson ?? "")).toStrictEqual({ expandFirst: true });
  });

  it("renders the generated hero form with no axe violations", async () => {
    const hero = stored.find((widget) => widget.widgetType === "hero");
    const { container } = renderAt(
      `/site-pages/${PAGE_ID}/layout/widgets/${hero?.id ?? ""}`,
      "/site-pages/$id/layout/widgets/$widgetId",
      EditSitePageWidgetPage,
    );
    expect(await screen.findByDisplayValue("Fixture hero")).toBeInTheDocument();
    expect(await axe(container, PAGE_AXE)).toHaveNoViolations();
  });
});

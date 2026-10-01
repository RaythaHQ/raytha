import type { EntityRef, PagedResult } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createMemoryHistory, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CrudListPage } from "./crud-list";

type Person = EntityRef & { name: string };

const people: Person[] = Array.from({ length: 60 }, (_, index) => ({ id: `u${index + 1}`, name: `Person ${index + 1}` }));

async function listPeople(params?: Record<string, string | number | boolean | undefined>): Promise<PagedResult<EntityRef>> {
  const pageSize = Number(params?.pageSize ?? 50);
  const pageNumber = Number(params?.pageNumber ?? 1);
  const start = (pageNumber - 1) * pageSize;
  return { items: people.slice(start, start + pageSize), totalCount: people.length, pageNumber, pageSize };
}

function PeoplePage() {
  return (
    <CrudListPage
      title="People"
      queryKey={["people"]}
      listKey="people"
      noun="person"
      list={listPeople}
      columns={[{ header: "Name", cell: (entity) => (entity as Person).name }]}
    />
  );
}

function renderPeople() {
  const rootRoute = createRootRoute();
  const route = createRoute({ getParentRoute: () => rootRoute, path: "/people", component: PeoplePage });
  const router = createRouter({
    routeTree: rootRoute.addChildren([route]),
    history: createMemoryHistory({ initialEntries: ["/people"] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return router;
}

describe("CrudListPage", () => {
  it("pages past the first 50 rows", async () => {
    const router = renderPeople();

    expect(await screen.findByText("Person 50")).toBeInTheDocument();
    expect(screen.queryByText("Person 51")).not.toBeInTheDocument();
    expect(screen.getByText("Page 1 of 2")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Next" }));

    expect(await screen.findByText("Person 60")).toBeInTheDocument();
    expect(screen.getByText("Page 2 of 2")).toBeInTheDocument();
    expect(router.state.location.search).toMatchObject({ pageNumber: "2" });
  });
});

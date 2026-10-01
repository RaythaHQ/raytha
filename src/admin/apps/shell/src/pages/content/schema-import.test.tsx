import { ApiError, type SchemaImportResult } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  RouterProvider,
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ComponentType } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { NewContentTypePage } from "../content";
import { ExportSchemaButton } from "./schema-export";

const access = vi.hoisted(() => ({ manageContentTypes: true }));
const importSchema = vi.hoisted(() => vi.fn());
const exportSchema = vi.hoisted(() => vi.fn());
const downloadJson = vi.hoisted(() => vi.fn());

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  return {
    ...actual,
    hasPermission: (permission: string) =>
      access.manageContentTypes && permission === actual.platformPermissions.contentTypes,
    adminApi: {
      ...actual.adminApi,
      contentTypes: { ...actual.adminApi.contentTypes, importSchema, exportSchema },
    },
  };
});

vi.mock("../../lib/download", () => ({ downloadJson }));

// Uppy needs a real browser; a file input stands in for the dashboard and reports the same File.
vi.mock("@raytha/ui", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/ui")>();
  return {
    ...actual,
    FileDrop: ({ onFileChange }: { onFileChange: (file: File | null) => void }) => (
      <input
        aria-label="Schema file"
        type="file"
        onChange={(event) => onFileChange(event.target.files?.[0] ?? null)}
      />
    ),
  };
});

const SCHEMA = { schemaVersion: 1, contentTypes: [{ developerName: "posts", fields: [{}, {}], views: [{}] }] };

const PREVIEW: SchemaImportResult = {
  dryRun: true,
  created: 3,
  updated: 1,
  unchanged: 0,
  changes: [
    { kind: "content_type", contentType: "posts", name: null, action: "created", details: [] },
    { kind: "field", contentType: "posts", name: "title", action: "created", details: [] },
    { kind: "view", contentType: "posts", name: "all", action: "updated", details: ["label"] },
  ],
  warnings: ["posts.views.all: template 'x' is not available."],
};

function renderPage(page: ComponentType) {
  const rootRoute = createRootRoute();
  const newType = createRoute({ getParentRoute: () => rootRoute, path: "/content-types/new", component: page });
  const list = createRoute({
    getParentRoute: () => rootRoute,
    path: "/content-types",
    component: () => <h1>Content types</h1>,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([newType, list]),
    history: createMemoryHistory({ initialEntries: ["/content-types/new"] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

async function chooseSchemaFile(content: string) {
  fireEvent.click(await screen.findByRole("tab", { name: "Import schema" }));
  const input = await screen.findByLabelText("Schema file");
  const file = new File([content], "schema.json", { type: "application/json" });
  // jsdom's File has no text(); the panel only needs the text of the file.
  Object.defineProperty(file, "text", { value: async () => content });
  fireEvent.change(input, { target: { files: [file] } });
}

describe("import schema", () => {
  beforeEach(() => {
    access.manageContentTypes = true;
    importSchema.mockReset();
    exportSchema.mockReset();
    downloadJson.mockReset();
  });

  it("previews the file with a dry run, then imports it and returns to the list", async () => {
    importSchema.mockResolvedValueOnce(PREVIEW).mockResolvedValueOnce({ ...PREVIEW, dryRun: false });
    renderPage(NewContentTypePage as ComponentType);

    await chooseSchemaFile(JSON.stringify(SCHEMA));

    expect(await screen.findByText("3 created")).toBeTruthy();
    expect(importSchema).toHaveBeenCalledWith(SCHEMA, true);
    expect(screen.getByText("title")).toBeTruthy();
    expect(screen.getByText(/template 'x' is not available/)).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Import schema" }));
    await waitFor(() => expect(importSchema).toHaveBeenLastCalledWith(SCHEMA, false));
    expect(await screen.findByRole("heading", { name: "Content types" })).toBeTruthy();
  });

  it("shows the server's errors and keeps Import disabled", async () => {
    const problem = { errors: { Schema: ["posts.title: unknown field type 'nope'.", "posts.views.all: bad route."] } };
    importSchema.mockRejectedValueOnce(new ApiError(400, JSON.stringify(problem)));
    renderPage(NewContentTypePage as ComponentType);

    await chooseSchemaFile(JSON.stringify(SCHEMA));

    expect(await screen.findByText("posts.title: unknown field type 'nope'.")).toBeTruthy();
    expect(screen.getByText("posts.views.all: bad route.")).toBeTruthy();
    expect((screen.getByRole("button", { name: "Import schema" }) as HTMLButtonElement).disabled).toBe(true);
    expect(importSchema).toHaveBeenCalledTimes(1);
  });

  it("rejects a file that is not a schema without calling the server", async () => {
    renderPage(NewContentTypePage as ComponentType);

    await chooseSchemaFile("not json");

    expect(await screen.findByText("This file is not valid JSON.")).toBeTruthy();
    expect(importSchema).not.toHaveBeenCalled();
  });

  it("names the missing permission instead of offering import", async () => {
    access.manageContentTypes = false;
    renderPage(NewContentTypePage as ComponentType);

    expect(await screen.findByText(/Manage Content Types/)).toBeTruthy();
    expect(screen.queryByRole("tab", { name: "Import schema" })).toBeNull();
  });
});

describe("export schema", () => {
  beforeEach(() => {
    exportSchema.mockReset();
    downloadJson.mockReset();
  });

  it("downloads the exported document as a dated file", async () => {
    exportSchema.mockResolvedValueOnce(SCHEMA);
    renderPage(ExportSchemaButton as ComponentType);

    fireEvent.click(await screen.findByRole("button", { name: /Export schema/ }));

    await waitFor(() => expect(downloadJson).toHaveBeenCalledTimes(1));
    expect(downloadJson).toHaveBeenCalledWith(expect.stringMatching(/^raytha-schema-\d{4}-\d{2}-\d{2}\.json$/), SCHEMA);
  });
});

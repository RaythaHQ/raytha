import { parseWidgetFieldTypeOptions, parseWidgetFields, parseWidgetTemplate, type WidgetField } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  RouterProvider,
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { axe } from "jest-axe";
import { useState, type ComponentType } from "react";
import { describe, expect, it, vi } from "vitest";
import fixture from "../site-pages/widget-fixture.test-data.json";
import { widgetTemplateVariables } from "./liquid-catalog";
import { WidgetFieldsEditor } from "./widget-fields-editor";
import { emptyFieldDraft, fieldDrafts, fieldsPayload, moveItem, toCamelCase, withLabel, type WidgetFieldDraft } from "./widget-fields-model";
import { NewWidgetTemplatePage, WidgetTemplateEditorPage } from "./widget-templates";

const typeOptions = parseWidgetFieldTypeOptions(fixture.fieldTypes);
const faq = fixture.definitions.find((definition) => definition.developerName === "faq");
const faqTemplate = parseWidgetTemplate({
  id: "tpl-faq",
  themeId: "theme-1",
  label: "FAQ",
  developerName: "faq",
  content: "<section>{% for item in widget.settings.items %}{{ item.question }}{% endfor %}</section>",
  isBuiltInTemplate: true,
  fields: faq?.fields ?? [],
});

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  const empty = { items: [], totalCount: 0, pageNumber: 1, pageSize: 50 };
  return {
    ...actual,
    hasPermission: () => true,
    adminApi: {
      ...actual.adminApi,
      themes: {
        ...actual.adminApi.themes,
        widgetFieldTypes: async () => typeOptions,
        get: async () => ({ id: "theme-1", title: "Theme", developerName: "theme" }),
      },
      widgetTemplates: () => ({
        get: async () => faqTemplate,
        revisions: async () => empty,
        list: async () => empty,
      }),
    },
  };
});

function renderAt(path: string, pattern: string, component: ComponentType) {
  const rootRoute = createRootRoute();
  const route = createRoute({ getParentRoute: () => rootRoute, path: pattern, component });
  const listRoute = createRoute({ getParentRoute: () => rootRoute, path: "/themes/$themeId/widget-templates", component: () => null });
  const router = createRouter({
    routeTree: rootRoute.addChildren([route, listRoute]),
    history: createMemoryHistory({ initialEntries: [path] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

/** CardTitle is an h3 under PageHeader's h1 on every admin page; that kit-wide order is out of scope here. */
const PAGE_AXE = { rules: { "heading-order": { enabled: false } } };

function names(fields: readonly WidgetField[]) {
  return fields.map((field) => field.developerName);
}

describe("widget field drafts", () => {
  it("derives a camelCase developer name from the label until it is edited", () => {
    expect(toCamelCase("Background color")).toBe("backgroundColor");
    expect(toCamelCase("2nd button URL")).toBe("field2ndButtonUrl");
    const derived = withLabel(emptyFieldDraft(), "Button text");
    expect(derived.developerName).toBe("buttonText");
    const touched = withLabel({ ...derived, developerName: "cta", nameTouched: true }, "Call to action");
    expect(touched.developerName).toBe("cta");
  });

  it("round-trips a built-in's fields through drafts unchanged", () => {
    const fields = parseWidgetFields(faq?.fields);
    expect(fieldsPayload(fieldDrafts(fields), typeOptions)).toStrictEqual(fields);
  });

  it("reorders fields with moveItem", () => {
    const drafts = fieldDrafts(faqTemplate.fields);
    const moved = moveItem(drafts, 0, 1);
    expect(names(fieldsPayload(moved, typeOptions))).toStrictEqual([
      drafts[1]?.developerName,
      drafts[0]?.developerName,
      ...drafts.slice(2).map((draft) => draft.developerName),
    ]);
    expect(moveItem(drafts, 0, -1)).toStrictEqual(drafts);
  });

  it("reorders fields from the editor and the payload follows", () => {
    const changes = vi.fn();
    function Harness() {
      const [fields, setFields] = useState<WidgetFieldDraft[]>(() => fieldDrafts(faqTemplate.fields));
      return (
        <WidgetFieldsEditor
          fields={fields}
          typeOptions={typeOptions}
          onChange={(next) => {
            setFields(next);
            changes(fieldsPayload(next, typeOptions));
          }}
        />
      );
    }
    render(<Harness />);
    const before = names(faqTemplate.fields);
    fireEvent.click(screen.getByRole("button", { name: "Move Headline down" }));
    const after = changes.mock.calls.at(-1)?.[0] as WidgetField[];
    expect(names(after)).toStrictEqual([before[1], before[0], ...before.slice(2)]);
    const items = within(screen.getAllByRole("list")[0] as HTMLElement).getAllByRole("listitem");
    expect(items[0]).toHaveTextContent("Subheadline");
    expect(items[1]).toHaveTextContent("Headline");
  });
});

describe("insert variable entries", () => {
  it("lists every field and a loop snippet for a repeater", () => {
    const groups = widgetTemplateVariables([
      { developerName: "title", label: "Title", fieldType: "single_line_text", subFields: [] },
      {
        developerName: "slides",
        label: "Slides",
        fieldType: "repeater",
        subFields: [
          { developerName: "image", label: "Image", fieldType: "image", subFields: [] },
          { developerName: "caption", label: "Caption", fieldType: "single_line_text", subFields: [] },
        ],
      },
      { developerName: "", label: "Unnamed", fieldType: "number", subFields: [] },
    ]);
    const settings = groups.find((group) => group.category === "Widget settings");
    expect(settings?.variables.map((variable) => variable.path)).toStrictEqual([
      "widget.settings.title",
      "widget.settings.slides",
    ]);
    expect(settings?.variables[1]?.example).toBe(
      "{% for row in widget.settings.slides %}{{ row.image }}{{ row.caption }}{% endfor %}",
    );
    expect(groups.some((group) => group.variables.some((variable) => variable.path === "widget.css_class"))).toBe(true);
  });

  it("drops the settings group when there are no fields", () => {
    expect(widgetTemplateVariables([]).map((group) => group.category)).toStrictEqual(["Widget"]);
  });
});

describe("widget template pages", () => {
  it("new template page starts on the fields tab with a headline field and no axe violations", async () => {
    const { container } = renderAt(
      "/themes/theme-1/widget-templates/new",
      "/themes/$themeId/widget-templates/new",
      NewWidgetTemplatePage,
    );
    expect(await screen.findByRole("heading", { name: "New widget template" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: /fields/i })).toHaveAttribute("aria-selected", "true");
    expect(await screen.findByRole("button", { name: /headline/i, expanded: false })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/^label/i, { selector: "#new-widget-label" }), {
      target: { value: "Promo banner" },
    });
    expect(screen.getByLabelText(/^developer name/i, { selector: "#new-widget-developer-name" })).toHaveValue(
      "promo_banner",
    );
    expect(await axe(container, PAGE_AXE)).toHaveNoViolations();
  });

  it("editor fields tab lists the template's fields with no axe violations", async () => {
    const { container } = renderAt(
      "/themes/theme-1/widget-templates/tpl-faq",
      "/themes/$themeId/widget-templates/$id",
      WidgetTemplateEditorPage,
    );
    fireEvent.click(await screen.findByRole("tab", { name: /fields/i }));
    for (const field of faqTemplate.fields) {
      expect(screen.getByRole("button", { name: new RegExp(`^\\d+\\s*${field.label}`) })).toBeInTheDocument();
    }
    expect(await axe(container, PAGE_AXE)).toHaveNoViolations();
  });
});

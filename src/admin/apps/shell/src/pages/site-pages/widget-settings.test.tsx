import { parseWidgetDefinitions, type SitePageWidgetDefinition } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { axe } from "jest-axe";
import { useState, type ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import fixture from "./widget-fixture.test-data.json";
import { newWidget, parseWidget, settingsFromJson, settingsJson, withSetting, type WidgetSettings } from "./models";
import { WidgetSettingsForm } from "./widget-settings-form";

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  const page = <T,>(items: T[]) => ({ items, totalCount: items.length, pageNumber: 1, pageSize: 100 });
  return {
    ...actual,
    adminApi: {
      ...actual.adminApi,
      contentTypes: {
        list: async () =>
          page([
            { id: "ct-posts", developerName: "posts", labelPlural: "Posts" },
            { id: "ct-pages", developerName: "pages", labelPlural: "Pages" },
          ]),
      },
      views: (contentType: string) => ({
        list: async () =>
          page(
            contentType === "posts"
              ? [{ id: "tDofqaGRJESssWjQRZOmVw", label: "All posts" }]
              : [{ id: "pages-view", label: "All pages" }],
          ),
      }),
    },
  };
});

const definitions = parseWidgetDefinitions(fixture.definitions);
const definitionOf = (widgetType: string): SitePageWidgetDefinition => {
  const found = definitions.find((definition) => definition.developerName === widgetType);
  if (!found) {
    throw new Error(`fixture has no ${widgetType} definition`);
  }
  return found;
};

function wrap(children: ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
}

/** Renders the form like the widget page does: settings live in state and only change through onChange. */
function Harness({
  widgetType,
  initial,
  onSettings,
}: {
  widgetType: string;
  initial: WidgetSettings;
  onSettings: (settings: WidgetSettings) => void;
}) {
  const [settings, setSettings] = useState(initial);
  const definition = definitions.find((item) => item.developerName === widgetType);
  return (
    <WidgetSettingsForm
      widgetType={widgetType}
      definition={definition}
      settings={settings}
      onChange={(next) => {
        setSettings(next);
        onSettings(next);
      }}
    />
  );
}

function typesOf(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(typesOf);
  }
  if (value !== null && typeof value === "object") {
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, typesOf(item)]));
  }
  return value === null ? "null" : typeof value;
}

describe("widget settings round trip", () => {
  it.each(fixture.widgets.map((widget, index) => [`${index} ${widget.widgetType}`, widget] as const))(
    "saves %s back unchanged after every control is focused and blurred",
    async (_name, stored) => {
      const widget = parseWidget({ id: "w", widgetType: stored.widgetType, settingsJson: stored.settingsJson });
      let latest = widget.settings;
      const changes = vi.fn((next: WidgetSettings) => {
        latest = next;
      });
      const { container } = render(
        wrap(<Harness widgetType={stored.widgetType} initial={widget.settings} onSettings={changes} />),
      );
      await act(() => new Promise((resolve) => setTimeout(resolve, 0)));

      for (const control of container.querySelectorAll<HTMLElement>("input, select, textarea, [contenteditable]")) {
        fireEvent.focus(control);
        fireEvent.blur(control);
      }

      const saved = settingsJson(latest);
      expect(saved).toBe(stored.settingsJson);
      const before: unknown = JSON.parse(stored.settingsJson);
      const after: unknown = JSON.parse(saved);
      expect(after).toStrictEqual(before);
      expect(typesOf(after)).toStrictEqual(typesOf(before));
      expect(changes).not.toHaveBeenCalled();
    },
  );

  it("keeps legacy values and unknown keys when another setting changes", () => {
    const embed = fixture.widgets.find((widget) => widget.widgetType === "embed");
    if (!embed) throw new Error("fixture has no embed");
    const stored = JSON.stringify({ ...JSON.parse(embed.settingsJson), legacyFlag: 1 });
    const next = withSetting(settingsFromJson(stored), definitionOf("embed").fields, "caption", "New caption");

    expect(JSON.parse(settingsJson(next))).toStrictEqual({
      ...JSON.parse(stored),
      caption: "New caption",
    });
    expect(next.values?.maxWidth).toBe("640px");
    expect(next.values?.legacyFlag).toBe(1);
  });

  it("does not inject defaults into an existing widget", () => {
    const sparse = fixture.widgets.find((widget) => widget.settingsJson === '{"contentType": "posts", "displayStyle": "compact", "pageSize": 5}');
    if (!sparse) throw new Error("fixture has no sparse contentlist");
    const widget = parseWidget({ widgetType: "contentlist", settingsJson: sparse.settingsJson });
    expect(Object.keys(widget.settings.values ?? {})).toEqual(["contentType", "displayStyle", "pageSize"]);
  });

  it("removes a key when its value is cleared, as the old form did", () => {
    const next = withSetting(settingsFromJson('{"headline": "Hi", "buttonText": "Go"}'), definitionOf("hero").fields, "buttonText", "");
    expect(JSON.parse(settingsJson(next))).toStrictEqual({ headline: "Hi" });
  });
});

describe("new widgets", () => {
  it.each(definitions.map((definition) => [definition.developerName, definition] as const))(
    "%s starts from its template's defaults and nothing else",
    (_name, definition) => {
      const widget = newWidget(definition.developerName, definition.fields, 4);
      const expected = Object.fromEntries(
        definition.fields.filter((field) => field.defaultValue !== null).map((field) => [field.developerName, field.defaultValue]),
      );
      expect(widget.settings.values).toStrictEqual(expected);
      expect(JSON.parse(settingsJson(widget.settings))).toStrictEqual(expected);
      expect(widget.row).toBe(4);
      expect(widget.id).toBe("");
    },
  );

  it("gives faq and contentlist their typed defaults", () => {
    expect(newWidget("faq", definitionOf("faq").fields, 0).settings.values).toStrictEqual({ expandFirst: true });
    expect(newWidget("contentlist", definitionOf("contentlist").fields, 0).settings.values).toMatchObject({
      pageSize: 3,
      showImage: true,
      displayStyle: "cards",
    });
  });
});

describe("content type and view", () => {
  const contentlist = () => fixture.widgets.find((widget) => widget.settingsJson.includes('"viewId"'));

  it("clears the view when the content type changes", () => {
    const stored = contentlist();
    if (!stored) throw new Error("fixture has no contentlist with a view");
    const next = withSetting(settingsFromJson(stored.settingsJson), definitionOf("contentlist").fields, "contentType", "pages");
    expect(next.values?.contentType).toBe("pages");
    expect(next.values).not.toHaveProperty("viewId");
    expect(next.values?.headline).toBe("Latest");
  });

  it("keeps the view when an unrelated setting changes", () => {
    const stored = contentlist();
    if (!stored) throw new Error("fixture has no contentlist with a view");
    const next = withSetting(settingsFromJson(stored.settingsJson), definitionOf("contentlist").fields, "headline", "Newest");
    expect(next.values?.viewId).toBe("tDofqaGRJESssWjQRZOmVw");
  });

  it("clears the view from the form and lists the new type's views", async () => {
    const stored = contentlist();
    if (!stored) throw new Error("fixture has no contentlist with a view");
    const changes = vi.fn();
    render(wrap(<Harness widgetType="contentlist" initial={settingsFromJson(stored.settingsJson)} onSettings={changes} />));

    const view = await screen.findByRole("combobox", { name: /view/i });
    await waitFor(() => expect(view).toHaveDisplayValue("All posts"));

    fireEvent.change(screen.getByRole("combobox", { name: /content type/i }), { target: { value: "pages" } });

    const last = changes.mock.calls.at(-1)?.[0] as WidgetSettings;
    expect(last.values).not.toHaveProperty("viewId");
    await waitFor(() => expect(view).toHaveDisplayValue("Default view"));
    expect(await screen.findByRole("option", { name: "All pages" })).toBeInTheDocument();
    expect(screen.queryByRole("option", { name: "All posts" })).not.toBeInTheDocument();
  });
});

describe("widget settings form", () => {
  it("falls back to a JSON editor for a widget type the theme lacks", () => {
    const changes = vi.fn();
    render(wrap(<Harness widgetType="retired_widget" initial={settingsFromJson('{"a": 1}')} onSettings={changes} />));
    const editor = screen.getByLabelText("Settings JSON");
    expect(editor).toHaveValue('{"a": 1}');
    fireEvent.change(editor, { target: { value: '{"a": 2}' } });
    expect((changes.mock.calls.at(-1)?.[0] as WidgetSettings).json).toBe('{"a": 2}');
  });

  it("renders the hero form from its fields with no axe violations", async () => {
    const hero = fixture.widgets.find((widget) => widget.widgetType === "hero");
    if (!hero) throw new Error("fixture has no hero");
    const { container } = render(wrap(<Harness widgetType="hero" initial={settingsFromJson(hero.settingsJson)} onSettings={vi.fn()} />));
    expect(screen.getByLabelText(/^headline/i)).toHaveValue("Fixture hero");
    expect(screen.getByRole("combobox", { name: /button style/i })).toHaveValue("outline-light");
    expect(screen.getByRole("spinbutton", { name: /minimum height/i })).toHaveValue(520);
    expect(await axe(container)).toHaveNoViolations();
  });
});

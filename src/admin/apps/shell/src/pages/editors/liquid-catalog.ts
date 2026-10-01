import type { TemplateVariable, TemplateVariableGroup, WidgetField } from "@raytha/api";

/**
 * Liquid vocabulary the template editors offer. Raytha filters and functions mirror what
 * `Raytha.Web/Services/RenderEngine.cs` registers. Templates use CodeMirror snippet syntax:
 * `${name}` is a tab stop and `\t` is one indent unit. Tag templates start after `{% `.
 */
export interface LiquidTerm {
  name: string;
  detail: string;
  template?: string;
}

export type AssetSnippetKind = "public" | "redirect";

export function variableSnippet(path: string): string {
  return `{{ ${path} }}`;
}

type WidgetVariableField = Pick<WidgetField, "developerName" | "label" | "fieldType"> & {
  subFields: readonly WidgetVariableField[];
};

/** What every widget template can read besides its settings. Mirrors RenderEngine's widget object. */
const WIDGET_META_VARIABLES: TemplateVariable[] = [
  { path: "widget.id", description: "Unique id of this widget", example: null },
  { path: "widget.type", description: "Widget template developer name", example: null },
  { path: "widget.css_class", description: "CSS class from the widget's advanced settings", example: null },
  { path: "widget.html_id", description: "HTML id from the widget's advanced settings", example: null },
  { path: "widget.custom_attributes", description: "Raw HTML attributes, like data-theme=\"dark\"", example: null },
  { path: "widget.row", description: "Row in the section grid", example: null },
  { path: "widget.column", description: "Column in the section grid, 0 to 11", example: null },
  { path: "widget.column_span", description: "Columns the widget spans, 1 to 12", example: null },
];

/** Insert Variable entries for a widget template, built from its current (unsaved) fields. */
export function widgetTemplateVariables(fields: readonly WidgetVariableField[]): TemplateVariableGroup[] {
  const settings: TemplateVariable[] = fields
    .filter((field) => field.developerName.trim() !== "")
    .map((field) => {
      const path = `widget.settings.${field.developerName.trim()}`;
      if (field.fieldType !== "repeater") {
        return { path, description: field.label || null, example: null };
      }
      const cells = field.subFields
        .filter((sub) => sub.developerName.trim() !== "")
        .map((sub) => `{{ row.${sub.developerName.trim()} }}`)
        .join("");
      return {
        path,
        description: `${field.label || "Repeater"}. A list of rows; insert the loop from the example button.`,
        example: `{% for row in ${path} %}${cells || "{{ row }}"}{% endfor %}`,
      };
    });
  return [
    ...(settings.length > 0 ? [{ category: "Widget settings", variables: settings }] : []),
    { category: "Widget", variables: WIDGET_META_VARIABLES },
  ];
}

export function assetSnippet(objectKey: string, kind: AssetSnippetKind): string {
  return `{{ "${objectKey}" | attachment_${kind}_url }}`;
}

export const liquidTags: LiquidTerm[] = [
  { name: "if", detail: "Render when a condition is true", template: "if ${condition} %}\n\t${}\n{% endif %}" },
  { name: "unless", detail: "Render when a condition is false", template: "unless ${condition} %}\n\t${}\n{% endunless %}" },
  { name: "elsif", detail: "Another condition inside if", template: "elsif ${condition} %}" },
  { name: "else", detail: "Fallback branch", template: "else %}" },
  {
    name: "case",
    detail: "Branch on a value",
    template: "case ${variable} %}\n\t{% when ${value} %}\n\t\t${}\n{% endcase %}",
  },
  { name: "when", detail: "A branch inside case", template: "when ${value} %}" },
  { name: "for", detail: "Loop over a collection", template: "for ${item} in ${collection} %}\n\t${}\n{% endfor %}" },
  {
    name: "tablerow",
    detail: "Loop into table rows",
    template: "tablerow ${item} in ${collection} %}\n\t${}\n{% endtablerow %}",
  },
  { name: "break", detail: "Stop the loop", template: "break %}" },
  { name: "continue", detail: "Skip to the next iteration", template: "continue %}" },
  { name: "assign", detail: "Create a variable", template: "assign ${name} = ${value} %}" },
  { name: "capture", detail: "Capture output into a variable", template: "capture ${name} %}\n\t${}\n{% endcapture %}" },
  { name: "increment", detail: "Counter that counts up", template: "increment ${counter} %}" },
  { name: "decrement", detail: "Counter that counts down", template: "decrement ${counter} %}" },
  { name: "cycle", detail: "Alternate values each call", template: "cycle ${'odd'}, ${'even'} %}" },
  { name: "echo", detail: "Output a value", template: "echo ${value} %}" },
  { name: "liquid", detail: "Several tags in one block", template: "liquid\n\t${}\n%}" },
  { name: "raw", detail: "Output Liquid without running it", template: "raw %}${}{% endraw %}" },
  { name: "comment", detail: "Not rendered", template: "comment %}\n\t${}\n{% endcomment %}" },
  { name: "renderbody", detail: "Raytha. Where child templates render in a base layout", template: "renderbody %}" },
  ...["endif", "endunless", "endcase", "endfor", "endtablerow", "endcapture", "endraw", "endcomment"].map((name) => ({
    name,
    detail: "Close the block",
    template: `${name} %}`,
  })),
];

export const raythaFilters: LiquidTerm[] = [
  {
    name: "attachment_public_url",
    detail: "Direct URL of a media object key: root-relative for local storage, absolute for cloud storage",
  },
  {
    name: "attachment_redirect_url",
    detail: "Stable root-relative URL of a media object key that redirects to the file",
  },
  { name: "attachment_url", detail: "Alias of attachment_redirect_url; always root-relative" },
  {
    name: "organization_time",
    detail: "Convert to the organization time zone and format",
    template: 'organization_time: "${%b %d, %Y}"',
  },
  { name: "groupby", detail: "Group items by a property into key and items", template: 'groupby: "${property}"' },
  { name: "json", detail: "Serialize the value as JSON" },
];

export const liquidFilters: LiquidTerm[] = (
  "abs append at_least at_most capitalize ceil compact concat date default divided_by downcase escape " +
  "escape_once first floor join last lstrip map minus modulo newline_to_br plus prepend remove remove_first " +
  "replace replace_first reverse round rstrip size slice sort sort_natural split strip strip_html " +
  "strip_newlines sum times truncate truncatewords uniq upcase url_decode url_encode where"
)
  .split(" ")
  .map((name) => ({ name, detail: "Liquid" }));

export const raythaFunctions: LiquidTerm[] = [
  {
    name: "get_content_items",
    detail: "Query content items of a type",
    template: 'get_content_items(ContentType: "${content_type}", Filter: "", OrderBy: "", PageNumber: 1, PageSize: ${25})',
  },
  { name: "get_content_item_by_id", detail: "Load one content item", template: 'get_content_item_by_id("${id}")' },
  {
    name: "get_content_type_by_developer_name",
    detail: "Load a content type",
    template: 'get_content_type_by_developer_name("${developer_name}")',
  },
  { name: "get_main_menu", detail: "The main navigation menu", template: "get_main_menu()" },
  { name: "get_menu", detail: "A navigation menu by developer name", template: 'get_menu("${developer_name}")' },
  {
    name: "raytha_function",
    detail: "Call a Liquid-triggered Raytha Function",
    template: 'raytha_function("${developer_name}", "${method}")',
  },
  { name: "render_section", detail: "Site pages. Render a widget section", template: 'render_section("${main}")' },
  { name: "get_section", detail: "Site pages. Widgets of a section as data", template: 'get_section("${main}")' },
];

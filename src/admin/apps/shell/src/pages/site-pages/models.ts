import type { EntityRef } from "@raytha/api";
import { entityFields, isRecord, readBoolean, readString } from "../entity";

export type WidgetKind =
  | "hero"
  | "wysiwyg"
  | "imagetext"
  | "card"
  | "faq"
  | "cta"
  | "embed"
  | "contentlist"
  | "unknown";

export type FaqItem = {
  question: string;
  answer: string;
};

export type HeroSettings = {
  kind: "hero";
  headline: string;
  subheadline: string;
  backgroundImage: string;
  backgroundColor: string;
  textColor: string;
  buttonText: string;
  buttonUrl: string;
  buttonStyle: string;
  alignment: string;
  minHeight: number;
};

export type WysiwygSettings = {
  kind: "wysiwyg";
  content: string;
  backgroundColor: string;
  padding: string;
};

export type ImageTextSettings = {
  kind: "imagetext";
  imageUrl: string;
  imageAlt: string;
  headline: string;
  content: string;
  imagePosition: string;
  buttonText: string;
  buttonUrl: string;
  buttonStyle: string;
  backgroundColor: string;
};

export type CardSettings = {
  kind: "card";
  title: string;
  description: string;
  imageUrl: string;
  imageAlt: string;
  buttonText: string;
  buttonUrl: string;
  buttonStyle: string;
  backgroundColor: string;
};

export type FaqSettings = {
  kind: "faq";
  headline: string;
  subheadline: string;
  items: FaqItem[];
  backgroundColor: string;
  expandFirst: boolean;
};

export type CtaSettings = {
  kind: "cta";
  headline: string;
  content: string;
  buttonText: string;
  buttonUrl: string;
  buttonStyle: string;
  backgroundColor: string;
  textColor: string;
  alignment: string;
};

export type EmbedSettings = {
  kind: "embed";
  embedType: string;
  iframeUrl: string;
  htmlContent: string;
  aspectRatio: string;
  maxWidth: number | null;
  caption: string;
  backgroundColor: string;
};

export type ContentListSettings = {
  kind: "contentlist";
  headline: string;
  subheadline: string;
  contentType: string;
  viewId: string;
  filter: string;
  orderBy: string;
  pageSize: number;
  displayStyle: string;
  showImage: boolean;
  showDate: boolean;
  showExcerpt: boolean;
  linkText: string;
  linkUrl: string;
  backgroundColor: string;
};

export type UnknownSettings = {
  kind: "unknown";
  rawJson: string;
};

export type WidgetSettings =
  | HeroSettings
  | WysiwygSettings
  | ImageTextSettings
  | CardSettings
  | FaqSettings
  | CtaSettings
  | EmbedSettings
  | ContentListSettings
  | UnknownSettings;

export type SitePageWidget = {
  clientKey: string;
  id: string;
  widgetType: string;
  settings: WidgetSettings;
  row: number;
  column: number;
  columnSpan: number;
  cssClass: string;
  htmlId: string;
  customAttributes: string;
};

export type SitePageSection = {
  name: string;
  widgets: SitePageWidget[];
};

export type SitePageDetail = {
  id: string;
  title: string;
  routePath: string;
  isPublished: boolean;
  isDraft: boolean;
  webTemplateId: string;
  templateContent: string;
  widgets: SitePageSection[];
};

export function defaultHero(): HeroSettings {
  return {
    kind: "hero",
    headline: "",
    subheadline: "",
    backgroundImage: "",
    backgroundColor: "#1e293b",
    textColor: "#ffffff",
    buttonText: "",
    buttonUrl: "",
    buttonStyle: "light",
    alignment: "center",
    minHeight: 400,
  };
}

export function defaultWysiwyg(): WysiwygSettings {
  return {
    kind: "wysiwyg",
    content: "",
    backgroundColor: "",
    padding: "medium",
  };
}

export function defaultImageText(): ImageTextSettings {
  return {
    kind: "imagetext",
    imageUrl: "",
    imageAlt: "",
    headline: "",
    content: "",
    imagePosition: "left",
    buttonText: "",
    buttonUrl: "",
    buttonStyle: "primary",
    backgroundColor: "",
  };
}

export function defaultCard(): CardSettings {
  return {
    kind: "card",
    title: "",
    description: "",
    imageUrl: "",
    imageAlt: "",
    buttonText: "",
    buttonUrl: "",
    buttonStyle: "primary",
    backgroundColor: "",
  };
}

export function defaultFaq(): FaqSettings {
  return {
    kind: "faq",
    headline: "",
    subheadline: "",
    items: [],
    backgroundColor: "",
    expandFirst: true,
  };
}

export function defaultCta(): CtaSettings {
  return {
    kind: "cta",
    headline: "",
    content: "",
    buttonText: "",
    buttonUrl: "",
    buttonStyle: "light",
    backgroundColor: "#0d6efd",
    textColor: "#ffffff",
    alignment: "center",
  };
}

export function defaultEmbed(): EmbedSettings {
  return {
    kind: "embed",
    embedType: "iframe",
    iframeUrl: "",
    htmlContent: "",
    aspectRatio: "16x9",
    maxWidth: null,
    caption: "",
    backgroundColor: "",
  };
}

export function defaultContentList(): ContentListSettings {
  return {
    kind: "contentlist",
    headline: "",
    subheadline: "",
    contentType: "",
    viewId: "",
    filter: "IsPublished eq 'true'",
    orderBy: "",
    pageSize: 3,
    displayStyle: "cards",
    showImage: true,
    showDate: true,
    showExcerpt: true,
    linkText: "",
    linkUrl: "",
    backgroundColor: "",
  };
}

export function defaultSettingsForType(widgetType: string): WidgetSettings {
  switch (widgetType) {
    case "hero":
      return defaultHero();
    case "wysiwyg":
      return defaultWysiwyg();
    case "imagetext":
      return defaultImageText();
    case "card":
      return defaultCard();
    case "faq":
      return defaultFaq();
    case "cta":
      return defaultCta();
    case "embed":
      return defaultEmbed();
    case "contentlist":
      return defaultContentList();
    default:
      return { kind: "unknown", rawJson: "{}" };
  }
}

export function newWidget(widgetType: string, row: number): SitePageWidget {
  return {
    clientKey: newClientKey(),
    id: "",
    widgetType,
    settings: defaultSettingsForType(widgetType),
    row,
    column: 0,
    columnSpan: 12,
    cssClass: "",
    htmlId: "",
    customAttributes: "",
  };
}

export function parseSitePage(entity: EntityRef): SitePageDetail {
  const fields = entityFields(entity);
  const template = fields.webTemplate;
  const templateContent = isRecord(template) ? readString(template, "content") : "";
  const stored = parseSections(fields.widgets);
  return {
    id: entity.id,
    title: readString(fields, "title"),
    routePath: readString(fields, "routePath"),
    isPublished: readBoolean(fields, "isPublished"),
    isDraft: readBoolean(fields, "isDraft"),
    webTemplateId: readString(fields, "webTemplateId"),
    templateContent,
    widgets: mergeTemplateSections(stored, sectionNamesFromTemplate(templateContent)),
  };
}

export function parseSections(value: unknown): SitePageSection[] {
  if (!isRecord(value)) {
    return [];
  }
  const sections: SitePageSection[] = [];
  for (const [name, widgets] of Object.entries(value)) {
    if (!Array.isArray(widgets)) {
      continue;
    }
    sections.push({
      name,
      widgets: widgets
        .map(parseWidget)
        .sort((left, right) => left.row - right.row || left.column - right.column),
    });
  }
  return sections;
}

export function sectionNamesFromTemplate(content: string): string[] {
  const names: string[] = [];
  const pattern = /(?:render_section|get_section)\(\s*["']([^"']+)["']/g;
  let match = pattern.exec(content);
  while (match) {
    const name = match[1];
    if (name && !names.includes(name)) {
      names.push(name);
    }
    match = pattern.exec(content);
  }
  return names;
}

export function mergeTemplateSections(
  stored: SitePageSection[],
  templateNames: string[],
): SitePageSection[] {
  const byName = new Map(stored.map((section) => [section.name, section]));
  const merged: SitePageSection[] = [];
  for (const name of templateNames) {
    const existing = byName.get(name);
    if (existing) {
      merged.push(existing);
      byName.delete(name);
    } else {
      merged.push({ name, widgets: [] });
    }
  }
  for (const leftover of byName.values()) {
    merged.push(leftover);
  }
  return merged;
}

export function parseWidget(value: unknown): SitePageWidget {
  const record = isRecord(value) ? value : {};
  const id = readString(record, "id");
  const widgetType = readString(record, "widgetType");
  const columnSpan = readNumber(record, "columnSpan", 12);
  return {
    clientKey: id || newClientKey(),
    id,
    widgetType,
    settings: parseSettings(widgetType, record.settingsJson),
    row: readNumber(record, "row", 0),
    column: clamp(readNumber(record, "column", 0), 0, 11),
    columnSpan: clamp(columnSpan, 1, 12),
    cssClass: readString(record, "cssClass"),
    htmlId: readString(record, "htmlId"),
    customAttributes: readString(record, "customAttributes"),
  };
}

export function parseSettings(widgetType: string, raw: unknown): WidgetSettings {
  const parsed = parseSettingsRecord(raw);
  switch (widgetType) {
    case "hero":
      return parseHero(parsed);
    case "wysiwyg":
      return parseWysiwyg(parsed);
    case "imagetext":
      return parseImageText(parsed);
    case "card":
      return parseCard(parsed);
    case "faq":
      return parseFaq(parsed);
    case "cta":
      return parseCta(parsed);
    case "embed":
      return parseEmbed(parsed);
    case "contentlist":
      return parseContentList(parsed);
    default:
      return { kind: "unknown", rawJson: rawJsonString(raw) };
  }
}

export function settingsJson(settings: WidgetSettings): string {
  switch (settings.kind) {
    case "hero":
      return JSON.stringify(omitEmpty({
        headline: settings.headline,
        subheadline: settings.subheadline,
        backgroundImage: settings.backgroundImage,
        backgroundColor: settings.backgroundColor,
        textColor: settings.textColor,
        buttonText: settings.buttonText,
        buttonUrl: settings.buttonUrl,
        buttonStyle: settings.buttonStyle,
        alignment: settings.alignment,
        minHeight: settings.minHeight,
      }));
    case "wysiwyg":
      return JSON.stringify(omitEmpty({
        content: settings.content,
        backgroundColor: settings.backgroundColor,
        padding: settings.padding,
      }));
    case "imagetext":
      return JSON.stringify(omitEmpty({
        imageUrl: settings.imageUrl,
        imageAlt: settings.imageAlt,
        headline: settings.headline,
        content: settings.content,
        imagePosition: settings.imagePosition,
        buttonText: settings.buttonText,
        buttonUrl: settings.buttonUrl,
        buttonStyle: settings.buttonStyle,
        backgroundColor: settings.backgroundColor,
      }));
    case "card":
      return JSON.stringify(omitEmpty({
        title: settings.title,
        description: settings.description,
        imageUrl: settings.imageUrl,
        imageAlt: settings.imageAlt,
        buttonText: settings.buttonText,
        buttonUrl: settings.buttonUrl,
        buttonStyle: settings.buttonStyle,
        backgroundColor: settings.backgroundColor,
      }));
    case "faq":
      return JSON.stringify(omitEmpty({
        headline: settings.headline,
        subheadline: settings.subheadline,
        items: settings.items.map((item) => ({ question: item.question, answer: item.answer })),
        backgroundColor: settings.backgroundColor,
        expandFirst: settings.expandFirst,
      }));
    case "cta":
      return JSON.stringify(omitEmpty({
        headline: settings.headline,
        content: settings.content,
        buttonText: settings.buttonText,
        buttonUrl: settings.buttonUrl,
        buttonStyle: settings.buttonStyle,
        backgroundColor: settings.backgroundColor,
        textColor: settings.textColor,
        alignment: settings.alignment,
      }));
    case "embed":
      return JSON.stringify(omitEmpty({
        embedType: settings.embedType,
        iframeUrl: settings.iframeUrl,
        htmlContent: settings.htmlContent,
        aspectRatio: settings.aspectRatio,
        maxWidth: settings.maxWidth,
        caption: settings.caption,
        backgroundColor: settings.backgroundColor,
      }));
    case "contentlist":
      return JSON.stringify(omitEmpty({
        headline: settings.headline,
        subheadline: settings.subheadline,
        contentType: settings.contentType,
        viewId: settings.viewId,
        filter: settings.filter,
        orderBy: settings.orderBy,
        pageSize: settings.pageSize,
        displayStyle: settings.displayStyle,
        showImage: settings.showImage,
        showDate: settings.showDate,
        showExcerpt: settings.showExcerpt,
        linkText: settings.linkText,
        linkUrl: settings.linkUrl,
        backgroundColor: settings.backgroundColor,
      }));
    case "unknown":
      return settings.rawJson;
    default: {
      const _exhaustive: never = settings;
      return _exhaustive;
    }
  }
}

export function widgetSummary(widget: SitePageWidget): string {
  const settings = widget.settings;
  switch (settings.kind) {
    case "hero":
      return settings.headline || "No headline set";
    case "wysiwyg":
      return stripTags(settings.content) || "No content";
    case "imagetext":
      return settings.headline || "No headline set";
    case "card":
      return settings.title || "No title set";
    case "faq":
      return settings.headline || `${settings.items.length} questions`;
    case "cta":
      return settings.headline || "No headline set";
    case "embed":
      return settings.iframeUrl || settings.caption || settings.embedType;
    case "contentlist":
      return settings.contentType
        ? `${settings.contentType} (${settings.displayStyle}, ${settings.pageSize} items)`
        : "No content type selected";
    case "unknown":
      return widget.widgetType || "Unknown widget";
    default: {
      const _exhaustive: never = settings;
      return _exhaustive;
    }
  }
}

function parseHero(record: Record<string, unknown>): HeroSettings {
  const defaults = defaultHero();
  return {
    kind: "hero",
    headline: readString(record, "headline"),
    subheadline: readString(record, "subheadline"),
    backgroundImage: readString(record, "backgroundImage"),
    backgroundColor: readString(record, "backgroundColor") || defaults.backgroundColor,
    textColor: readString(record, "textColor") || defaults.textColor,
    buttonText: readString(record, "buttonText"),
    buttonUrl: readString(record, "buttonUrl"),
    buttonStyle: readString(record, "buttonStyle") || defaults.buttonStyle,
    alignment: readString(record, "alignment") || defaults.alignment,
    minHeight: readNumber(record, "minHeight", defaults.minHeight),
  };
}

function parseWysiwyg(record: Record<string, unknown>): WysiwygSettings {
  const defaults = defaultWysiwyg();
  return {
    kind: "wysiwyg",
    content: readString(record, "content"),
    backgroundColor: readString(record, "backgroundColor"),
    padding: readString(record, "padding") || defaults.padding,
  };
}

function parseImageText(record: Record<string, unknown>): ImageTextSettings {
  const defaults = defaultImageText();
  return {
    kind: "imagetext",
    imageUrl: readString(record, "imageUrl"),
    imageAlt: readString(record, "imageAlt"),
    headline: readString(record, "headline"),
    content: readString(record, "content"),
    imagePosition: readString(record, "imagePosition") || defaults.imagePosition,
    buttonText: readString(record, "buttonText"),
    buttonUrl: readString(record, "buttonUrl"),
    buttonStyle: readString(record, "buttonStyle") || defaults.buttonStyle,
    backgroundColor: readString(record, "backgroundColor"),
  };
}

function parseCard(record: Record<string, unknown>): CardSettings {
  const defaults = defaultCard();
  return {
    kind: "card",
    title: readString(record, "title"),
    description: readString(record, "description"),
    imageUrl: readString(record, "imageUrl"),
    imageAlt: readString(record, "imageAlt"),
    buttonText: readString(record, "buttonText"),
    buttonUrl: readString(record, "buttonUrl"),
    buttonStyle: readString(record, "buttonStyle") || defaults.buttonStyle,
    backgroundColor: readString(record, "backgroundColor"),
  };
}

function parseFaq(record: Record<string, unknown>): FaqSettings {
  return {
    kind: "faq",
    headline: readString(record, "headline"),
    subheadline: readString(record, "subheadline"),
    items: parseFaqItems(record.items),
    backgroundColor: readString(record, "backgroundColor"),
    expandFirst: readBooleanField(record, "expandFirst", true),
  };
}

function parseCta(record: Record<string, unknown>): CtaSettings {
  const defaults = defaultCta();
  return {
    kind: "cta",
    headline: readString(record, "headline"),
    content: readString(record, "content"),
    buttonText: readString(record, "buttonText"),
    buttonUrl: readString(record, "buttonUrl"),
    buttonStyle: readString(record, "buttonStyle") || defaults.buttonStyle,
    backgroundColor: readString(record, "backgroundColor") || defaults.backgroundColor,
    textColor: readString(record, "textColor") || defaults.textColor,
    alignment: readString(record, "alignment") || defaults.alignment,
  };
}

function parseEmbed(record: Record<string, unknown>): EmbedSettings {
  const defaults = defaultEmbed();
  return {
    kind: "embed",
    embedType: readString(record, "embedType") || defaults.embedType,
    iframeUrl: readString(record, "iframeUrl"),
    htmlContent: readString(record, "htmlContent"),
    aspectRatio: readString(record, "aspectRatio") || defaults.aspectRatio,
    maxWidth: readOptionalNumber(record, "maxWidth"),
    caption: readString(record, "caption"),
    backgroundColor: readString(record, "backgroundColor"),
  };
}

function parseContentList(record: Record<string, unknown>): ContentListSettings {
  const defaults = defaultContentList();
  return {
    kind: "contentlist",
    headline: readString(record, "headline"),
    subheadline: readString(record, "subheadline"),
    contentType: readString(record, "contentType"),
    viewId: readString(record, "viewId"),
    filter: readString(record, "filter") || defaults.filter,
    orderBy: readString(record, "orderBy"),
    pageSize: readNumber(record, "pageSize", defaults.pageSize),
    displayStyle: readString(record, "displayStyle") || defaults.displayStyle,
    showImage: readBooleanField(record, "showImage", true),
    showDate: readBooleanField(record, "showDate", true),
    showExcerpt: readBooleanField(record, "showExcerpt", true),
    linkText: readString(record, "linkText"),
    linkUrl: readString(record, "linkUrl"),
    backgroundColor: readString(record, "backgroundColor"),
  };
}

function parseFaqItems(value: unknown): FaqItem[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const items: FaqItem[] = [];
  for (const item of value) {
    if (!isRecord(item)) {
      continue;
    }
    items.push({
      question: readString(item, "question"),
      answer: readString(item, "answer"),
    });
  }
  return items;
}

function parseSettingsRecord(raw: unknown): Record<string, unknown> {
  if (isRecord(raw)) {
    return raw;
  }
  if (typeof raw !== "string" || raw.trim() === "") {
    return {};
  }
  try {
    const parsed: unknown = JSON.parse(raw);
    return isRecord(parsed) ? parsed : {};
  } catch {
    return {};
  }
}

function rawJsonString(raw: unknown): string {
  if (typeof raw === "string" && raw.trim() !== "") {
    return raw;
  }
  if (isRecord(raw) || Array.isArray(raw)) {
    return JSON.stringify(raw);
  }
  return "{}";
}

type JsonValue = string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue };

function omitEmpty(input: Record<string, JsonValue>): Record<string, JsonValue> {
  const output: Record<string, JsonValue> = {};
  for (const [key, value] of Object.entries(input)) {
    if (value === "" || value === null || value === undefined) {
      continue;
    }
    output[key] = value;
  }
  return output;
}

function readNumber(record: Record<string, unknown>, key: string, fallback: number): number {
  const value = record[key];
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === "string" && value.trim() !== "") {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }
  return fallback;
}

function readOptionalNumber(record: Record<string, unknown>, key: string): number | null {
  const value = record[key];
  if (value === null || value === undefined || value === "") {
    return null;
  }
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === "string") {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }
  return null;
}

function readBooleanField(record: Record<string, unknown>, key: string, fallback: boolean): boolean {
  const value = record[key];
  if (typeof value === "boolean") {
    return value;
  }
  return fallback;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function stripTags(html: string): string {
  return html.replace(/<[^>]*>/g, "").trim();
}

function newClientKey(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  return `widget-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

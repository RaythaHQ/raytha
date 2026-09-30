import type { EntityRef, WidgetField } from "@raytha/api";
import { clearDependentViews } from "../content/definition-field-control";
import type { DefinitionValue } from "../content/fields-model";
import { entityFields, isRecord, readBoolean, readString } from "../entity";

/**
 * A widget's SettingsJson and its parsed object. Only the functions below build one, so `json` stays
 * byte-for-byte what the server sent until a value changes. `values` is null when `json` is not an object.
 */
export type WidgetSettings = {
  readonly json: string;
  readonly values: Readonly<Record<string, unknown>> | null;
};

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

/** `orphaned`: saved widgets under a name the template no longer renders, so they never reach the public page. */
export type SitePageSection = {
  name: string;
  kind: "template" | "orphaned";
  widgets: SitePageWidget[];
};

export type SitePageStatus = "draft" | "published" | "published-with-changes";

export type AuditStamp = {
  at: string;
  by: string;
};

export type SitePageDetail = {
  id: string;
  title: string;
  routePath: string;
  status: SitePageStatus;
  isPublished: boolean;
  isDraft: boolean;
  webTemplateId: string;
  templateLabel: string;
  created: AuditStamp;
  modified: AuditStamp;
  widgets: SitePageSection[];
};

export function settingsFromJson(raw: unknown): WidgetSettings {
  if (isRecord(raw)) {
    return { json: JSON.stringify(raw), values: raw };
  }
  const json = typeof raw === "string" ? raw : "";
  if (json.trim() === "") {
    return { json, values: {} };
  }
  try {
    const parsed: unknown = JSON.parse(json);
    return { json, values: isRecord(parsed) ? parsed : null };
  } catch {
    return { json, values: null };
  }
}

function settingsFromValues(values: Record<string, unknown>): WidgetSettings {
  return { json: JSON.stringify(values), values };
}

/** The settings a newly added widget starts with: each field's default, and nothing for fields without one. */
export function defaultSettings(fields: readonly WidgetField[]): WidgetSettings {
  const values: Record<string, unknown> = {};
  for (const field of fields) {
    if (field.defaultValue !== null) {
      values[field.developerName] = field.defaultValue;
    }
  }
  return settingsFromValues(values);
}

/**
 * Sets one field and leaves every other key, known or not, where it was. Clearing a field removes its
 * key, as the pre-field admin did; a content_type change also clears the view fields that follow it.
 */
export function withSetting(
  settings: WidgetSettings,
  fields: readonly WidgetField[],
  developerName: string,
  next: DefinitionValue,
): WidgetSettings {
  const values: Record<string, unknown> = { ...settings.values };
  if (next === null || next === "") {
    delete values[developerName];
  } else {
    values[developerName] = next;
  }
  return settingsFromValues(clearDependentViews(values, fields, developerName));
}

export function settingsJson(settings: WidgetSettings): string {
  return settings.json;
}

export function newWidget(widgetType: string, fields: readonly WidgetField[], row: number): SitePageWidget {
  return {
    clientKey: newClientKey(),
    id: "",
    widgetType,
    settings: defaultSettings(fields),
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
  const template = isRecord(fields.webTemplate) ? fields.webTemplate : {};
  const isPublished = readBoolean(fields, "isPublished");
  const isDraft = readBoolean(fields, "isDraft");
  return {
    id: entity.id,
    title: readString(fields, "title"),
    routePath: readString(fields, "routePath"),
    status: sitePageStatus(isPublished, isDraft),
    isPublished,
    isDraft,
    webTemplateId: readString(fields, "webTemplateId"),
    templateLabel: readString(template, "label", "developerName"),
    created: auditStamp(fields.creationTime, fields.creatorUser),
    modified: auditStamp(fields.lastModificationTime ?? fields.creationTime, fields.lastModifierUser ?? fields.creatorUser),
    widgets: layoutSections(parseStoredWidgets(fields.widgets), readStringList(fields.templateSections)),
  };
}

export function sitePageStatus(isPublished: boolean, isDraft: boolean): SitePageStatus {
  if (!isPublished) {
    return "draft";
  }
  return isDraft ? "published-with-changes" : "published";
}

/**
 * Template sections come first in template order, always present so an empty one can take widgets.
 * A stored key the template does not render is kept only while it still holds widgets; the match is
 * exact because the public renderer looks sections up by exact key.
 */
export function layoutSections(stored: Map<string, SitePageWidget[]>, templateNames: string[]): SitePageSection[] {
  const sections: SitePageSection[] = templateNames.map((name) => ({
    name,
    kind: "template",
    widgets: stored.get(name) ?? [],
  }));
  for (const [name, widgets] of stored) {
    if (!templateNames.includes(name) && widgets.length > 0) {
      sections.push({ name, kind: "orphaned", widgets });
    }
  }
  return sections;
}

function parseStoredWidgets(value: unknown): Map<string, SitePageWidget[]> {
  const stored = new Map<string, SitePageWidget[]>();
  if (!isRecord(value)) {
    return stored;
  }
  for (const [name, widgets] of Object.entries(value)) {
    if (Array.isArray(widgets)) {
      stored.set(
        name,
        widgets.map(parseWidget).sort((left, right) => left.row - right.row || left.column - right.column),
      );
    }
  }
  return stored;
}

function auditStamp(at: unknown, user: unknown): AuditStamp {
  return {
    at: typeof at === "string" ? at : "",
    by: isRecord(user) ? readString(user, "fullName", "emailAddress") : "",
  };
}

function readStringList(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
}

export function parseWidget(value: unknown): SitePageWidget {
  const record = isRecord(value) ? value : {};
  const id = readString(record, "id");
  const columnSpan = readNumber(record, "columnSpan", 12);
  return {
    clientKey: id || newClientKey(),
    id,
    widgetType: readString(record, "widgetType"),
    settings: settingsFromJson(record.settingsJson),
    row: readNumber(record, "row", 0),
    column: clamp(readNumber(record, "column", 0), 0, 11),
    columnSpan: clamp(columnSpan, 1, 12),
    cssClass: readString(record, "cssClass"),
    htmlId: readString(record, "htmlId"),
    customAttributes: readString(record, "customAttributes"),
  };
}

const SUMMARY_KEYS = ["headline", "title", "content", "caption", "iframeUrl", "contentType"];

export function widgetSummary(widget: SitePageWidget): string {
  const values = widget.settings.values ?? {};
  const texts = [...SUMMARY_KEYS.map((key) => values[key]), ...Object.values(values)]
    .filter((value): value is string => typeof value === "string")
    .map(stripTags)
    .filter((text) => text.length > 0 && !text.startsWith("#") && !text.startsWith("/"));
  return texts[0] ?? "No settings yet";
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

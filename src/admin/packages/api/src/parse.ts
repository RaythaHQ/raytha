import type {
  BackgroundTaskDetail,
  BackgroundTaskStatusName,
  ClearedLog,
  EmailTemplateDetail,
  FunctionDetail,
  IdResponse,
  MaintenanceSnapshot,
  MenuDetail,
  MenuItemDetail,
  PagedResult,
  RetainedLogKey,
  RetainedLogStats,
  SizeUsage,
  TaskMediaItem,
  TemplateRevision,
  TemplateVariable,
  TemplateVariableGroup,
  ThemeMediaItem,
  JsonValue,
  SitePageWidgetDefinition,
  WebTemplateDetail,
  WidgetField,
  WidgetFieldChoice,
  WidgetFieldTypeName,
  WidgetFieldTypeOption,
  WidgetTemplateDetail,
} from "./types";
import { WIDGET_FIELD_TYPES } from "./types";

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function stringField(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string") {
      return value;
    }
  }
  return "";
}

function booleanField(record: Record<string, unknown>, key: string): boolean {
  const value = record[key];
  return value === true;
}

function numberField(record: Record<string, unknown>, key: string, fallback: number): number {
  const value = record[key];
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function optionalId(value: unknown): string | null {
  if (typeof value === "string" && value.length > 0) {
    return value;
  }
  if (isRecord(value)) {
    const id = value.id;
    if (typeof id === "string" && id.length > 0) {
      return id;
    }
  }
  return null;
}

function creatorName(value: unknown): string {
  if (!isRecord(value)) {
    return "";
  }
  return stringField(value, "fullName", "emailAddress") || `${stringField(value, "firstName")} ${stringField(value, "lastName")}`.trim();
}

function optionalText(record: Record<string, unknown>, key: string): string | null {
  const value = record[key];
  return typeof value === "string" && value.length > 0 ? value : null;
}

function availableVariables(value: unknown): TemplateVariableGroup[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const groups: TemplateVariableGroup[] = [];
  for (const group of value) {
    if (!isRecord(group) || !Array.isArray(group.variables)) {
      continue;
    }
    const variables: TemplateVariable[] = [];
    for (const variable of group.variables) {
      if (!isRecord(variable)) {
        continue;
      }
      const path = stringField(variable, "path");
      if (path.length > 0) {
        variables.push({
          path,
          description: optionalText(variable, "description"),
          example: optionalText(variable, "example"),
        });
      }
    }
    const category = stringField(group, "category");
    if (category.length > 0 && variables.length > 0) {
      groups.push({ category, variables });
    }
  }
  return groups;
}

function accessIds(value: unknown): string[] {
  if (Array.isArray(value)) {
    const ids: string[] = [];
    for (const item of value) {
      const id = optionalId(item);
      if (id) {
        ids.push(id);
      }
    }
    return ids;
  }
  if (isRecord(value)) {
    return Object.keys(value).filter((key) => key.length > 0);
  }
  return [];
}

function triggerType(value: unknown): { developerName: string; label: string } {
  if (typeof value === "string") {
    return { developerName: value, label: value };
  }
  if (isRecord(value)) {
    const developerName = stringField(value, "developerName");
    const label = stringField(value, "label") || developerName;
    return { developerName, label };
  }
  return { developerName: "", label: "" };
}

export function parseIdResponse(value: unknown): IdResponse {
  if (!isRecord(value)) {
    throw new Error("The server did not return an id.");
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    throw new Error("The server did not return an id.");
  }
  return { id };
}

export function parsePaged<T>(value: unknown, parseItem: (item: unknown) => T | null): PagedResult<T> {
  if (!isRecord(value)) {
    return { items: [], totalCount: 0, pageNumber: 1, pageSize: 50 };
  }
  const rawItems = value.items;
  const items: T[] = [];
  if (Array.isArray(rawItems)) {
    for (const item of rawItems) {
      const parsed = parseItem(item);
      if (parsed !== null) {
        items.push(parsed);
      }
    }
  }
  return {
    items,
    totalCount: numberField(value, "totalCount", items.length),
    pageNumber: numberField(value, "pageNumber", 1),
    pageSize: numberField(value, "pageSize", 50),
  };
}

export function parseEmailTemplate(value: unknown): EmailTemplateDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    subject: stringField(value, "subject"),
    developerName: stringField(value, "developerName"),
    content: stringField(value, "content"),
    cc: stringField(value, "cc"),
    bcc: stringField(value, "bcc"),
    availableVariables: availableVariables(value.availableVariables),
  };
}

export function parseWebTemplate(value: unknown): WebTemplateDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    themeId: stringField(value, "themeId"),
    label: stringField(value, "label"),
    developerName: stringField(value, "developerName"),
    content: stringField(value, "content"),
    isBaseLayout: booleanField(value, "isBaseLayout"),
    isBuiltInTemplate: booleanField(value, "isBuiltInTemplate"),
    parentTemplateId: optionalId(value.parentTemplateId ?? value.parentTemplate),
    allowAccessForNewContentTypes: booleanField(value, "allowAccessForNewContentTypes"),
    templateAccessToModelDefinitions: accessIds(value.templateAccessToModelDefinitions),
    availableVariables: availableVariables(value.availableVariables),
    isFavorite: booleanField(value, "isFavorite"),
  };
}

export function parseWidgetTemplate(value: unknown): WidgetTemplateDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    themeId: stringField(value, "themeId"),
    label: stringField(value, "label"),
    developerName: stringField(value, "developerName"),
    content: stringField(value, "content"),
    isBuiltInTemplate: booleanField(value, "isBuiltInTemplate"),
    fields: parseWidgetFields(value.fields),
  };
}

function widgetFieldTypeName(value: unknown): WidgetFieldTypeName | null {
  return WIDGET_FIELD_TYPES.find((name) => name === value) ?? null;
}

function jsonValue(value: unknown): JsonValue {
  if (value === null || typeof value === "string" || typeof value === "boolean") {
    return value;
  }
  if (typeof value === "number") {
    return Number.isFinite(value) ? value : null;
  }
  if (Array.isArray(value)) {
    return value.map(jsonValue);
  }
  if (isRecord(value)) {
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, jsonValue(item)]));
  }
  return null;
}

function widgetFieldChoices(value: unknown): WidgetFieldChoice[] {
  const choices: WidgetFieldChoice[] = [];
  for (const item of Array.isArray(value) ? value : []) {
    if (isRecord(item)) {
      choices.push({
        label: stringField(item, "label"),
        developerName: stringField(item, "developerName"),
        disabled: booleanField(item, "disabled"),
      });
    }
  }
  return choices;
}

/** Fields in display order. A field whose type this admin does not know is dropped. */
export function parseWidgetFields(value: unknown): WidgetField[] {
  const fields: WidgetField[] = [];
  for (const item of Array.isArray(value) ? value : []) {
    if (!isRecord(item)) {
      continue;
    }
    const fieldType = widgetFieldTypeName(item.fieldType);
    if (fieldType === null) {
      continue;
    }
    fields.push({
      developerName: stringField(item, "developerName"),
      label: stringField(item, "label"),
      fieldType,
      description: stringField(item, "description"),
      isRequired: booleanField(item, "isRequired"),
      defaultValue: jsonValue(item.defaultValue ?? null),
      choices: widgetFieldChoices(item.choices),
      subFields: parseWidgetFields(item.subFields),
      contentTypeField: stringField(item, "contentTypeField"),
    });
  }
  return fields;
}

export function parseWidgetFieldTypeOptions(value: unknown): WidgetFieldTypeOption[] {
  const options: WidgetFieldTypeOption[] = [];
  for (const item of Array.isArray(value) ? value : []) {
    const developerName = isRecord(item) ? widgetFieldTypeName(item.developerName) : null;
    if (!isRecord(item) || developerName === null) {
      continue;
    }
    options.push({
      developerName,
      label: stringField(item, "label") || developerName,
      hasChoices: booleanField(item, "hasChoices"),
      allowedInRepeater: booleanField(item, "allowedInRepeater"),
    });
  }
  return options;
}

export function parseWidgetDefinitions(value: unknown): SitePageWidgetDefinition[] {
  const definitions: SitePageWidgetDefinition[] = [];
  for (const item of Array.isArray(value) ? value : []) {
    const developerName = isRecord(item) ? stringField(item, "developerName") : "";
    if (!isRecord(item) || developerName.length === 0) {
      continue;
    }
    definitions.push({
      developerName,
      displayName: stringField(item, "displayName") || developerName,
      description: stringField(item, "description"),
      iconClass: stringField(item, "iconClass"),
      isBuiltInTemplate: booleanField(item, "isBuiltInTemplate"),
      fields: parseWidgetFields(item.fields),
    });
  }
  return definitions;
}

export function parseFunction(value: unknown): FunctionDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  const trigger = triggerType(value.triggerType);
  return {
    id,
    name: stringField(value, "name"),
    developerName: stringField(value, "developerName"),
    triggerType: trigger.developerName,
    triggerLabel: trigger.label,
    isActive: booleanField(value, "isActive"),
    code: stringField(value, "code"),
    routePath: stringField(value, "routePath"),
  };
}

export function parseMenu(value: unknown): MenuDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    label: stringField(value, "label"),
    developerName: stringField(value, "developerName"),
    isMainMenu: booleanField(value, "isMainMenu"),
  };
}

export function parseMenuItem(value: unknown): MenuItemDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    label: stringField(value, "label"),
    url: stringField(value, "url"),
    isDisabled: booleanField(value, "isDisabled"),
    openInNewTab: booleanField(value, "openInNewTab"),
    cssClassName: stringField(value, "cssClassName"),
    ordinal: numberField(value, "ordinal", 0),
    parentNavigationMenuItemId: optionalId(value.parentNavigationMenuItemId),
    navigationMenuId: stringField(value, "navigationMenuId"),
  };
}

export function parseMenuItems(value: unknown): MenuItemDetail[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const items: MenuItemDetail[] = [];
  for (const item of value) {
    const parsed = parseMenuItem(item);
    if (parsed) {
      items.push(parsed);
    }
  }
  return items;
}

export function parseRevision(value: unknown): TemplateRevision | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    creationTime: stringField(value, "creationTime"),
    creatorName: creatorName(value.creatorUser),
    subject: stringField(value, "subject"),
    label: stringField(value, "label"),
    content: stringField(value, "content"),
    code: stringField(value, "code"),
  };
}

export function requireValue<T>(value: T | null, label: string): T {
  if (value === null) {
    throw new Error(`Could not read ${label} from the server.`);
  }
  return value;
}

function taskStatus(value: unknown): { status: BackgroundTaskStatusName; label: string } {
  if (typeof value === "string") {
    return namedTaskStatus(value);
  }
  if (isRecord(value)) {
    return namedTaskStatus(stringField(value, "developerName", "label"));
  }
  return { status: "enqueued", label: "Enqueued" };
}

function namedTaskStatus(name: string): { status: BackgroundTaskStatusName; label: string } {
  const lowered = name.toLowerCase();
  if (lowered === "processing") {
    return { status: "processing", label: "Processing" };
  }
  if (lowered === "complete") {
    return { status: "complete", label: "Complete" };
  }
  if (lowered === "error") {
    return { status: "error", label: "Error" };
  }
  return { status: "enqueued", label: name || "Enqueued" };
}

export function parseBackgroundTask(value: unknown): BackgroundTaskDetail | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  const named = taskStatus(value.status);
  return {
    id,
    name: stringField(value, "name"),
    status: named.status,
    statusLabel: stringField(value, "statusLabel") || named.label,
    statusInfo: stringField(value, "statusInfo"),
    errorMessage: stringField(value, "errorMessage"),
    percentComplete: numberField(value, "percentComplete", 0),
    taskStep: numberField(value, "taskStep", 0),
    creationTime: stringField(value, "creationTime"),
    lastModificationTime: stringField(value, "lastModificationTime"),
    completionTime: stringField(value, "completionTime"),
  };
}

const RETAINED_LOG_KEYS: readonly RetainedLogKey[] = ["audit_logs", "email_logs", "webhook_deliveries", "background_tasks"];

function retainedLogKey(value: unknown): RetainedLogKey | null {
  return RETAINED_LOG_KEYS.find((key) => key === value) ?? null;
}

function sizeUsage(value: unknown): SizeUsage {
  const record = isRecord(value) ? value : {};
  return {
    sizeBytes: numberField(record, "sizeBytes", 0),
    sizeDisplay: stringField(record, "sizeDisplay"),
    maxBytes: numberField(record, "maxBytes", 0),
    maxDisplay: stringField(record, "maxDisplay"),
  };
}

/** Tolerates servers that predate retention settings: their log rows parse with `retentionDays: null`. */
export function parseMaintenanceSnapshot(value: unknown): MaintenanceSnapshot {
  const record = isRecord(value) ? value : {};
  const storage = isRecord(record.storage) ? record.storage : {};
  const tasks = isRecord(record.backgroundTasks) ? record.backgroundTasks : {};
  const logs: RetainedLogStats[] = [];
  for (const item of Array.isArray(record.logs) ? record.logs : []) {
    const key = isRecord(item) ? retainedLogKey(item.key) : null;
    if (!isRecord(item) || key === null) {
      continue;
    }
    const retentionDays = item.retentionDays;
    logs.push({
      key,
      label: stringField(item, "label"),
      rowCount: numberField(item, "rowCount", 0),
      oldestEntry: optionalText(item, "oldestEntry"),
      retentionDays: typeof retentionDays === "number" ? retentionDays : null,
    });
  }
  return {
    version: stringField(record, "version"),
    environment: stringField(record, "environment"),
    database: sizeUsage(record.database),
    storage: {
      ...sizeUsage(storage),
      provider: stringField(storage, "provider"),
      fileCount: numberField(storage, "fileCount", 0),
    },
    logs,
    backgroundTasks: {
      enqueued: numberField(tasks, "enqueued", 0),
      processing: numberField(tasks, "processing", 0),
      complete: numberField(tasks, "complete", 0),
      error: numberField(tasks, "error", 0),
    },
  };
}

export function parseClearedLog(value: unknown): ClearedLog {
  const deleted = isRecord(value) ? value.deleted : undefined;
  return { deleted: typeof deleted === "number" ? deleted : null };
}

export function parseThemeMediaItem(value: unknown): ThemeMediaItem | null {
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id");
  if (id.length === 0) {
    return null;
  }
  return {
    id,
    fileName: stringField(value, "fileName"),
    contentType: stringField(value, "contentType"),
    length: numberField(value, "length", 0),
    objectKey: stringField(value, "objectKey"),
    url: stringField(value, "url"),
  };
}

export function parseThemeMediaItems(value: unknown): ThemeMediaItem[] {
  const items: ThemeMediaItem[] = [];
  const raw = Array.isArray(value) ? value : isRecord(value) && Array.isArray(value.items) ? value.items : [];
  for (const item of raw) {
    const parsed = parseThemeMediaItem(item);
    if (parsed) {
      items.push(parsed);
    }
  }
  return items;
}

/** Export/import tasks store a MediaItemDto JSON blob in statusInfo when a file is ready. */
export function parseTaskMediaItem(statusInfo: string): TaskMediaItem | null {
  if (statusInfo.length === 0 || !statusInfo.startsWith("{")) {
    return null;
  }
  let value: unknown;
  try {
    value = JSON.parse(statusInfo);
  } catch {
    return null;
  }
  if (!isRecord(value)) {
    return null;
  }
  const id = stringField(value, "id", "Id");
  const objectKey = stringField(value, "objectKey", "ObjectKey");
  if (id.length === 0 && objectKey.length === 0) {
    return null;
  }
  const fileName = stringField(value, "fileName", "FileName");
  return {
    id,
    fileName,
    contentType: stringField(value, "contentType", "ContentType") || "text/csv",
    objectKey,
    downloadUrl:
      objectKey.length > 0
        ? `/raytha/media-items/objectkey/${encodeURIComponent(objectKey)}`
        : `/raytha/media-items/id/${encodeURIComponent(id)}`,
  };
}

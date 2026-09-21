import type {
  BackgroundTaskDetail,
  BackgroundTaskStatusName,
  EmailTemplateDetail,
  FunctionDetail,
  IdResponse,
  MenuDetail,
  MenuItemDetail,
  PagedResult,
  TaskMediaItem,
  TemplateRevision,
  ThemeMediaItem,
  WebTemplateDetail,
  WidgetTemplateDetail,
} from "./types";

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

function availableVariables(record: Record<string, unknown>): string[] | null {
  const raw = record.availableVariables ?? record.templateVariables ?? record.variables;
  if (raw == null) {
    return null;
  }
  if (Array.isArray(raw)) {
    const names: string[] = [];
    for (const item of raw) {
      if (typeof item === "string" && item.length > 0) {
        names.push(item);
      } else if (isRecord(item)) {
        const name = stringField(item, "developerName", "name", "key", "value");
        if (name.length > 0) {
          names.push(name);
        }
      }
    }
    return names.length > 0 ? names : null;
  }
  if (isRecord(raw)) {
    const names = Object.keys(raw).filter((key) => key.length > 0);
    return names.length > 0 ? names : null;
  }
  return null;
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
    availableVariables: availableVariables(value),
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
  };
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

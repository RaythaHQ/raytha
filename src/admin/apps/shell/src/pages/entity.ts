import type { EntityRef, JsonObject } from "@raytha/api";

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** Copies enumerable fields off a list/detail DTO. EntityRef only types `id`. */
export function entityFields(entity: EntityRef): Record<string, unknown> {
  const fields: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(entity)) {
    fields[key] = value;
  }
  return fields;
}

export function readString(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string") {
      return value;
    }
    if (typeof value === "number" || typeof value === "boolean") {
      return String(value);
    }
  }
  return "";
}

export function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return false;
}

export function jsonString(data: JsonObject, key: string, fallback = ""): string {
  const value = data[key];
  return typeof value === "string" ? value : fallback;
}

export function jsonBoolean(data: JsonObject, key: string, fallback = false): boolean {
  const value = data[key];
  return typeof value === "boolean" ? value : fallback;
}

export function jsonNumber(data: JsonObject, key: string): number | undefined {
  const value = data[key];
  return typeof value === "number" ? value : undefined;
}

export function displayName(entity: EntityRef): string {
  const fields = entityFields(entity);
  return (
    readString(fields, "fullName", "label", "labelPlural", "title", "name", "fileName", "emailAddress", "subject") ||
    entity.id
  );
}

export function toDeveloperName(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

export function humanizeKey(key: string): string {
  const spaced = key.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/[_-]+/g, " ");
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}

export function formatCell(value: unknown): string {
  if (value === null || value === undefined) {
    return "";
  }
  if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  if (value instanceof Date) {
    return value.toISOString();
  }
  if (isRecord(value)) {
    return readString(value, "label", "developerName", "fullName", "name") || JSON.stringify(value);
  }
  if (Array.isArray(value)) {
    return value.map((item) => formatCell(item)).filter(Boolean).join(", ");
  }
  return "";
}

export function formatWhen(value: unknown): string {
  if (typeof value !== "string" || value.length === 0) {
    return "";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return date.toLocaleString();
}

/** Light English pluralization for list chrome (search, counts, empty states). */
export function pluralize(noun: string): string {
  const trimmed = noun.trim();
  if (trimmed.length === 0) {
    return trimmed;
  }
  if (trimmed.includes(" ")) {
    const parts = trimmed.split(/\s+/);
    const last = parts.at(-1) ?? trimmed;
    return [...parts.slice(0, -1), pluralize(last)].join(" ");
  }
  if (/[^aeiou]y$/i.test(trimmed)) {
    return `${trimmed.slice(0, -1)}ies`;
  }
  if (/(?:s|x|z|ch|sh)$/i.test(trimmed)) {
    return `${trimmed}es`;
  }
  return `${trimmed}s`;
}

/** Turns `Login.Commands.LoginWithEmailAndPassword` into a readable label. */
export function humanizeAuditCategory(category: string): string {
  const leaf = category.split(".").filter(Boolean).at(-1) ?? category;
  const spaced = leaf
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .trim();
  if (spaced.length === 0) {
    return category;
  }
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

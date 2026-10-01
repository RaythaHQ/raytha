import { isRecord, type JsonObject } from "@raytha/api";

export type SchemaFileResult =
  | { ok: true; document: JsonObject; contentTypes: number; fields: number; views: number }
  | { ok: false; error: string };

function count(value: unknown): number {
  return Array.isArray(value) ? value.length : 0;
}

/**
 * Reads the text of a schema file exported from Raytha. This only checks the shape the browser
 * needs to show a summary; the server validates everything else when the preview runs.
 */
export function readSchemaFile(text: string): SchemaFileResult {
  let parsed: unknown;
  try {
    parsed = JSON.parse(text);
  } catch {
    return { ok: false, error: "This file is not valid JSON." };
  }
  if (!isRecord(parsed) || !Array.isArray(parsed.contentTypes)) {
    return { ok: false, error: "This is not a Raytha schema file: it has no contentTypes list." };
  }
  if (parsed.contentTypes.length === 0) {
    return { ok: false, error: "This schema file has no content types." };
  }

  let fields = 0;
  let views = 0;
  for (const contentType of parsed.contentTypes) {
    if (isRecord(contentType)) {
      fields += count(contentType.fields);
      views += count(contentType.views);
    }
  }
  return { ok: true, document: parsed, contentTypes: parsed.contentTypes.length, fields, views };
}

export function schemaFileName(now: Date = new Date()): string {
  return `raytha-schema-${now.toISOString().slice(0, 10)}.json`;
}

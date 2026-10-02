import { describe, expect, it } from "vitest";
import { readSchemaFile, schemaFileName } from "./schema-file";

describe("readSchemaFile", () => {
  it("counts content types, fields, and views", () => {
    const result = readSchemaFile(
      JSON.stringify({
        schemaVersion: 1,
        contentTypes: [
          { developerName: "posts", fields: [{}, {}], views: [{}] },
          { developerName: "authors", fields: [{}], views: [] },
        ],
      }),
    );

    expect(result).toMatchObject({ ok: true, contentTypes: 2, fields: 3, views: 1 });
  });

  it("rejects text that is not JSON", () => {
    expect(readSchemaFile("{nope")).toEqual({ ok: false, error: "This file is not valid JSON." });
  });

  it("rejects JSON that is not a schema", () => {
    expect(readSchemaFile("[1, 2]")).toMatchObject({ ok: false });
    expect(readSchemaFile('{"items": []}')).toMatchObject({ ok: false });
  });

  it("rejects a schema with no content types", () => {
    expect(readSchemaFile('{"contentTypes": []}')).toEqual({
      ok: false,
      error: "This schema file has no content types.",
    });
  });
});

describe("schemaFileName", () => {
  it("is dated", () => {
    expect(schemaFileName(new Date("2026-10-01T12:00:00Z"))).toBe("raytha-schema-2026-10-01.json");
  });
});

import { describe, expect, it } from "vitest";
import { attachmentUrl } from "../../lib/media-upload";
import { fieldValueForSave, parseContentField, parseFieldValue, type ContentField } from "./fields-model";

function field(fieldType: string): ContentField {
  const parsed = parseContentField({ id: "f1", developerName: "author", label: "Author", fieldType });
  if (!parsed) {
    throw new Error(`unparsed ${fieldType}`);
  }
  return parsed;
}

describe("parseFieldValue", () => {
  it("keeps a relationship the admin API returns as the related item", () => {
    const related = {
      id: "fvKgASCku3WdCG1j0X1chw",
      primaryField: "Sophie Wilson",
      publishedContent: { role: { value: "Contributor", text: "Contributor", hasValue: true } },
    };
    const value = parseFieldValue(field("one_to_one_relationship"), related);
    expect(value).toEqual({ fieldType: "one_to_one_relationship", value: "fvKgASCku3WdCG1j0X1chw" });
    expect(fieldValueForSave(value)).toBe("fvKgASCku3WdCG1j0X1chw");
  });

  it("reads a relationship stored as a bare id", () => {
    expect(parseFieldValue(field("one_to_one_relationship"), "fvKgASCku3WdCG1j0X1chw").value).toBe(
      "fvKgASCku3WdCG1j0X1chw",
    );
    expect(parseFieldValue(field("one_to_one_relationship"), null).value).toBe("");
  });

  it("keeps an attachment's object key", () => {
    const value = parseFieldValue(field("attachment"), { value: "abc_report.pdf", text: "abc_report.pdf" });
    expect(fieldValueForSave(value)).toBe("abc_report.pdf");
  });
});

describe("attachmentUrl", () => {
  it("links an object key through the media download route", () => {
    expect(attachmentUrl("abc_report final.pdf")).toBe("/raytha/media-items/objectkey/abc_report%20final.pdf");
  });

  it("leaves a path or absolute URL alone", () => {
    expect(attachmentUrl("/raytha/media-items/objectkey/abc.pdf")).toBe("/raytha/media-items/objectkey/abc.pdf");
    expect(attachmentUrl("https://cdn.example.com/abc.pdf")).toBe("https://cdn.example.com/abc.pdf");
  });
});

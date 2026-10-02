import type { JsonValue, WidgetField, WidgetFieldChoice, WidgetFieldTypeName, WidgetFieldTypeOption } from "@raytha/api";
import { newId } from "../content/parse";

/** One widget template field while it is being edited. `key` is only for React and focus. */
export type WidgetFieldDraft = {
  key: string;
  /** The developer name the server has, or null for a field added in this session. */
  savedName: string | null;
  /** Once the developer name is typed by hand, the label stops rewriting it. */
  nameTouched: boolean;
  developerName: string;
  label: string;
  fieldType: WidgetFieldTypeName;
  description: string;
  isRequired: boolean;
  defaultValue: JsonValue;
  choices: WidgetFieldChoice[];
  subFields: WidgetFieldDraft[];
  contentTypeField: string;
};

/** `Background color` becomes `backgroundColor`, the shape the built-in widgets use. */
export function toCamelCase(label: string): string {
  const words = label
    .normalize("NFKD")
    .replace(/[^A-Za-z0-9]+/g, " ")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  const name = words
    .map((word, index) => {
      const lower = word.toLowerCase();
      return index === 0 ? lower : lower.charAt(0).toUpperCase() + lower.slice(1);
    })
    .join("");
  return /^[0-9]/.test(name) ? `field${name}` : name;
}

function blankChoice(): WidgetFieldChoice {
  return { label: "", developerName: "", disabled: false };
}

export function emptyFieldDraft(fieldType: WidgetFieldTypeName = "single_line_text"): WidgetFieldDraft {
  return {
    key: newId(),
    savedName: null,
    nameTouched: false,
    developerName: "",
    label: "",
    fieldType,
    description: "",
    isRequired: false,
    defaultValue: null,
    choices: [blankChoice()],
    subFields: [],
    contentTypeField: "",
  };
}

export function fieldDrafts(fields: readonly WidgetField[]): WidgetFieldDraft[] {
  return fields.map((field) => ({
    key: newId(),
    savedName: field.developerName,
    nameTouched: true,
    developerName: field.developerName,
    label: field.label,
    fieldType: field.fieldType,
    description: field.description,
    isRequired: field.isRequired,
    defaultValue: field.defaultValue,
    choices: field.choices.length > 0 ? field.choices : [blankChoice()],
    subFields: fieldDrafts(field.subFields),
    contentTypeField: field.contentTypeField,
  }));
}

export function withLabel(draft: WidgetFieldDraft, label: string): WidgetFieldDraft {
  return { ...draft, label, developerName: draft.nameTouched ? draft.developerName : toCamelCase(label) };
}

/** A new type starts without a default: the old one was checked against the old type. */
export function withFieldType(
  draft: WidgetFieldDraft,
  fieldType: WidgetFieldTypeName,
  siblings: readonly WidgetFieldDraft[],
): WidgetFieldDraft {
  const contentTypes = siblings.filter((sibling) => sibling.fieldType === "content_type" && sibling.developerName);
  return {
    ...draft,
    fieldType,
    defaultValue: null,
    contentTypeField:
      fieldType === "view" && !draft.contentTypeField && contentTypes.length === 1
        ? (contentTypes[0]?.developerName ?? "")
        : draft.contentTypeField,
  };
}

export function hasChoices(fieldType: WidgetFieldTypeName, options: readonly WidgetFieldTypeOption[]): boolean {
  return options.find((option) => option.developerName === fieldType)?.hasChoices ?? false;
}

/**
 * The fields as the API takes them. Choices, sub-fields, and the content type link go only on the
 * types that use them, so switching a type back and forth in the editor loses nothing until save.
 */
export function fieldsPayload(
  drafts: readonly WidgetFieldDraft[],
  options: readonly WidgetFieldTypeOption[],
): WidgetField[] {
  return drafts.map((draft) => ({
    developerName: draft.developerName.trim(),
    label: draft.label.trim(),
    fieldType: draft.fieldType,
    description: draft.description,
    isRequired: draft.isRequired,
    defaultValue: draft.fieldType === "repeater" || draft.fieldType === "view" ? null : draft.defaultValue,
    choices: hasChoices(draft.fieldType, options) ? draft.choices : [],
    subFields: draft.fieldType === "repeater" ? fieldsPayload(draft.subFields, options) : [],
    contentTypeField: draft.fieldType === "view" ? draft.contentTypeField : "",
  }));
}

export function moveItem<T>(items: readonly T[], index: number, offset: -1 | 1): T[] {
  const target = index + offset;
  const current = items[index];
  const other = items[target];
  if (current === undefined || other === undefined) {
    return [...items];
  }
  const next = [...items];
  next[index] = other;
  next[target] = current;
  return next;
}

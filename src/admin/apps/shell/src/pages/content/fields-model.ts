import { isRecord, readBoolean, readDeveloperName, readNumber, readString, readStringArray } from "./parse";

export const FIELD_TYPE_NAMES = [
  "single_line_text",
  "long_text",
  "wysiwyg",
  "radio",
  "dropdown",
  "checkbox",
  "multiple_select",
  "date",
  "number",
  "attachment",
  "one_to_one_relationship",
] as const;

export type FieldTypeName = (typeof FIELD_TYPE_NAMES)[number];

const FIELD_TYPE_SET = new Set<string>(FIELD_TYPE_NAMES);

export function isFieldTypeName(value: string): value is FieldTypeName {
  return FIELD_TYPE_SET.has(value);
}

export type FieldChoice = {
  label: string;
  developerName: string;
  disabled: boolean;
};

type FieldBase = {
  id: string;
  label: string;
  developerName: string;
  description: string;
  isRequired: boolean;
  fieldOrder: number;
};

export type ChoiceField = FieldBase & {
  fieldType: "dropdown" | "radio" | "multiple_select";
  choices: FieldChoice[];
};

export type RelationshipField = FieldBase & {
  fieldType: "one_to_one_relationship";
  relatedContentTypeId: string;
};

export type SimpleField = FieldBase & {
  fieldType: "single_line_text" | "long_text" | "wysiwyg" | "checkbox" | "date" | "number" | "attachment";
};

export type ContentField = ChoiceField | RelationshipField | SimpleField;

export type FieldTypeOption = {
  label: string;
  developerName: FieldTypeName;
};

export type ChoiceFieldValue = {
  fieldType: "dropdown" | "radio";
  value: string;
};

export type MultipleSelectFieldValue = {
  fieldType: "multiple_select";
  value: string[];
};

export type CheckboxFieldValue = {
  fieldType: "checkbox";
  value: boolean;
};

export type TextFieldValue = {
  fieldType: "single_line_text" | "long_text" | "wysiwyg" | "date" | "number" | "attachment" | "one_to_one_relationship";
  value: string;
};

export type ContentFieldValue = ChoiceFieldValue | MultipleSelectFieldValue | CheckboxFieldValue | TextFieldValue;

export type NamedRef = {
  id: string;
  label: string;
  developerName: string;
};

export type ContentTypeSummary = {
  id: string;
  labelSingular: string;
  labelPlural: string;
  developerName: string;
  description: string;
  defaultRouteTemplate: string;
  primaryFieldId: string;
  fields: ContentField[];
};

export function parseFieldTypeName(value: unknown): FieldTypeName | undefined {
  const name = readDeveloperName(value).toLowerCase();
  return isFieldTypeName(name) ? name : undefined;
}

export function parseFieldTypeOptions(value: unknown): FieldTypeOption[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const options: FieldTypeOption[] = [];
  for (const item of value) {
    if (!isRecord(item)) {
      continue;
    }
    const developerName = parseFieldTypeName(item.developerName);
    if (!developerName) {
      continue;
    }
    options.push({
      label: readString(item, "label") || developerName,
      developerName,
    });
  }
  return options;
}

export function parseChoice(value: unknown): FieldChoice | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const label = readString(value, "label");
  const developerName = readString(value, "developerName");
  if (!label && !developerName) {
    return undefined;
  }
  return {
    label,
    developerName,
    disabled: readBoolean(value, "disabled"),
  };
}

export function parseContentField(value: unknown): ContentField | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const id = readString(value, "id");
  const fieldType = parseFieldTypeName(value.fieldType);
  if (!id || !fieldType) {
    return undefined;
  }
  const base: FieldBase = {
    id,
    label: readString(value, "label"),
    developerName: readString(value, "developerName"),
    description: readString(value, "description"),
    isRequired: readBoolean(value, "isRequired"),
    fieldOrder: readNumber(value, "fieldOrder") ?? 0,
  };
  if (fieldType === "dropdown" || fieldType === "radio" || fieldType === "multiple_select") {
    const choices: FieldChoice[] = [];
    if (Array.isArray(value.choices)) {
      for (const choice of value.choices) {
        const parsed = parseChoice(choice);
        if (parsed) {
          choices.push(parsed);
        }
      }
    }
    return { ...base, fieldType, choices };
  }
  if (fieldType === "one_to_one_relationship") {
    return {
      ...base,
      fieldType,
      relatedContentTypeId: readString(value, "relatedContentTypeId"),
    };
  }
  return { ...base, fieldType };
}

export function parseContentFields(value: unknown): ContentField[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const fields: ContentField[] = [];
  for (const item of value) {
    const field = parseContentField(item);
    if (field) {
      fields.push(field);
    }
  }
  return fields.sort((a, b) => a.fieldOrder - b.fieldOrder);
}

export function parseContentTypeSummary(value: unknown): ContentTypeSummary | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const id = readString(value, "id");
  const developerName = readString(value, "developerName");
  if (!id || !developerName) {
    return undefined;
  }
  return {
    id,
    labelSingular: readString(value, "labelSingular"),
    labelPlural: readString(value, "labelPlural"),
    developerName,
    description: readString(value, "description"),
    defaultRouteTemplate: readString(value, "defaultRouteTemplate"),
    primaryFieldId: readString(value, "primaryFieldId"),
    fields: parseContentFields(value.contentTypeFields),
  };
}

export function parseNamedRefs(value: unknown): NamedRef[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const refs: NamedRef[] = [];
  for (const item of value) {
    if (!isRecord(item)) {
      continue;
    }
    const id = readString(item, "id");
    if (!id) {
      continue;
    }
    refs.push({
      id,
      label: readString(item, "label", "labelPlural", "primaryField", "title") || id,
      developerName: readString(item, "developerName"),
    });
  }
  return refs;
}

export function emptyFieldValue(field: ContentField): ContentFieldValue {
  switch (field.fieldType) {
    case "checkbox":
      return { fieldType: "checkbox", value: false };
    case "multiple_select":
      return { fieldType: "multiple_select", value: [] };
    case "dropdown":
    case "radio":
      return { fieldType: field.fieldType, value: "" };
    case "single_line_text":
    case "long_text":
    case "wysiwyg":
    case "date":
    case "number":
    case "attachment":
    case "one_to_one_relationship":
      return { fieldType: field.fieldType, value: "" };
    default: {
      const _exhaustive: never = field;
      return _exhaustive;
    }
  }
}

function unwrapStoredField(raw: unknown): unknown {
  if (isRecord(raw) && "value" in raw) {
    return raw.value;
  }
  return raw;
}

export function parseFieldValue(field: ContentField, raw: unknown): ContentFieldValue {
  const stored = unwrapStoredField(raw);
  switch (field.fieldType) {
    case "checkbox":
      return { fieldType: "checkbox", value: stored === true || stored === "true" || stored === "True" };
    case "multiple_select":
      return { fieldType: "multiple_select", value: readStringArray(stored) };
    case "dropdown":
    case "radio":
      return { fieldType: field.fieldType, value: scalarString(stored) };
    case "date":
      return { fieldType: "date", value: toDateInput(scalarString(stored)) };
    case "number":
      return { fieldType: "number", value: scalarString(stored) };
    case "single_line_text":
    case "long_text":
    case "wysiwyg":
    case "attachment":
    case "one_to_one_relationship":
      return { fieldType: field.fieldType, value: scalarString(stored) };
    default: {
      const _exhaustive: never = field;
      return _exhaustive;
    }
  }
}

export function fieldValueForSave(value: ContentFieldValue): string | number | boolean | string[] {
  switch (value.fieldType) {
    case "checkbox":
      return value.value;
    case "multiple_select":
      return value.value;
    case "number": {
      if (value.value.trim() === "") {
        return "";
      }
      const parsed = Number(value.value);
      return Number.isFinite(parsed) ? parsed : value.value;
    }
    case "dropdown":
    case "radio":
    case "single_line_text":
    case "long_text":
    case "wysiwyg":
    case "date":
    case "attachment":
    case "one_to_one_relationship":
      return value.value;
    default: {
      const _exhaustive: never = value;
      return _exhaustive;
    }
  }
}

export function parseContentMap(value: unknown): Record<string, unknown> {
  if (!isRecord(value)) {
    return {};
  }
  return value;
}

function scalarString(value: unknown): string {
  if (typeof value === "string") {
    return value;
  }
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  return "";
}

function toDateInput(value: string): string {
  if (/^\d{4}-\d{2}-\d{2}/.test(value)) {
    return value.slice(0, 10);
  }
  if (value.length === 0) {
    return "";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function fieldTypeLabel(fieldType: FieldTypeName, options: FieldTypeOption[]): string {
  return options.find((option) => option.developerName === fieldType)?.label ?? fieldType.replace(/_/g, " ");
}

export function hasChoices(fieldType: FieldTypeName): boolean {
  return fieldType === "dropdown" || fieldType === "radio" || fieldType === "multiple_select";
}

export function isRelationship(fieldType: FieldTypeName): boolean {
  return fieldType === "one_to_one_relationship";
}

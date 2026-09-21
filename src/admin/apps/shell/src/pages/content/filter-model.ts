import type { ContentField, FieldTypeName } from "./fields-model";
import { isRecord, newId, readDeveloperName, readString } from "./parse";

export const GROUP_OPERATORS = ["AND", "OR", "NOT"] as const;
export type GroupOperator = (typeof GROUP_OPERATORS)[number];

export type ConditionOperator = {
  developerName: string;
  label: string;
  needsValue: boolean;
};

export type FilterConditionNode = {
  kind: "condition";
  id: string;
  parentId: string | null;
  field: string;
  conditionOperator: string;
  value: string;
};

export type FilterGroupNode = {
  kind: "group";
  id: string;
  parentId: string | null;
  groupOperator: GroupOperator;
  children: FilterNode[];
};

export type FilterNode = FilterConditionNode | FilterGroupNode;

export type FilterPayload = {
  id: string;
  parentId: string | null;
  type: string;
  groupOperator: string;
  field: string | null;
  conditionOperator: string;
  value: string | null;
};

export type SortDirection = "asc" | "desc";

export type ViewSortRow = {
  developerName: string;
  direction: SortDirection;
};

export type ViewColumnOption = {
  developerName: string;
  label: string;
  fieldType: FieldTypeName | "id";
};

export type ViewModel = {
  id: string;
  label: string;
  developerName: string;
  description: string;
  columns: string[];
  sort: ViewSortRow[];
  filter: FilterGroupNode;
  routePath: string;
  isPublished: boolean;
  defaultNumberOfItemsPerPage: number;
  maxNumberOfItemsPerPage: number;
  ignoreClientFilterAndSortQueryParams: boolean;
};

const TEXT_OPERATORS: ConditionOperator[] = [
  { developerName: "eq", label: "equals", needsValue: true },
  { developerName: "ne", label: "does not equal", needsValue: true },
  { developerName: "contains", label: "contains", needsValue: true },
  { developerName: "notcontains", label: "does not contain", needsValue: true },
  { developerName: "startswith", label: "starts with", needsValue: true },
  { developerName: "notstartswith", label: "does not start with", needsValue: true },
  { developerName: "endswith", label: "ends with", needsValue: true },
  { developerName: "notendswith", label: "does not end with", needsValue: true },
  { developerName: "empty", label: "is empty", needsValue: false },
  { developerName: "notempty", label: "is not empty", needsValue: false },
];

const SELECT_OPERATORS: ConditionOperator[] = [
  { developerName: "eq", label: "equals", needsValue: true },
  { developerName: "ne", label: "does not equal", needsValue: true },
  { developerName: "empty", label: "is empty", needsValue: false },
  { developerName: "notempty", label: "is not empty", needsValue: false },
];

const NUMERIC_OPERATORS: ConditionOperator[] = [
  { developerName: "eq", label: "equals", needsValue: true },
  { developerName: "ne", label: "does not equal", needsValue: true },
  { developerName: "gt", label: "greater than", needsValue: true },
  { developerName: "ge", label: "greater than or equal", needsValue: true },
  { developerName: "lt", label: "less than", needsValue: true },
  { developerName: "le", label: "less than or equal", needsValue: true },
  { developerName: "empty", label: "is empty", needsValue: false },
  { developerName: "notempty", label: "is not empty", needsValue: false },
];

const MULTI_OPERATORS: ConditionOperator[] = [
  { developerName: "has", label: "has", needsValue: true },
  { developerName: "nothas", label: "does not have", needsValue: true },
  { developerName: "empty", label: "is empty", needsValue: false },
  { developerName: "notempty", label: "is not empty", needsValue: false },
];

const CHECKBOX_OPERATORS: ConditionOperator[] = [
  { developerName: "true", label: "is true", needsValue: false },
  { developerName: "false", label: "is false", needsValue: false },
];

const ID_OPERATORS: ConditionOperator[] = [
  { developerName: "eq", label: "equals", needsValue: true },
  { developerName: "ne", label: "does not equal", needsValue: true },
];

const ATTACHMENT_OPERATORS: ConditionOperator[] = [
  { developerName: "empty", label: "is empty", needsValue: false },
  { developerName: "notempty", label: "is not empty", needsValue: false },
];

export const BUILT_IN_COLUMNS: ViewColumnOption[] = [
  { developerName: "PrimaryField", label: "Primary field", fieldType: "single_line_text" },
  { developerName: "CreationTime", label: "Created at", fieldType: "date" },
  { developerName: "CreatorUser", label: "Created by", fieldType: "single_line_text" },
  { developerName: "LastModificationTime", label: "Last modified at", fieldType: "date" },
  { developerName: "LastModifierUser", label: "Last modified by", fieldType: "single_line_text" },
  { developerName: "Id", label: "Id", fieldType: "id" },
  { developerName: "IsDraft", label: "Is draft", fieldType: "checkbox" },
  { developerName: "IsPublished", label: "Is published", fieldType: "checkbox" },
  { developerName: "Template", label: "Template", fieldType: "single_line_text" },
  { developerName: "RoutePath", label: "Route path", fieldType: "single_line_text" },
];

export function operatorsForFieldType(fieldType: FieldTypeName | "id"): ConditionOperator[] {
  switch (fieldType) {
    case "single_line_text":
    case "long_text":
    case "wysiwyg":
    case "one_to_one_relationship":
      return TEXT_OPERATORS;
    case "radio":
    case "dropdown":
      return SELECT_OPERATORS;
    case "checkbox":
      return CHECKBOX_OPERATORS;
    case "multiple_select":
      return MULTI_OPERATORS;
    case "date":
    case "number":
      return NUMERIC_OPERATORS;
    case "attachment":
      return ATTACHMENT_OPERATORS;
    case "id":
      return ID_OPERATORS;
    default: {
      const _exhaustive: never = fieldType;
      return _exhaustive;
    }
  }
}

export function viewColumnOptions(fields: ContentField[]): ViewColumnOption[] {
  const custom: ViewColumnOption[] = fields.map((field) => ({
    developerName: field.developerName,
    label: field.label || field.developerName,
    fieldType: field.fieldType,
  }));
  return [...BUILT_IN_COLUMNS, ...custom];
}

export function emptyRootGroup(): FilterGroupNode {
  return {
    kind: "group",
    id: newId(),
    parentId: null,
    groupOperator: "AND",
    children: [],
  };
}

export function emptyCondition(parentId: string): FilterConditionNode {
  return {
    kind: "condition",
    id: newId(),
    parentId,
    field: "",
    conditionOperator: "eq",
    value: "",
  };
}

export function emptyChildGroup(parentId: string): FilterGroupNode {
  return {
    kind: "group",
    id: newId(),
    parentId,
    groupOperator: "AND",
    children: [],
  };
}

function parseGroupOperator(value: unknown): GroupOperator {
  const name = readDeveloperName(value).toUpperCase();
  if (name === "AND" || name === "OR" || name === "NOT") {
    return name;
  }
  return "AND";
}

function parseSortDirection(value: unknown): SortDirection {
  const name = readDeveloperName(value).toLowerCase();
  return name === "desc" ? "desc" : "asc";
}

type FlatRow = {
  id: string;
  parentId: string | null;
  type: string;
  groupOperator: GroupOperator;
  field: string;
  conditionOperator: string;
  value: string;
};

function parseFlatRow(value: unknown): FlatRow | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const id = readString(value, "id");
  if (!id) {
    return undefined;
  }
  const parentRaw = value.parentId;
  const parentId = typeof parentRaw === "string" && parentRaw.length > 0 ? parentRaw : null;
  return {
    id,
    parentId,
    type: readDeveloperName(value.type).toLowerCase(),
    groupOperator: parseGroupOperator(value.groupOperator),
    field: readString(value, "field"),
    conditionOperator: readDeveloperName(value.conditionOperator).toLowerCase(),
    value: readString(value, "value"),
  };
}

function buildGroup(row: FlatRow, byParent: Map<string, FlatRow[]>): FilterGroupNode {
  const childrenRows = byParent.get(row.id) ?? [];
  const children: FilterNode[] = [];
  for (const child of childrenRows) {
    if (child.type === "filter_condition_group") {
      children.push(buildGroup(child, byParent));
    } else {
      children.push({
        kind: "condition",
        id: child.id,
        parentId: child.parentId,
        field: child.field,
        conditionOperator: child.conditionOperator || "eq",
        value: child.value,
      });
    }
  }
  return {
    kind: "group",
    id: row.id,
    parentId: row.parentId,
    groupOperator: row.groupOperator,
    children,
  };
}

export function parseFilterTree(value: unknown): FilterGroupNode {
  if (!Array.isArray(value) || value.length === 0) {
    return emptyRootGroup();
  }
  const rows: FlatRow[] = [];
  for (const item of value) {
    const row = parseFlatRow(item);
    if (row) {
      rows.push(row);
    }
  }
  if (rows.length === 0) {
    return emptyRootGroup();
  }
  const byParent = new Map<string, FlatRow[]>();
  const roots: FlatRow[] = [];
  for (const row of rows) {
    if (row.parentId === null) {
      roots.push(row);
    } else {
      const list = byParent.get(row.parentId) ?? [];
      list.push(row);
      byParent.set(row.parentId, list);
    }
  }
  const root = roots.find((row) => row.type === "filter_condition_group") ?? roots[0];
  if (!root) {
    return emptyRootGroup();
  }
  if (root.type === "filter_condition_group") {
    return buildGroup(root, byParent);
  }
  const wrapper = emptyRootGroup();
  wrapper.children = roots.map((row) => {
    if (row.type === "filter_condition_group") {
      return buildGroup(row, byParent);
    }
    const condition: FilterConditionNode = {
      kind: "condition",
      id: row.id,
      parentId: wrapper.id,
      field: row.field,
      conditionOperator: row.conditionOperator || "eq",
      value: row.value,
    };
    return condition;
  });
  return wrapper;
}

export function flattenFilter(root: FilterGroupNode): FilterPayload[] {
  const rows: FilterPayload[] = [];
  const walk = (node: FilterNode) => {
    if (node.kind === "group") {
      rows.push({
        id: node.id,
        parentId: node.parentId,
        type: "filter_condition_group",
        groupOperator: node.groupOperator,
        field: null,
        conditionOperator: "",
        value: null,
      });
      for (const child of node.children) {
        walk(child);
      }
      return;
    }
    rows.push({
      id: node.id,
      parentId: node.parentId,
      type: "filter_condition",
      groupOperator: "",
      field: node.field,
      conditionOperator: node.conditionOperator,
      value: node.value,
    });
  };
  walk(root);
  return rows;
}

export function parseViewSort(value: unknown): ViewSortRow[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const rows: ViewSortRow[] = [];
  for (const item of value) {
    if (!isRecord(item)) {
      continue;
    }
    const developerName = readString(item, "developerName");
    if (!developerName) {
      continue;
    }
    rows.push({
      developerName,
      direction: parseSortDirection(item.sortOrder),
    });
  }
  return rows;
}

export function parseViewModel(value: unknown): ViewModel | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const id = readString(value, "id");
  if (!id) {
    return undefined;
  }
  const columns = Array.isArray(value.columns)
    ? value.columns.filter((column): column is string => typeof column === "string")
    : [];
  return {
    id,
    label: readString(value, "label"),
    developerName: readString(value, "developerName"),
    description: readString(value, "description"),
    columns,
    sort: parseViewSort(value.sort),
    filter: parseFilterTree(value.filter),
    routePath: readString(value, "routePath"),
    isPublished: value.isPublished === true,
    defaultNumberOfItemsPerPage: typeof value.defaultNumberOfItemsPerPage === "number" ? value.defaultNumberOfItemsPerPage : 25,
    maxNumberOfItemsPerPage: typeof value.maxNumberOfItemsPerPage === "number" ? value.maxNumberOfItemsPerPage : 1000,
    ignoreClientFilterAndSortQueryParams: value.ignoreClientFilterAndSortQueryParams === true,
  };
}

export function operatorNeedsValue(operator: string, fieldType: FieldTypeName | "id"): boolean {
  return operatorsForFieldType(fieldType).find((item) => item.developerName === operator)?.needsValue ?? true;
}

import type { JsonObject } from "@raytha/api";
import { ArrowDown, ArrowUp, ListPlus, Plus, Trash2 } from "lucide-react";
import { useRef } from "react";
import { Button, Checkbox, EmptyState, FormField, Input, Select } from "@raytha/ui";
import { toDeveloperName } from "../entity";
import { ChoicesEditor } from "./choices-editor";
import {
  REPEATER_SUB_FIELD_TYPES,
  fieldTypeLabel,
  type FieldChoice,
  type FieldDefinition,
  type FieldTypeOption,
  type RepeaterSubFieldType,
} from "./fields-model";
import { newId } from "./parse";
import { usePendingFocus } from "./use-pending-focus";

export type SubFieldDraft = {
  key: string;
  label: string;
  developerName: string;
  fieldType: RepeaterSubFieldType;
  description: string;
  isRequired: boolean;
  choices: FieldChoice[];
  /** Saved sub-fields keep their developer name and type, so rows already stored still read back. */
  locked: boolean;
};

const SUB_FIELD_TYPE_SET = new Set<string>(REPEATER_SUB_FIELD_TYPES);

function isSubFieldType(value: string): value is RepeaterSubFieldType {
  return SUB_FIELD_TYPE_SET.has(value);
}

function takesChoices(fieldType: RepeaterSubFieldType): boolean {
  return fieldType === "dropdown" || fieldType === "radio";
}

function blankChoice(): FieldChoice {
  return { label: "", developerName: "", disabled: false };
}

function emptySubField(): SubFieldDraft {
  return {
    key: newId(),
    label: "",
    developerName: "",
    fieldType: "single_line_text",
    description: "",
    isRequired: false,
    choices: [blankChoice()],
    locked: false,
  };
}

export function subFieldDrafts(definitions: FieldDefinition[]): SubFieldDraft[] {
  const drafts: SubFieldDraft[] = [];
  for (const definition of definitions) {
    if (!isSubFieldType(definition.fieldType)) {
      continue;
    }
    drafts.push({
      key: newId(),
      label: definition.label,
      developerName: definition.developerName,
      fieldType: definition.fieldType,
      description: definition.description,
      isRequired: definition.isRequired,
      choices: definition.choices.length > 0 ? definition.choices : [blankChoice()],
      locked: true,
    });
  }
  return drafts;
}

export function subFieldsPayload(drafts: SubFieldDraft[]): JsonObject[] {
  return drafts.map((draft) => ({
    developerName: draft.developerName,
    label: draft.label,
    fieldType: draft.fieldType,
    description: draft.description,
    isRequired: draft.isRequired,
    choices: takesChoices(draft.fieldType)
      ? draft.choices.map((choice) => ({
          label: choice.label,
          developerName: choice.developerName,
          disabled: choice.disabled,
        }))
      : [],
  }));
}

export function SubFieldsEditor({
  subFields,
  onChange,
  fieldTypes,
  fieldDeveloperName,
}: {
  subFields: SubFieldDraft[];
  onChange: (next: SubFieldDraft[]) => void;
  fieldTypes: FieldTypeOption[];
  fieldDeveloperName: string;
}) {
  const rootRef = useRef<HTMLDivElement>(null);
  const focusAfterRender = usePendingFocus();
  const card = (key: string) =>
    rootRef.current?.querySelector<HTMLElement>(`:scope > ol > li[data-sub-field-key="${key}"]`);
  const addButton = () => rootRef.current?.querySelector<HTMLElement>("[data-sub-field-add]");

  const update = (index: number, patch: Partial<SubFieldDraft>) => {
    onChange(subFields.map((draft, i) => (i === index ? { ...draft, ...patch } : draft)));
  };

  const add = () => {
    const draft = emptySubField();
    focusAfterRender(() => document.getElementById(`sub-field-${draft.key}-label`));
    onChange([...subFields, draft]);
  };

  const remove = (index: number) => {
    const neighbour = subFields[index + 1] ?? subFields[index - 1];
    focusAfterRender(
      () =>
        (neighbour ? card(neighbour.key)?.querySelector<HTMLElement>('[data-sub-field-action="remove"]') : null) ??
        addButton(),
    );
    onChange(subFields.filter((_, i) => i !== index));
  };

  const move = (index: number, offset: -1 | 1) => {
    const target = index + offset;
    const current = subFields[index];
    const other = subFields[target];
    if (!current || !other) {
      return;
    }
    const next = [...subFields];
    next[index] = other;
    next[target] = current;
    const action = offset < 0 ? "up" : "down";
    focusAfterRender(
      () =>
        card(current.key)?.querySelector<HTMLElement>(`[data-sub-field-action="${action}"]:not(:disabled)`) ??
        card(current.key)?.querySelector<HTMLElement>("[data-sub-field-action]:not(:disabled)"),
    );
    onChange(next);
  };

  const loopPath = `Target.PublishedContent.${fieldDeveloperName || "field_name"}.Value`;
  const loopBody = subFields.length
    ? subFields.map((draft) => `  {{ row.${draft.developerName || "sub_field"} }}`).join("\n")
    : "  {{ row.sub_field }}";

  return (
    <div ref={rootRef} className="space-y-4" role="group" aria-labelledby="sub-fields-heading">
      <div className="space-y-1">
        <p id="sub-fields-heading" className="text-sm font-medium">
          Sub-fields
          {subFields.length > 0 ? (
            <span className="ml-2 rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
              {subFields.length}
            </span>
          ) : null}
        </p>
        <p className="text-xs leading-5 text-muted-foreground">
          Every row of this repeater holds these fields, in this order. Repeaters cannot nest, hold relationships, or
          hold multiple selects.
        </p>
      </div>

      {subFields.length === 0 ? (
        <EmptyState
          icon={ListPlus}
          className="py-8"
          title="No sub-fields yet"
          hint="Add the fields each row should hold, like a question and an answer."
          action={
            <Button type="button" size="sm" data-sub-field-add onClick={add}>
              <Plus className="size-4" aria-hidden />
              Add the first sub-field
            </Button>
          }
        />
      ) : (
        <ol className="space-y-3">
          {subFields.map((draft, index) => {
            const prefix = `sub-field-${draft.key}`;
            const name = draft.label || `Sub-field ${index + 1}`;
            return (
              <li
                key={draft.key}
                data-sub-field-key={draft.key}
                className="overflow-hidden rounded-xl border border-border bg-card shadow-xs transition-shadow focus-within:shadow-card"
              >
                <div className="flex items-center gap-2 border-b border-border bg-muted/40 py-1.5 pr-1.5 pl-3">
                  <span className="inline-flex size-6 shrink-0 items-center justify-center rounded-md bg-brand-50 text-xs font-semibold text-brand-700 tabular-nums">
                    {index + 1}
                  </span>
                  <span className={draft.label ? "truncate text-sm font-medium" : "truncate text-sm text-muted-foreground"}>
                    {draft.label || "New sub-field"}
                  </span>
                  <span className="shrink-0 rounded-full border border-border bg-card px-2 py-0.5 text-xs text-muted-foreground">
                    {fieldTypeLabel(draft.fieldType, fieldTypes)}
                  </span>
                  {draft.developerName ? (
                    <code className="hidden truncate text-xs text-muted-foreground sm:inline">
                      row.{draft.developerName}
                    </code>
                  ) : null}
                  <span className="flex-1" />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    data-sub-field-action="up"
                    aria-label={`Move ${name} up`}
                    disabled={index === 0}
                    onClick={() => move(index, -1)}
                  >
                    <ArrowUp className="size-4" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    data-sub-field-action="down"
                    aria-label={`Move ${name} down`}
                    disabled={index === subFields.length - 1}
                    onClick={() => move(index, 1)}
                  >
                    <ArrowDown className="size-4" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8 text-muted-foreground hover:text-destructive"
                    data-sub-field-action="remove"
                    aria-label={`Remove ${name}`}
                    onClick={() => remove(index)}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </Button>
                </div>
                <div className="grid gap-4 p-4 sm:grid-cols-2">
                  <FormField label="Label" required htmlFor={`${prefix}-label`}>
                    {(control) => (
                      <Input
                        {...control}
                        value={draft.label}
                        onChange={(event) => {
                          const label = event.target.value;
                          const follows =
                            !draft.locked &&
                            (draft.developerName === "" || draft.developerName === toDeveloperName(draft.label));
                          update(index, {
                            label,
                            developerName: follows ? toDeveloperName(label) : draft.developerName,
                          });
                        }}
                      />
                    )}
                  </FormField>
                  <FormField
                    label="Developer name"
                    required
                    htmlFor={`${prefix}-developer-name`}
                    hint={draft.locked ? "Saved sub-fields keep their developer name." : undefined}
                  >
                    {(control) => (
                      <Input
                        {...control}
                        className="font-mono"
                        value={draft.developerName}
                        disabled={draft.locked}
                        onChange={(event) => update(index, { developerName: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField
                    label="Type"
                    required
                    htmlFor={`${prefix}-type`}
                    hint={draft.locked ? "Saved sub-fields keep their type." : undefined}
                  >
                    {(control) => (
                      <Select
                        {...control}
                        value={draft.fieldType}
                        disabled={draft.locked}
                        onChange={(event) => {
                          const next = event.target.value;
                          if (isSubFieldType(next)) {
                            update(index, { fieldType: next });
                          }
                        }}
                      >
                        {REPEATER_SUB_FIELD_TYPES.map((type) => (
                          <option key={type} value={type}>
                            {fieldTypeLabel(type, fieldTypes)}
                          </option>
                        ))}
                      </Select>
                    )}
                  </FormField>
                  <FormField label="Help text" htmlFor={`${prefix}-description`}>
                    {(control) => (
                      <Input
                        {...control}
                        value={draft.description}
                        onChange={(event) => update(index, { description: event.target.value })}
                      />
                    )}
                  </FormField>
                  <div className="flex items-center gap-2 sm:col-span-2">
                    <Checkbox
                      id={`${prefix}-required`}
                      checked={draft.isRequired}
                      onCheckedChange={(checked) => update(index, { isRequired: checked })}
                    />
                    <label htmlFor={`${prefix}-required`} className="text-sm">
                      Required in every row
                    </label>
                  </div>
                  {takesChoices(draft.fieldType) ? (
                    <div className="sm:col-span-2">
                      <ChoicesEditor
                        labelPrefix={`${name} `}
                        choices={draft.choices}
                        onChange={(choices) => update(index, { choices })}
                      />
                    </div>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ol>
      )}

      {subFields.length > 0 ? (
        <Button type="button" variant="outline" size="sm" data-sub-field-add onClick={add}>
          <Plus className="size-4" aria-hidden />
          Add sub-field
        </Button>
      ) : null}

      <div className="space-y-2 rounded-xl border border-border bg-muted/30 p-4">
        <p className="text-sm font-medium">Use it in a template</p>
        <p className="text-xs leading-5 text-muted-foreground">
          The value is a list of rows. Loop it and read each sub-field by its developer name.
        </p>
        <pre className="overflow-x-auto rounded-lg border border-border bg-card p-3 font-mono text-xs leading-5">
          {`{% for row in ${loopPath} %}\n${loopBody}\n{% endfor %}`}
        </pre>
      </div>
    </div>
  );
}

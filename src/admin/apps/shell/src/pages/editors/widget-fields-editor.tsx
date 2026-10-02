import type { JsonValue, WidgetFieldTypeName, WidgetFieldTypeOption } from "@raytha/api";
import { Button, Checkbox, EmptyState, FormField, Input, Select, cn } from "@raytha/ui";
import { ArrowDown, ArrowUp, ChevronDown, ListPlus, Plus, Trash2 } from "lucide-react";
import { useRef, useState } from "react";
import { ChoicesEditor } from "../content/choices-editor";
import {
  DefinitionFieldControl,
  type ControlDefinition,
  type ControlFieldType,
} from "../content/definition-field-control";
import { usePendingFocus } from "../content/use-pending-focus";
import {
  emptyFieldDraft,
  hasChoices,
  moveItem,
  withFieldType,
  withLabel,
  type WidgetFieldDraft,
} from "./widget-fields-model";

/** The input a field's default value is typed into; null when the type takes no default. */
const DEFAULT_INPUT: Record<WidgetFieldTypeName, ControlFieldType | null> = {
  single_line_text: "single_line_text",
  long_text: "long_text",
  wysiwyg: "long_text",
  number: "number",
  checkbox: "checkbox",
  date: "date",
  dropdown: "dropdown",
  radio: "dropdown",
  color: "color",
  repeater: null,
  image: "single_line_text",
  content_type: "content_type",
  view: null,
};

type Scope = "widget" | "row";

/**
 * The ordered fields of a widget template, or the sub-fields of one of its repeaters. Each card
 * edits one field; new cards open, saved ones start collapsed to their summary line.
 */
export function WidgetFieldsEditor({
  fields,
  onChange,
  typeOptions,
  scope = "widget",
  idPrefix = "widget-field",
}: {
  fields: WidgetFieldDraft[];
  onChange: (next: WidgetFieldDraft[]) => void;
  typeOptions: WidgetFieldTypeOption[];
  scope?: Scope;
  idPrefix?: string;
}) {
  const rootRef = useRef<HTMLDivElement>(null);
  const focusAfterRender = usePendingFocus();
  const [open, setOpen] = useState<ReadonlySet<string>>(() => new Set());
  const card = (key: string) =>
    rootRef.current?.querySelector<HTMLElement>(`:scope > ol > li[data-field-key="${key}"]`);
  const addButton = () =>
    rootRef.current?.querySelector<HTMLElement>(":scope > [data-field-add]") ??
    rootRef.current?.querySelector<HTMLElement>("[data-field-add-first]");
  const allowed = scope === "row" ? typeOptions.filter((option) => option.allowedInRepeater) : typeOptions;
  const noun = scope === "row" ? "sub-field" : "field";

  const update = (index: number, next: WidgetFieldDraft) => onChange(fields.map((draft, i) => (i === index ? next : draft)));

  const toggle = (key: string) =>
    setOpen((current) => {
      const next = new Set(current);
      if (!next.delete(key)) {
        next.add(key);
      }
      return next;
    });

  const add = () => {
    const draft = emptyFieldDraft();
    setOpen((current) => new Set(current).add(draft.key));
    focusAfterRender(() => document.getElementById(`${idPrefix}-${draft.key}-label`));
    onChange([...fields, draft]);
  };

  const remove = (index: number) => {
    const neighbour = fields[index + 1] ?? fields[index - 1];
    focusAfterRender(
      () =>
        (neighbour ? card(neighbour.key)?.querySelector<HTMLElement>('[data-field-action="remove"]') : null) ??
        addButton(),
    );
    onChange(fields.filter((_, i) => i !== index));
  };

  const move = (index: number, offset: -1 | 1) => {
    const current = fields[index];
    if (!current) {
      return;
    }
    const action = offset < 0 ? "up" : "down";
    focusAfterRender(
      () =>
        card(current.key)?.querySelector<HTMLElement>(`[data-field-action="${action}"]:not(:disabled)`) ??
        card(current.key)?.querySelector<HTMLElement>("[data-field-action]:not(:disabled)"),
    );
    onChange(moveItem(fields, index, offset));
  };

  return (
    <div ref={rootRef} className="space-y-3">
      {fields.length === 0 ? (
        <EmptyState
          icon={ListPlus}
          className="py-8"
          title={scope === "row" ? "No sub-fields yet" : "No fields yet"}
          hint={
            scope === "row"
              ? "Add what each row holds, like a question and an answer."
              : "Fields become the widget's settings form and its widget.settings values in Liquid."
          }
          action={
            <Button type="button" size="sm" data-field-add-first onClick={add}>
              <Plus className="size-4" aria-hidden />
              Add the first {noun}
            </Button>
          }
        />
      ) : (
        <ol className="space-y-3">
          {fields.map((draft, index) => {
            const prefix = `${idPrefix}-${draft.key}`;
            const name = draft.label || `${scope === "row" ? "Sub-field" : "Field"} ${index + 1}`;
            const expanded = open.has(draft.key);
            const typeLabel = allowed.find((option) => option.developerName === draft.fieldType)?.label ?? draft.fieldType;
            return (
              <li
                key={draft.key}
                data-field-key={draft.key}
                className="overflow-hidden rounded-xl border border-border bg-card shadow-xs transition-shadow focus-within:shadow-card"
              >
                <div
                  className={cn(
                    "flex items-center gap-1 bg-muted/40 py-1.5 pr-1.5 pl-2",
                    expanded && "border-b border-border",
                  )}
                >
                  <button
                    type="button"
                    aria-expanded={expanded}
                    aria-controls={`${prefix}-body`}
                    onClick={() => toggle(draft.key)}
                    className="flex min-w-0 flex-1 items-center gap-2 rounded-md px-1 py-1 text-left text-sm outline-none focus-visible:ring-[3px] focus-visible:ring-brand-500/25"
                  >
                    <ChevronDown
                      aria-hidden
                      className={cn("size-4 shrink-0 text-muted-foreground transition-transform", !expanded && "-rotate-90")}
                    />
                    <span className="inline-flex size-6 shrink-0 items-center justify-center rounded-md bg-brand-50 text-xs font-semibold text-brand-700 tabular-nums">
                      {index + 1}
                    </span>
                    <span className={cn("truncate", draft.label ? "font-medium" : "text-muted-foreground")}>
                      {draft.label || `New ${noun}`}
                    </span>
                    <span className="shrink-0 rounded-full border border-border bg-card px-2 py-0.5 text-xs text-muted-foreground">
                      {typeLabel}
                    </span>
                    {draft.isRequired ? <span className="sr-only">required</span> : null}
                    {draft.developerName ? (
                      <code className="hidden truncate text-xs text-muted-foreground md:inline">
                        {scope === "row" ? "row" : "widget.settings"}.{draft.developerName}
                      </code>
                    ) : null}
                  </button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    data-field-action="up"
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
                    data-field-action="down"
                    aria-label={`Move ${name} down`}
                    disabled={index === fields.length - 1}
                    onClick={() => move(index, 1)}
                  >
                    <ArrowDown className="size-4" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8 text-muted-foreground hover:text-destructive"
                    data-field-action="remove"
                    aria-label={`Remove ${name}`}
                    onClick={() => remove(index)}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </Button>
                </div>
                <div id={`${prefix}-body`} hidden={!expanded}>
                  {expanded ? (
                    <FieldForm
                      draft={draft}
                      siblings={fields}
                      prefix={prefix}
                      name={name}
                      typeOptions={typeOptions}
                      allowed={allowed}
                      onChange={(next) => update(index, next)}
                    />
                  ) : null}
                </div>
              </li>
            );
          })}
        </ol>
      )}

      {fields.length > 0 ? (
        <Button type="button" variant="outline" size="sm" data-field-add onClick={add}>
          <Plus className="size-4" aria-hidden />
          Add {noun}
        </Button>
      ) : null}
    </div>
  );
}

function FieldForm({
  draft,
  siblings,
  prefix,
  name,
  typeOptions,
  allowed,
  onChange,
}: {
  draft: WidgetFieldDraft;
  siblings: WidgetFieldDraft[];
  prefix: string;
  name: string;
  typeOptions: WidgetFieldTypeOption[];
  allowed: WidgetFieldTypeOption[];
  onChange: (next: WidgetFieldDraft) => void;
}) {
  const renamed = draft.savedName !== null && draft.developerName !== draft.savedName;
  const defaultInput = DEFAULT_INPUT[draft.fieldType];
  const contentTypeFields = siblings.filter(
    (sibling) => sibling.key !== draft.key && sibling.fieldType === "content_type" && sibling.developerName,
  );
  const typeChoices = allowed.some((option) => option.developerName === draft.fieldType)
    ? allowed
    : [{ developerName: draft.fieldType, label: draft.fieldType, hasChoices: false, allowedInRepeater: false }, ...allowed];

  const defaultDefinition: ControlDefinition | null = defaultInput
    ? {
        developerName: "default",
        label: draft.fieldType === "checkbox" ? "Checked by default" : "Default value",
        fieldType: defaultInput,
        description: "Filled in when a widget is added. Widgets already on a page keep what they have.",
        isRequired: false,
        choices: draft.choices.filter((choice) => choice.developerName),
        subFields: [],
      }
    : null;

  return (
    <div className="grid gap-4 p-4 sm:grid-cols-2">
      <FormField label="Label" required htmlFor={`${prefix}-label`}>
        {(control) => (
          <Input {...control} value={draft.label} onChange={(event) => onChange(withLabel(draft, event.target.value))} />
        )}
      </FormField>
      <FormField
        label="Developer name"
        required
        htmlFor={`${prefix}-developer-name`}
        hint={
          renamed
            ? `Widgets saved earlier keep their value under ${draft.savedName}, which this form will no longer show.`
            : "Letters, digits, and underscores. camelCase is fine."
        }
      >
        {(control) => (
          <Input
            {...control}
            className="font-mono"
            value={draft.developerName}
            onChange={(event) => onChange({ ...draft, developerName: event.target.value, nameTouched: true })}
          />
        )}
      </FormField>
      <FormField label="Type" required htmlFor={`${prefix}-type`}>
        {(control) => (
          <Select
            {...control}
            value={draft.fieldType}
            onChange={(event) => {
              const next = typeChoices.find((option) => option.developerName === event.target.value);
              if (next) {
                onChange(withFieldType(draft, next.developerName, siblings));
              }
            }}
          >
            {typeChoices.map((option) => (
              <option key={option.developerName} value={option.developerName}>
                {option.label}
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
            onChange={(event) => onChange({ ...draft, description: event.target.value })}
          />
        )}
      </FormField>
      <div className="flex items-center gap-2 sm:col-span-2">
        <Checkbox
          id={`${prefix}-required`}
          checked={draft.isRequired}
          onCheckedChange={(checked) => onChange({ ...draft, isRequired: checked })}
        />
        <label htmlFor={`${prefix}-required`} className="text-sm">
          Required
        </label>
      </div>

      {draft.fieldType === "view" ? (
        <FormField
          label="Views of"
          required
          htmlFor={`${prefix}-content-type-field`}
          hint={
            contentTypeFields.length === 0
              ? "Add a content type field to this list first; this dropdown lists that type's views."
              : "The content type field whose views this dropdown lists."
          }
        >
          {(control) => (
            <Select
              {...control}
              value={draft.contentTypeField}
              onChange={(event) => onChange({ ...draft, contentTypeField: event.target.value })}
            >
              <option value="">Select a content type field</option>
              {contentTypeFields.map((sibling) => (
                <option key={sibling.key} value={sibling.developerName}>
                  {sibling.label || sibling.developerName}
                </option>
              ))}
            </Select>
          )}
        </FormField>
      ) : null}

      {hasChoices(draft.fieldType, typeOptions) ? (
        <div className="sm:col-span-2">
          <ChoicesEditor
            labelPrefix={`${name} `}
            choices={draft.choices}
            onChange={(choices) => onChange({ ...draft, choices })}
          />
        </div>
      ) : null}

      {defaultDefinition ? (
        <div className={cn(defaultInput === "long_text" && "sm:col-span-2")}>
          <DefinitionFieldControl
            key={`${draft.fieldType}-default`}
            definition={defaultDefinition}
            idPrefix={`${prefix}-`}
            value={draft.defaultValue}
            onChange={(next) => onChange({ ...draft, defaultValue: toDefault(next) })}
          />
        </div>
      ) : null}

      {draft.fieldType === "repeater" ? (
        <div className="space-y-3 rounded-xl border border-border bg-muted/20 p-4 sm:col-span-2">
          <div className="space-y-1">
            <p className="text-sm font-medium">Sub-fields</p>
            <p className="text-xs leading-5 text-muted-foreground">
              Every row holds these, in this order. Loop them with{" "}
              <code className="font-mono">
                {`{% for row in widget.settings.${draft.developerName || "name"} %}`}
              </code>
              . A repeater cannot hold another repeater.
            </p>
          </div>
          <WidgetFieldsEditor
            fields={draft.subFields}
            onChange={(subFields) => onChange({ ...draft, subFields })}
            typeOptions={typeOptions}
            scope="row"
            idPrefix={`${prefix}-sub`}
          />
        </div>
      ) : null}
    </div>
  );
}

function toDefault(next: JsonValue): JsonValue {
  return next === "" ? null : next;
}

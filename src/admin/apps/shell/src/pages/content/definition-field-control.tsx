import { adminApi, type WidgetFieldTypeName } from "@raytha/api";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, ChevronDown, Plus, Rows3, Trash2 } from "lucide-react";
import { useRef, useState, type ReactNode } from "react";
import { Button, Checkbox, ColorInput, EmptyState, FileUpload, FormField, Input, Select, Textarea, cn } from "@raytha/ui";
import { RichTextEditor } from "../../components/rich-text-editor";
import { entityFields, readString } from "../entity";
import {
  readRows,
  type DefinitionFieldType,
  type DefinitionValue,
  type FieldDefinition,
  type RepeaterRow,
} from "./fields-model";
import { newId } from "./parse";
import { usePendingFocus } from "./use-pending-focus";

/** Content fields plus the widget-only types (image, content_type, view). */
export type ControlFieldType = DefinitionFieldType | WidgetFieldTypeName;

export type ControlDefinition = Omit<FieldDefinition, "fieldType" | "subFields"> & {
  fieldType: ControlFieldType;
  subFields: ControlDefinition[];
  /** For a view field, the sibling content_type field whose views it lists. */
  contentTypeField?: string;
};

export type DefinitionFieldControlProps = {
  definition: ControlDefinition;
  /** The stored JSON value: a scalar, or rows for a repeater. */
  value: unknown;
  onChange: (next: DefinitionValue) => void;
  /** Prefixes element ids so repeated copies of a field stay unique on the page. */
  idPrefix?: string;
  /** Values of the fields beside this one (the settings object, or the repeater row). */
  siblings?: Readonly<Record<string, unknown>>;
};

type ControlProps = {
  definition: ControlDefinition;
  id: string;
  label: string;
  hint?: string;
  value: unknown;
  onChange: (next: DefinitionValue) => void;
  siblings: Readonly<Record<string, unknown>>;
};

const NO_SIBLINGS: Readonly<Record<string, unknown>> = {};

/** Changing a content_type field empties every view field that lists its views. */
export function clearDependentViews<T extends Record<string, unknown>>(
  values: T,
  definitions: readonly ControlDefinition[],
  changedName: string,
): T {
  const dependents = definitions.filter(
    (definition) => definition.fieldType === "view" && definition.contentTypeField === changedName,
  );
  if (dependents.length === 0) {
    return values;
  }
  const next = { ...values };
  for (const dependent of dependents) {
    delete next[dependent.developerName];
  }
  return next;
}

const CONTROLS: Record<ControlFieldType, (props: ControlProps) => ReactNode> = {
  single_line_text: TextControl,
  long_text: LongTextControl,
  wysiwyg: WysiwygControl,
  number: NumberControl,
  checkbox: CheckboxControl,
  date: DateControl,
  dropdown: DropdownControl,
  radio: RadioControl,
  color: ColorControl,
  attachment: AttachmentControl,
  repeater: RepeaterControl,
  image: ImageControl,
  content_type: ContentTypeControl,
  view: ViewControl,
};

export const WIDE_TYPES = new Set<ControlFieldType>(["long_text", "wysiwyg", "radio", "attachment", "repeater", "image"]);

/** Renders one `FieldDefinition` against its raw JSON value. */
export function DefinitionFieldControl({
  definition,
  value,
  onChange,
  idPrefix = "",
  siblings = NO_SIBLINGS,
}: DefinitionFieldControlProps) {
  const Control = CONTROLS[definition.fieldType];
  return (
    <Control
      definition={definition}
      id={`${idPrefix}${definition.developerName}`}
      label={definition.label || definition.developerName}
      hint={definition.description || undefined}
      value={value}
      onChange={onChange}
      siblings={siblings}
    />
  );
}

function textOf(value: unknown): string {
  if (typeof value === "string") {
    return value;
  }
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  return "";
}

function RequiredMark({ required }: { required: boolean }) {
  return required ? (
    <span aria-hidden className="text-destructive">
      {" *"}
    </span>
  ) : null;
}

function TextControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => <Input {...control} value={textOf(value)} onChange={(event) => onChange(event.target.value)} />}
    </FormField>
  );
}

function LongTextControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => (
        <Textarea {...control} rows={4} value={textOf(value)} onChange={(event) => onChange(event.target.value)} />
      )}
    </FormField>
  );
}

function WysiwygControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {() => <RichTextEditor content={textOf(value)} onHtmlChange={onChange} ariaLabel={label} />}
    </FormField>
  );
}

function NumberControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => (
        <Input
          {...control}
          type="number"
          inputMode="decimal"
          value={textOf(value)}
          onChange={(event) => {
            const text = event.target.value.trim();
            if (text === "") {
              onChange(null);
              return;
            }
            const parsed = Number(text);
            onChange(Number.isFinite(parsed) && String(parsed) === text ? parsed : text);
          }}
        />
      )}
    </FormField>
  );
}

function CheckboxControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  return (
    <div className="flex flex-col gap-1.5 pt-1">
      <div className="flex items-center gap-2">
        <Checkbox
          id={id}
          checked={value === true}
          aria-describedby={hint ? `${id}-hint` : undefined}
          onCheckedChange={(checked) => onChange(checked)}
        />
        <label htmlFor={id} className="text-sm font-medium">
          {label}
          <RequiredMark required={definition.isRequired} />
        </label>
      </div>
      {hint ? (
        <p id={`${id}-hint`} className="text-xs leading-5 text-muted-foreground">
          {hint}
        </p>
      ) : null}
    </div>
  );
}

function DateControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const text = textOf(value);
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => (
        <Input
          {...control}
          type="date"
          value={/^\d{4}-\d{2}-\d{2}/.test(text) ? text.slice(0, 10) : text}
          onChange={(event) => onChange(event.target.value || null)}
        />
      )}
    </FormField>
  );
}

function DropdownControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const selected = textOf(value);
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => (
        <Select {...control} value={selected} onChange={(event) => onChange(event.target.value || null)}>
          <option value="">Select…</option>
          {definition.choices
            .filter((choice) => !choice.disabled || choice.developerName === selected)
            .map((choice) => (
              <option key={choice.developerName} value={choice.developerName}>
                {choice.label || choice.developerName}
              </option>
            ))}
        </Select>
      )}
    </FormField>
  );
}

function RadioControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const selected = textOf(value);
  return (
    <fieldset className="space-y-2" aria-describedby={hint ? `${id}-hint` : undefined}>
      <legend className="text-sm font-medium">
        {label}
        <RequiredMark required={definition.isRequired} />
      </legend>
      {hint ? (
        <p id={`${id}-hint`} className="text-xs leading-5 text-muted-foreground">
          {hint}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-x-5 gap-y-2">
        {definition.choices.map((choice) => {
          const choiceId = `${id}-${choice.developerName}`;
          return (
            <label key={choice.developerName} className="flex items-center gap-2 text-sm" htmlFor={choiceId}>
              <input
                id={choiceId}
                type="radio"
                name={id}
                checked={selected === choice.developerName}
                disabled={choice.disabled && selected !== choice.developerName}
                onChange={() => onChange(choice.developerName)}
                className="size-4 accent-brand-600"
              />
              {choice.label || choice.developerName}
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}

function ColorControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => (
        <ColorInput
          {...control}
          swatchLabel={`Pick ${label}`}
          value={textOf(value)}
          onChange={(next) => onChange(next || null)}
        />
      )}
    </FormField>
  );
}

function AttachmentControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const url = textOf(value);
  return (
    <div className="space-y-2" role="group" aria-labelledby={`${id}-label`}>
      <p id={`${id}-label`} className="text-sm font-medium">
        {label}
        <RequiredMark required={definition.isRequired} />
      </p>
      {hint ? <p className="text-xs leading-5 text-muted-foreground">{hint}</p> : null}
      {url ? (
        <div className="flex items-center gap-2 text-sm">
          <a href={url} className="truncate text-primary hover:underline" target="_blank" rel="noreferrer">
            Current file
          </a>
          <Button type="button" variant="ghost" size="sm" onClick={() => onChange(null)}>
            Clear
          </Button>
        </div>
      ) : null}
      <FileUpload
        height={160}
        onUploaded={(files) => {
          const first = files[0];
          if (first) {
            onChange(first.url || first.objectKey);
          }
        }}
      />
    </div>
  );
}

function ImageControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const url = textOf(value);
  return (
    <div className="space-y-2">
      <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
        {(control) => (
          <Input
            {...control}
            inputMode="url"
            placeholder="/_static-files/… or upload below"
            value={url}
            onChange={(event) => onChange(event.target.value)}
          />
        )}
      </FormField>
      {url ? <img src={url} alt="" className="max-h-32 rounded-lg border border-border object-contain" /> : null}
      <FileUpload
        height={160}
        allowedFileTypes={["image/*"]}
        note="Upload an image or paste a URL above."
        onUploaded={(files) => {
          const first = files[0];
          if (first) {
            onChange(first.url);
          }
        }}
      />
    </div>
  );
}

type Option = { value: string; label: string };

/** Keeps a stored value selectable when it is no longer offered, so opening a form never changes it. */
function withCurrent(options: Option[], selected: string, loaded: boolean): Option[] {
  if (!selected || options.some((option) => option.value === selected)) {
    return options;
  }
  return [{ value: selected, label: loaded ? `${selected} (not found)` : selected }, ...options];
}

function ContentTypeControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const selected = textOf(value);
  const types = useQuery({
    queryKey: ["content-types", "site-page-widget"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });
  const options = (types.data?.items ?? []).flatMap((type) => {
    const fields = entityFields(type);
    const developerName = readString(fields, "developerName");
    return developerName
      ? [{ value: developerName, label: readString(fields, "labelPlural", "labelSingular") || developerName }]
      : [];
  });
  return (
    <FormField label={label} required={definition.isRequired} hint={hint} htmlFor={id}>
      {(control) => (
        <Select {...control} value={selected} onChange={(event) => onChange(event.target.value || null)}>
          <option value="">Select a content type</option>
          {withCurrent(options, selected, types.isSuccess).map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      )}
    </FormField>
  );
}

function ViewControl({ definition, id, label, hint, value, onChange, siblings }: ControlProps) {
  const selected = textOf(value);
  const contentType = textOf(siblings[definition.contentTypeField ?? ""]);
  const views = useQuery({
    queryKey: ["content-views", contentType],
    queryFn: () => adminApi.views(contentType).list({ pageSize: 100 }),
    enabled: contentType.length > 0,
    placeholderData: keepPreviousData,
  });
  const options = contentType
    ? (views.data?.items ?? []).map((view) => ({
        value: view.id,
        label: readString(entityFields(view), "label", "developerName") || view.id,
      }))
    : [];
  return (
    <FormField
      label={label}
      required={definition.isRequired}
      hint={contentType ? hint : "Pick a content type first."}
      htmlFor={id}
    >
      {(control) => (
        <Select
          {...control}
          value={selected}
          disabled={!contentType && !selected}
          onChange={(event) => onChange(event.target.value || null)}
        >
          <option value="">Default view</option>
          {withCurrent(options, selected, views.isSuccess).map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      )}
    </FormField>
  );
}

function emptyRow(subFields: ControlDefinition[]): RepeaterRow {
  const row: RepeaterRow = {};
  for (const subField of subFields) {
    row[subField.developerName] =
      subField.fieldType === "checkbox" ? false : subField.fieldType === "repeater" ? [] : null;
  }
  return row;
}

function rowSummary(row: RepeaterRow, subFields: ControlDefinition[]): string {
  for (const subField of subFields) {
    const cell = row[subField.developerName];
    if (subField.fieldType === "single_line_text" && typeof cell === "string" && cell.trim()) {
      return cell.trim();
    }
  }
  return "";
}

const FOCUSABLE = "input:not([type=hidden]), textarea, select, [contenteditable=true]";

function RepeaterControl({ definition, id, label, hint, value, onChange }: ControlProps) {
  const rows = readRows(value);
  const [keys, setKeys] = useState<string[]>(() => rows.map(() => newId()));
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(() => new Set());
  const rootRef = useRef<HTMLDivElement>(null);
  const focusAfterRender = usePendingFocus();

  if (keys.length !== rows.length) {
    setKeys(rows.map((_, index) => keys[index] ?? newId()));
  }

  const subFields = definition.subFields;
  const rowElement = (key: string) =>
    rootRef.current?.querySelector<HTMLElement>(`:scope > ol > li[data-row-key="${key}"]`);
  const addButton = () => rootRef.current?.querySelector<HTMLElement>("[data-repeater-add]");

  const commit = (nextRows: RepeaterRow[], nextKeys: string[]) => {
    setKeys(nextKeys);
    onChange(nextRows);
  };

  const addRow = () => {
    const key = newId();
    focusAfterRender(() => rowElement(key)?.querySelector<HTMLElement>(FOCUSABLE));
    commit([...rows, emptyRow(subFields)], [...keys, key]);
  };

  const removeRow = (index: number) => {
    const neighbour = keys[index + 1] ?? keys[index - 1];
    focusAfterRender(
      () =>
        (neighbour ? rowElement(neighbour)?.querySelector<HTMLElement>('[data-row-action="remove"]') : null) ??
        addButton(),
    );
    commit(
      rows.filter((_, i) => i !== index),
      keys.filter((_, i) => i !== index),
    );
  };

  const moveRow = (index: number, offset: -1 | 1) => {
    const target = index + offset;
    if (target < 0 || target >= rows.length) {
      return;
    }
    const nextRows = [...rows];
    const nextKeys = [...keys];
    [nextRows[index], nextRows[target]] = [nextRows[target]!, nextRows[index]!];
    [nextKeys[index], nextKeys[target]] = [nextKeys[target]!, nextKeys[index]!];
    const key = keys[index]!;
    const action = offset < 0 ? "up" : "down";
    focusAfterRender(
      () =>
        rowElement(key)?.querySelector<HTMLElement>(`[data-row-action="${action}"]:not(:disabled)`) ??
        rowElement(key)?.querySelector<HTMLElement>("[data-row-action]:not(:disabled)"),
    );
    commit(nextRows, nextKeys);
  };

  const setCell = (index: number, developerName: string, next: DefinitionValue) => {
    onChange(
      rows.map((row, i) =>
        i === index ? clearDependentViews({ ...row, [developerName]: next }, subFields, developerName) : row,
      ),
    );
  };

  const toggle = (key: string) => {
    setCollapsed((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  const noun = `${rows.length} ${rows.length === 1 ? "row" : "rows"}`;

  return (
    <div
      ref={rootRef}
      role="group"
      aria-labelledby={`${id}-label`}
      aria-describedby={hint ? `${id}-hint` : undefined}
      className="space-y-3"
    >
      <div className="flex flex-wrap items-center gap-2">
        <span id={`${id}-label`} className="text-sm font-medium">
          {label}
          <RequiredMark required={definition.isRequired} />
        </span>
        {rows.length > 0 ? (
          <span className="rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">{noun}</span>
        ) : null}
      </div>
      {hint ? (
        <p id={`${id}-hint`} className="-mt-1 text-xs leading-5 text-muted-foreground">
          {hint}
        </p>
      ) : null}

      {subFields.length === 0 ? (
        <p className="rounded-xl border border-dashed border-border-strong px-4 py-6 text-center text-sm text-muted-foreground">
          This repeater has no sub-fields yet. Add them in the field settings.
        </p>
      ) : rows.length === 0 ? (
        <EmptyState
          icon={Rows3}
          className="py-8"
          title={`No ${label} rows yet`}
          hint={`Each row holds ${subFields.map((subField) => subField.label || subField.developerName).join(", ")}.`}
          action={
            <Button type="button" size="sm" data-repeater-add onClick={addRow}>
              <Plus className="size-4" aria-hidden />
              Add the first row
            </Button>
          }
        />
      ) : (
        <ol className="space-y-3">
          {rows.map((row, index) => {
            const key = keys[index] ?? String(index);
            const isCollapsed = collapsed.has(key);
            const summary = rowSummary(row, subFields);
            const bodyId = `${id}-${key}-body`;
            return (
              <li
                key={key}
                data-row-key={key}
                className="overflow-hidden rounded-xl border border-border bg-card shadow-xs transition-shadow focus-within:shadow-card"
              >
                <div
                  className={cn(
                    "flex items-center gap-1 bg-muted/40 py-1.5 pr-1.5 pl-2",
                    !isCollapsed && "border-b border-border",
                  )}
                >
                  <button
                    type="button"
                    aria-expanded={!isCollapsed}
                    aria-controls={bodyId}
                    onClick={() => toggle(key)}
                    className="flex min-w-0 flex-1 items-center gap-2 rounded-md px-1 py-1 text-left text-sm outline-none focus-visible:ring-[3px] focus-visible:ring-brand-500/25"
                  >
                    <ChevronDown
                      aria-hidden
                      className={cn("size-4 shrink-0 text-muted-foreground transition-transform", isCollapsed && "-rotate-90")}
                    />
                    <span className="inline-flex size-6 shrink-0 items-center justify-center rounded-md bg-brand-50 text-xs font-semibold text-brand-700 tabular-nums">
                      {index + 1}
                    </span>
                    <span className={cn("truncate", summary ? "font-medium" : "text-muted-foreground")}>
                      {summary || `Row ${index + 1}`}
                    </span>
                  </button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    data-row-action="up"
                    aria-label={`Move row ${index + 1} up`}
                    disabled={index === 0}
                    onClick={() => moveRow(index, -1)}
                  >
                    <ArrowUp className="size-4" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    data-row-action="down"
                    aria-label={`Move row ${index + 1} down`}
                    disabled={index === rows.length - 1}
                    onClick={() => moveRow(index, 1)}
                  >
                    <ArrowDown className="size-4" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8 text-muted-foreground hover:text-destructive"
                    data-row-action="remove"
                    aria-label={`Remove row ${index + 1}`}
                    onClick={() => removeRow(index)}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </Button>
                </div>
                <div id={bodyId} hidden={isCollapsed} className="grid gap-4 p-4 sm:grid-cols-2">
                  {subFields.map((subField) => (
                    <div
                      key={subField.developerName}
                      className={WIDE_TYPES.has(subField.fieldType) ? "sm:col-span-2" : undefined}
                    >
                      <DefinitionFieldControl
                        definition={subField}
                        value={row[subField.developerName]}
                        onChange={(next) => setCell(index, subField.developerName, next)}
                        idPrefix={`${id}-${key}-`}
                        siblings={row}
                      />
                    </div>
                  ))}
                </div>
              </li>
            );
          })}
        </ol>
      )}

      {subFields.length > 0 && rows.length > 0 ? (
        <Button type="button" variant="outline" size="sm" data-repeater-add onClick={addRow}>
          <Plus className="size-4" aria-hidden />
          Add row
        </Button>
      ) : null}
    </div>
  );
}

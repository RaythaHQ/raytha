import { Button, Combobox, Input, Select } from "@raytha/ui";
import { useState } from "react";
import type { ContentField, FieldChoice, FieldTypeName, RelationshipField } from "./fields-model";
import { useRelatedItemSearch } from "./relationship-picker";

type FilterValueKind = "text" | "number" | "date" | "choice" | "relationship" | "color";

const KIND_BY_FIELD_TYPE: Partial<Record<FieldTypeName | "id" | "color", FilterValueKind>> = {
  number: "number",
  date: "date",
  radio: "choice",
  dropdown: "choice",
  multiple_select: "choice",
  one_to_one_relationship: "relationship",
  color: "color",
};

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;
const HEX_COLOR = /^#[0-9a-f]{6}$/i;

type FilterValueControl =
  | { kind: "text" | "number" | "date" | "color" }
  | { kind: "choice"; choices: FieldChoice[] }
  | { kind: "relationship"; field: RelationshipField };

function filterValueControl(fieldType: FieldTypeName | "id", field: ContentField | undefined): FilterValueControl {
  const kind = KIND_BY_FIELD_TYPE[fieldType] ?? "text";
  if (kind === "choice") {
    return field && "choices" in field ? { kind, choices: field.choices } : { kind: "text" };
  }
  if (kind === "relationship") {
    return field?.fieldType === "one_to_one_relationship" ? { kind, field } : { kind: "text" };
  }
  return { kind };
}

/** The value control for one filter condition, chosen by the filtered field's type. */
export function FilterValueInput({
  fieldType,
  field,
  value,
  onChange,
}: {
  fieldType: FieldTypeName | "id";
  field: ContentField | undefined;
  value: string;
  onChange: (next: string) => void;
}) {
  const control = filterValueControl(fieldType, field);
  switch (control.kind) {
    case "choice":
      return (
        <Select aria-label="Value" value={value} onChange={(event) => onChange(event.target.value)}>
          <option value="">Choose value…</option>
          {value && !control.choices.some((choice) => choice.developerName === value) ? (
            <option value={value}>{value}</option>
          ) : null}
          {control.choices.map((choice) => (
            <option key={choice.developerName} value={choice.developerName}>
              {choice.label || choice.developerName}
            </option>
          ))}
        </Select>
      );
    case "date":
      return <DateValue value={value} onChange={onChange} />;
    case "number":
      return (
        <Input
          aria-label="Value"
          type="number"
          inputMode="decimal"
          placeholder="0"
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      );
    case "relationship":
      return <RelationshipValue field={control.field} value={value} onChange={onChange} />;
    case "color":
      return (
        <div className="flex items-center gap-2">
          <input
            type="color"
            aria-label="Value swatch"
            value={HEX_COLOR.test(value) ? value.toLowerCase() : "#000000"}
            onChange={(event) => onChange(event.target.value)}
            className="h-9 w-11 shrink-0 cursor-pointer rounded-lg border border-input bg-card p-1 shadow-xs"
          />
          <Input
            aria-label="Value"
            placeholder="#rrggbb"
            spellCheck={false}
            maxLength={7}
            value={value}
            aria-invalid={value !== "" && !HEX_COLOR.test(value) ? true : undefined}
            onChange={(event) => onChange(event.target.value.trim())}
            className="font-mono"
          />
        </div>
      );
    case "text":
      return (
        <Input aria-label="Value" placeholder="Value" value={value} onChange={(event) => onChange(event.target.value)} />
      );
    default: {
      const _exhaustive: never = control;
      return _exhaustive;
    }
  }
}

function DateValue({ value, onChange }: { value: string; onChange: (next: string) => void }) {
  const [legacyText, setLegacyText] = useState(() => value !== "" && !ISO_DATE.test(value));
  if (!legacyText) {
    return <Input aria-label="Value" type="date" value={value} onChange={(event) => onChange(event.target.value)} />;
  }
  return (
    <div className="flex items-center gap-2">
      <Input aria-label="Value" value={value} onChange={(event) => onChange(event.target.value)} />
      <Button
        type="button"
        size="sm"
        variant="ghost"
        onClick={() => {
          setLegacyText(false);
          onChange("");
        }}
      >
        Pick date
      </Button>
    </div>
  );
}

function RelationshipValue({
  field,
  value,
  onChange,
}: {
  field: RelationshipField;
  value: string;
  onChange: (next: string) => void;
}) {
  const { options, loading } = useRelatedItemSearch(field.relatedContentTypeId, value);
  return (
    <Combobox
      aria-label="Value"
      inputValue={value}
      onInputValueChange={onChange}
      options={options}
      loading={loading}
      placeholder="Search related items"
      emptyText={value.trim() ? `No items match “${value.trim()}”` : "No items yet"}
      onSelect={(option) => onChange(option.label)}
    />
  );
}

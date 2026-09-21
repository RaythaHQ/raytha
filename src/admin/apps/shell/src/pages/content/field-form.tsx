import type { JsonObject } from "@raytha/api";
import { Button, Checkbox, FormField, Input, Select, Textarea } from "@raytha/ui";
import { toDeveloperName } from "../entity";
import {
  hasChoices,
  isFieldTypeName,
  isRelationship,
  parseFieldTypeOptions,
  parseNamedRefs,
  type ContentField,
  type FieldChoice,
  type FieldTypeName,
} from "./fields-model";

export type FieldForm = {
  label: string;
  developerName: string;
  description: string;
  isRequired: boolean;
  fieldType: FieldTypeName;
  relatedContentTypeId: string;
  choices: FieldChoice[];
};

export function emptyFieldForm(): FieldForm {
  return {
    label: "",
    developerName: "",
    description: "",
    isRequired: false,
    fieldType: "single_line_text",
    relatedContentTypeId: "",
    choices: [{ label: "", developerName: "", disabled: false }],
  };
}

export function formFromField(field: ContentField): FieldForm {
  return {
    label: field.label,
    developerName: field.developerName,
    description: field.description,
    isRequired: field.isRequired,
    fieldType: field.fieldType,
    relatedContentTypeId: field.fieldType === "one_to_one_relationship" ? field.relatedContentTypeId : "",
    choices:
      field.fieldType === "dropdown" || field.fieldType === "radio" || field.fieldType === "multiple_select"
        ? field.choices.length > 0
          ? field.choices
          : [{ label: "", developerName: "", disabled: false }]
        : [{ label: "", developerName: "", disabled: false }],
  };
}

export function createFieldPayload(form: FieldForm): JsonObject {
  const payload: JsonObject = {
    fieldType: form.fieldType,
    developerName: form.developerName,
    label: form.label,
    isRequired: form.isRequired,
    description: form.description,
    choices: hasChoices(form.fieldType)
      ? form.choices.map((choice) => ({
          label: choice.label,
          developerName: choice.developerName,
          disabled: choice.disabled,
        }))
      : [],
  };
  if (isRelationship(form.fieldType) && form.relatedContentTypeId) {
    payload.relatedContentTypeId = form.relatedContentTypeId;
  }
  return payload;
}

export function editFieldPayload(form: FieldForm): JsonObject {
  return {
    label: form.label,
    isRequired: form.isRequired,
    description: form.description,
    choices: hasChoices(form.fieldType)
      ? form.choices.map((choice) => ({
          label: choice.label,
          developerName: choice.developerName,
          disabled: choice.disabled,
        }))
      : [],
  };
}

export function FieldFormFields({
  form,
  setForm,
  fieldTypes,
  relatedTypes,
  developerLocked,
  developerTouched,
  setDeveloperTouched,
}: {
  form: FieldForm;
  setForm: (next: FieldForm) => void;
  fieldTypes: ReturnType<typeof parseFieldTypeOptions>;
  relatedTypes: ReturnType<typeof parseNamedRefs>;
  developerLocked: boolean;
  developerTouched: boolean;
  setDeveloperTouched: (next: boolean) => void;
}) {
  return (
    <>
      <FormField label="Label" required htmlFor="field-label">
        {(control) => (
          <Input
            {...control}
            value={form.label}
            onChange={(event) => {
              const label = event.target.value;
              setForm({
                ...form,
                label,
                developerName: developerLocked || developerTouched ? form.developerName : toDeveloperName(label),
              });
            }}
          />
        )}
      </FormField>
      <FormField label="Developer name" required htmlFor="field-developer-name">
        {(control) => (
          <Input
            {...control}
            value={form.developerName}
            disabled={developerLocked}
            onChange={(event) => {
              setDeveloperTouched(true);
              setForm({ ...form, developerName: event.target.value });
            }}
          />
        )}
      </FormField>
      <FormField label="Field type" required htmlFor="field-type">
        {(control) => (
          <Select
            {...control}
            value={form.fieldType}
            disabled={developerLocked}
            onChange={(event) => {
              const next = event.target.value;
              if (isFieldTypeName(next)) {
                setForm({ ...form, fieldType: next });
              }
            }}
          >
            {(fieldTypes.length > 0 ? fieldTypes : fallbackFieldTypes()).map((option) => (
              <option key={option.developerName} value={option.developerName}>
                {option.label}
              </option>
            ))}
          </Select>
        )}
      </FormField>
      <FormField label="Description" htmlFor="field-description">
        {(control) => (
          <Textarea
            {...control}
            value={form.description}
            onChange={(event) => setForm({ ...form, description: event.target.value })}
          />
        )}
      </FormField>
      <div className="flex items-center gap-2">
        <Checkbox
          id="field-required"
          checked={form.isRequired}
          onCheckedChange={(checked) => setForm({ ...form, isRequired: checked })}
        />
        <label htmlFor="field-required" className="text-sm">
          Required
        </label>
      </div>
      {hasChoices(form.fieldType) ? (
        <ChoicesEditor choices={form.choices} onChange={(choices) => setForm({ ...form, choices })} />
      ) : null}
      {isRelationship(form.fieldType) ? (
        <FormField label="Related content type" required htmlFor="field-related-type">
          {(control) => (
            <Select
              {...control}
              value={form.relatedContentTypeId}
              onChange={(event) => setForm({ ...form, relatedContentTypeId: event.target.value })}
            >
              <option value="">Select a content type</option>
              {relatedTypes.map((type) => (
                <option key={type.id} value={type.id}>
                  {type.label || type.developerName}
                </option>
              ))}
            </Select>
          )}
        </FormField>
      ) : null}
    </>
  );
}

function ChoicesEditor({
  choices,
  onChange,
}: {
  choices: FieldChoice[];
  onChange: (choices: FieldChoice[]) => void;
}) {
  return (
    <div className="space-y-3">
      <p className="text-sm font-medium">Choices</p>
      {choices.map((choice, index) => (
        <div key={index} className="grid gap-2 rounded-lg border border-border p-3 sm:grid-cols-[1fr_1fr_auto_auto]">
          <Input
            aria-label={`Choice ${index + 1} label`}
            placeholder="Label"
            value={choice.label}
            onChange={(event) => {
              const label = event.target.value;
              const next = choices.slice();
              const current = next[index];
              if (!current) {
                return;
              }
              next[index] = {
                ...current,
                label,
                developerName:
                  current.developerName === "" || current.developerName === toDeveloperName(current.label)
                    ? toDeveloperName(label)
                    : current.developerName,
              };
              onChange(next);
            }}
          />
          <Input
            aria-label={`Choice ${index + 1} developer name`}
            placeholder="Developer name"
            value={choice.developerName}
            onChange={(event) => {
              const next = choices.slice();
              const current = next[index];
              if (!current) {
                return;
              }
              next[index] = { ...current, developerName: event.target.value };
              onChange(next);
            }}
          />
          <label className="flex items-center gap-2 text-sm">
            <Checkbox
              checked={choice.disabled}
              onCheckedChange={(checked) => {
                const next = choices.slice();
                const current = next[index];
                if (!current) {
                  return;
                }
                next[index] = { ...current, disabled: checked };
                onChange(next);
              }}
            />
            Disabled
          </label>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={choices.length === 1}
            onClick={() => onChange(choices.filter((_, choiceIndex) => choiceIndex !== index))}
          >
            Remove
          </Button>
        </div>
      ))}
      <Button
        type="button"
        size="sm"
        variant="outline"
        onClick={() => onChange([...choices, { label: "", developerName: "", disabled: false }])}
      >
        Add choice
      </Button>
    </div>
  );
}

function fallbackFieldTypes(): ReturnType<typeof parseFieldTypeOptions> {
  return [
    { label: "Single line text", developerName: "single_line_text" },
    { label: "Long text", developerName: "long_text" },
    { label: "Wysiwyg", developerName: "wysiwyg" },
    { label: "Radio", developerName: "radio" },
    { label: "Dropdown", developerName: "dropdown" },
    { label: "Checkbox", developerName: "checkbox" },
    { label: "Multiple select", developerName: "multiple_select" },
    { label: "Date", developerName: "date" },
    { label: "Number", developerName: "number" },
    { label: "Attachment", developerName: "attachment" },
    { label: "One to one relationship", developerName: "one_to_one_relationship" },
  ];
}

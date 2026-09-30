import type { JsonObject } from "@raytha/api";
import { Checkbox, FormField, Input, Select, Textarea } from "@raytha/ui";
import { toDeveloperName } from "../entity";
import { ChoicesEditor } from "./choices-editor";
import type { parseFieldTypeOptions, parseNamedRefs } from "./fields-model";
import {
  hasChoices,
  isFieldTypeName,
  isRelationship,
  isRepeater,
  type ContentField,
  type FieldChoice,
  type FieldTypeName,
} from "./fields-model";
import { SubFieldsEditor, subFieldDrafts, subFieldsPayload, type SubFieldDraft } from "./sub-fields-editor";

export type FieldForm = {
  label: string;
  developerName: string;
  description: string;
  isRequired: boolean;
  fieldType: FieldTypeName;
  relatedContentTypeId: string;
  choices: FieldChoice[];
  subFields: SubFieldDraft[];
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
    subFields: [],
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
    subFields: field.fieldType === "repeater" ? subFieldDrafts(field.subFields) : [],
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
  if (isRepeater(form.fieldType)) {
    payload.subFields = subFieldsPayload(form.subFields);
  }
  return payload;
}

export function editFieldPayload(form: FieldForm): JsonObject {
  const payload: JsonObject = {
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
  if (isRepeater(form.fieldType)) {
    payload.subFields = subFieldsPayload(form.subFields);
  }
  return payload;
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
  const typeOptions = fieldTypes.length > 0 ? fieldTypes : fallbackFieldTypes();
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
            {typeOptions.map((option) => (
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
      {isRepeater(form.fieldType) ? (
        <SubFieldsEditor
          subFields={form.subFields}
          onChange={(subFields) => setForm({ ...form, subFields })}
          fieldTypes={typeOptions}
          fieldDeveloperName={form.developerName}
        />
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
    { label: "Color", developerName: "color" },
    { label: "Repeater", developerName: "repeater" },
  ];
}

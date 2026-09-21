import { adminApi } from "@raytha/api";
import { Checkbox, FileUpload, FormField, Input, Select, Textarea } from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { RichTextEditor } from "../../components/rich-text-editor";
import { entityFields, readString } from "../entity";
import {
  type ChoiceField,
  type ContentField,
  type ContentFieldValue,
  type RelationshipField,
} from "./fields-model";

export function ContentFieldControl({
  field,
  value,
  onChange,
}: {
  field: ContentField;
  value: ContentFieldValue;
  onChange: (next: ContentFieldValue) => void;
}) {
  const label = field.label || field.developerName;
  const hint = field.description || undefined;

  switch (field.fieldType) {
    case "single_line_text":
      if (value.fieldType !== "single_line_text") {
        return null;
      }
      return (
        <FormField label={label} required={field.isRequired} hint={hint} htmlFor={field.developerName}>
          {(control) => (
            <Input
              {...control}
              value={value.value}
              onChange={(event) => onChange({ fieldType: "single_line_text", value: event.target.value })}
            />
          )}
        </FormField>
      );
    case "long_text":
      if (value.fieldType !== "long_text") {
        return null;
      }
      return (
        <FormField label={label} required={field.isRequired} hint={hint} htmlFor={field.developerName}>
          {(control) => (
            <Textarea
              {...control}
              rows={6}
              value={value.value}
              onChange={(event) => onChange({ fieldType: "long_text", value: event.target.value })}
            />
          )}
        </FormField>
      );
    case "wysiwyg":
      if (value.fieldType !== "wysiwyg") {
        return null;
      }
      return (
        <FormField label={label} required={field.isRequired} hint={hint} htmlFor={field.developerName}>
          {() => (
            <RichTextEditor
              content={value.value}
              onHtmlChange={(html) => onChange({ fieldType: "wysiwyg", value: html })}
              ariaLabel={label}
            />
          )}
        </FormField>
      );
    case "checkbox":
      if (value.fieldType !== "checkbox") {
        return null;
      }
      return (
        <div className="flex items-center gap-2">
          <Checkbox
            id={field.developerName}
            checked={value.value}
            onCheckedChange={(checked) => onChange({ fieldType: "checkbox", value: checked })}
          />
          <label htmlFor={field.developerName} className="text-sm">
            {label}
            {field.isRequired ? <span className="text-destructive"> *</span> : null}
          </label>
        </div>
      );
    case "radio":
      if (value.fieldType !== "radio") {
        return null;
      }
      return (
        <RadioChoices
          field={field}
          value={value.value}
          onChange={(next) => onChange({ fieldType: "radio", value: next })}
        />
      );
    case "dropdown":
      if (value.fieldType !== "dropdown") {
        return null;
      }
      return (
        <FormField label={label} required={field.isRequired} hint={hint} htmlFor={field.developerName}>
          {(control) => (
            <Select
              {...control}
              value={value.value}
              onChange={(event) => onChange({ fieldType: "dropdown", value: event.target.value })}
            >
              <option value="">Select…</option>
              {field.choices
                .filter((choice) => !choice.disabled || choice.developerName === value.value)
                .map((choice) => (
                  <option key={choice.developerName} value={choice.developerName}>
                    {choice.label || choice.developerName}
                  </option>
                ))}
            </Select>
          )}
        </FormField>
      );
    case "multiple_select":
      if (value.fieldType !== "multiple_select") {
        return null;
      }
      return (
        <MultipleChoices
          field={field}
          value={value.value}
          onChange={(next) => onChange({ fieldType: "multiple_select", value: next })}
        />
      );
    case "date":
      if (value.fieldType !== "date") {
        return null;
      }
      return (
        <FormField label={label} required={field.isRequired} hint={hint} htmlFor={field.developerName}>
          {(control) => (
            <Input
              {...control}
              type="date"
              value={value.value}
              onChange={(event) => onChange({ fieldType: "date", value: event.target.value })}
            />
          )}
        </FormField>
      );
    case "number":
      if (value.fieldType !== "number") {
        return null;
      }
      return (
        <FormField label={label} required={field.isRequired} hint={hint} htmlFor={field.developerName}>
          {(control) => (
            <Input
              {...control}
              type="number"
              value={value.value}
              onChange={(event) => onChange({ fieldType: "number", value: event.target.value })}
            />
          )}
        </FormField>
      );
    case "attachment":
      if (value.fieldType !== "attachment") {
        return null;
      }
      return (
        <div className="space-y-2">
          <p className="text-sm font-medium">
            {label}
            {field.isRequired ? <span className="text-destructive"> *</span> : null}
          </p>
          {hint ? <p className="text-xs text-muted-foreground">{hint}</p> : null}
          {value.value ? (
            <div className="flex items-center gap-2 text-sm">
              <a href={value.value} className="text-primary hover:underline" target="_blank" rel="noreferrer">
                Current file
              </a>
              <button
                type="button"
                className="text-muted-foreground hover:text-foreground"
                onClick={() => onChange({ fieldType: "attachment", value: "" })}
              >
                Clear
              </button>
            </div>
          ) : null}
          <FileUpload
            height={220}
            onUploaded={(files) => {
              const first = files[0];
              if (first) {
                onChange({ fieldType: "attachment", value: first.url || first.objectKey });
              }
            }}
          />
        </div>
      );
    case "one_to_one_relationship":
      if (value.fieldType !== "one_to_one_relationship") {
        return null;
      }
      return (
        <RelationshipPicker
          field={field}
          value={value.value}
          onChange={(next) => onChange({ fieldType: "one_to_one_relationship", value: next })}
        />
      );
    default: {
      const _exhaustive: never = field;
      return _exhaustive;
    }
  }
}

function RadioChoices({
  field,
  value,
  onChange,
}: {
  field: ChoiceField;
  value: string;
  onChange: (next: string) => void;
}) {
  const label = field.label || field.developerName;
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">
        {label}
        {field.isRequired ? <span className="text-destructive"> *</span> : null}
      </legend>
      {field.description ? <p className="text-xs text-muted-foreground">{field.description}</p> : null}
      <div className="space-y-2">
        {field.choices.map((choice) => {
          const id = `${field.developerName}-${choice.developerName}`;
          return (
            <label key={choice.developerName} className="flex items-center gap-2 text-sm" htmlFor={id}>
              <input
                id={id}
                type="radio"
                name={field.developerName}
                checked={value === choice.developerName}
                disabled={choice.disabled && value !== choice.developerName}
                onChange={() => onChange(choice.developerName)}
                className="size-4"
              />
              {choice.label || choice.developerName}
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}

function MultipleChoices({
  field,
  value,
  onChange,
}: {
  field: ChoiceField;
  value: string[];
  onChange: (next: string[]) => void;
}) {
  const label = field.label || field.developerName;
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">
        {label}
        {field.isRequired ? <span className="text-destructive"> *</span> : null}
      </legend>
      {field.description ? <p className="text-xs text-muted-foreground">{field.description}</p> : null}
      <div className="space-y-2">
        {field.choices.map((choice) => {
          const id = `${field.developerName}-${choice.developerName}`;
          const checked = value.includes(choice.developerName);
          return (
            <label key={choice.developerName} className="flex items-center gap-2 text-sm" htmlFor={id}>
              <Checkbox
                id={id}
                checked={checked}
                disabled={choice.disabled && !checked}
                onCheckedChange={(next) => {
                  const set = new Set(value);
                  if (next) {
                    set.add(choice.developerName);
                  } else {
                    set.delete(choice.developerName);
                  }
                  onChange([...set]);
                }}
              />
              {choice.label || choice.developerName}
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}

function RelationshipPicker({
  field,
  value,
  onChange,
}: {
  field: RelationshipField;
  value: string;
  onChange: (next: string) => void;
}) {
  const [search, setSearch] = useState("");
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
    placeholderData: keepPreviousData,
  });
  const relatedDeveloperName = relatedTypeDeveloperName(typesQuery.data?.items, field.relatedContentTypeId);
  const itemsQuery = useQuery({
    queryKey: ["content-items", relatedDeveloperName, "rel", search],
    queryFn: () => adminApi.contentItems(relatedDeveloperName).list({ search: search || undefined, pageSize: 20 }),
    enabled: relatedDeveloperName.length > 0,
    placeholderData: keepPreviousData,
  });

  const items = itemsQuery.data?.items ?? [];

  return (
    <FormField
      label={field.label || field.developerName}
      required={field.isRequired}
      hint={field.description || "Search items of the related content type."}
      htmlFor={field.developerName}
    >
      {(control) => (
        <div className="space-y-2">
          <Input
            {...control}
            value={search}
            placeholder="Search related items"
            onChange={(event) => setSearch(event.target.value)}
          />
          <Select value={value} onChange={(event) => onChange(event.target.value)}>
            <option value="">None</option>
            {value && !items.some((item) => item.id === value) ? <option value={value}>{value}</option> : null}
            {items.map((item) => (
              <option key={item.id} value={item.id}>
                {readString(entityFields(item), "primaryField", "title") || item.id}
              </option>
            ))}
          </Select>
        </div>
      )}
    </FormField>
  );
}

function relatedTypeDeveloperName(items: { id: string }[] | undefined, relatedId: string): string {
  if (!items || !relatedId) {
    return "";
  }
  for (const item of items) {
    if (item.id === relatedId) {
      return readString(entityFields(item), "developerName");
    }
  }
  return "";
}

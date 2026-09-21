import { adminApi, formatError } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import {
  Button,
  Checkbox,
  ConfirmDialog,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Textarea,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import { useMemo, useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { toDeveloperName } from "../entity";
import {
  fieldTypeLabel,
  hasChoices,
  isFieldTypeName,
  isRelationship,
  parseContentFields,
  parseContentTypeSummary,
  parseFieldTypeOptions,
  parseNamedRefs,
  type ContentField,
  type FieldChoice,
  type FieldTypeName,
} from "./fields-model";
import { ContentTypeNav } from "./nav";

type FieldForm = {
  label: string;
  developerName: string;
  description: string;
  isRequired: boolean;
  fieldType: FieldTypeName;
  relatedContentTypeId: string;
  choices: FieldChoice[];
};

function emptyForm(): FieldForm {
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

function formFromField(field: ContentField): FieldForm {
  return {
    label: field.label,
    developerName: field.developerName,
    description: field.description,
    isRequired: field.isRequired,
    fieldType: field.fieldType,
    relatedContentTypeId: field.fieldType === "one_to_one_relationship" ? field.relatedContentTypeId : "",
    choices: field.fieldType === "dropdown" || field.fieldType === "radio" || field.fieldType === "multiple_select"
      ? field.choices.length > 0
        ? field.choices
        : [{ label: "", developerName: "", disabled: false }]
      : [{ label: "", developerName: "", disabled: false }],
  };
}

export function ContentTypeFieldsPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Fields", developerName]);
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [editField, setEditField] = useState<ContentField | null>(null);
  const [deleteField, setDeleteField] = useState<ContentField | null>(null);
  const [form, setForm] = useState<FieldForm>(emptyForm);
  const [developerTouched, setDeveloperTouched] = useState(false);

  const fieldsApi = adminApi.fields(developerName);

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const fieldsQuery = useQuery({
    queryKey: ["content-fields", developerName],
    queryFn: () => fieldsApi.list({ pageSize: 200, orderBy: "FieldOrder asc" }),
    enabled: developerName.length > 0,
    placeholderData: keepPreviousData,
  });
  const fieldTypesQuery = useQuery({
    queryKey: ["content-field-types"],
    queryFn: () => adminApi.contentTypes.fieldTypes(),
  });
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
  });

  const contentType = parseContentTypeSummary(typeQuery.data);
  const fields = useMemo(() => {
    const fromList = parseContentFields(fieldsQuery.data?.items);
    if (fromList.length > 0) {
      return fromList;
    }
    return contentType?.fields ?? [];
  }, [fieldsQuery.data, contentType]);
  const fieldTypes = parseFieldTypeOptions(fieldTypesQuery.data);
  const relatedTypes = parseNamedRefs(typesQuery.data?.items);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-fields", developerName] });
    void queryClient.invalidateQueries({ queryKey: ["content-type", developerName] });
  };

  const createMutation = useMutation({
    mutationFn: (input: JsonObject) => fieldsApi.create(input),
    onSuccess: () => {
      toast.success("Field created");
      setCreateOpen(false);
      setForm(emptyForm());
      setDeveloperTouched(false);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const editMutation = useMutation({
    mutationFn: ({ id, input }: { id: string; input: JsonObject }) => fieldsApi.update(id, input),
    onSuccess: () => {
      toast.success("Field updated");
      setEditField(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => fieldsApi.remove(id),
    onSuccess: () => {
      toast.success("Field deleted");
      setDeleteField(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const reorderMutation = useMutation({
    mutationFn: ({ id, newFieldOrder }: { id: string; newFieldOrder: number }) =>
      fieldsApi.reorder(id, newFieldOrder),
    onSuccess: () => invalidate(),
    onError: (error) => toast.error(formatError(error)),
  });

  const openCreate = () => {
    setForm(emptyForm());
    setDeveloperTouched(false);
    setCreateOpen(true);
  };

  const openEdit = (field: ContentField) => {
    setForm(formFromField(field));
    setEditField(field);
  };

  const handleCreate = (event: FormEvent) => {
    event.preventDefault();
    createMutation.mutate(createPayload(form));
  };

  const handleEdit = (event: FormEvent) => {
    event.preventDefault();
    if (!editField) {
      return;
    }
    editMutation.mutate({ id: editField.id, input: editPayload(form) });
  };

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Fields" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${contentType?.labelPlural || developerName} fields`}
        description="Define the fields editors fill in for each item."
        actions={
          <Button type="button" onClick={openCreate}>
            New field
          </Button>
        }
      />
      <ContentTypeNav developerName={developerName} />
      <QueryGate query={fieldsQuery}>
        {() =>
          fields.length === 0 ? (
            <EmptyState icon={Inbox} title="No fields" hint="Add a field to start collecting content." />
          ) : (
            <Table aria-label="Fields">
              <TableHeader>
                <TableRow>
                  <TableHead>Label</TableHead>
                  <TableHead>Developer name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Required</TableHead>
                  <TableHead className="w-56">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {fields.map((field, index) => (
                  <TableRow key={field.id}>
                    <TableCell>{field.label}</TableCell>
                    <TableCell className="font-mono text-xs">{field.developerName}</TableCell>
                    <TableCell>{fieldTypeLabel(field.fieldType, fieldTypes)}</TableCell>
                    <TableCell>{field.isRequired ? "Yes" : "No"}</TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={index === 0 || reorderMutation.isPending}
                          onClick={() =>
                            reorderMutation.mutate({ id: field.id, newFieldOrder: field.fieldOrder - 1 })
                          }
                        >
                          Up
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={index === fields.length - 1 || reorderMutation.isPending}
                          onClick={() =>
                            reorderMutation.mutate({ id: field.id, newFieldOrder: field.fieldOrder + 1 })
                          }
                        >
                          Down
                        </Button>
                        <Button type="button" size="sm" variant="ghost" onClick={() => openEdit(field)}>
                          Edit
                        </Button>
                        <Button type="button" size="sm" variant="ghost" onClick={() => setDeleteField(field)}>
                          Delete
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )
        }
      </QueryGate>

      <Dialog
        open={createOpen}
        onOpenChange={(open) => {
          setCreateOpen(open);
          if (!open) {
            setForm(emptyForm());
            setDeveloperTouched(false);
          }
        }}
        widthClassName="max-w-2xl"
      >
        <form onSubmit={handleCreate}>
          <DialogHeader>
            <DialogTitle>New field</DialogTitle>
          </DialogHeader>
          <DialogContent className="space-y-4">
            <FieldFormFields
              form={form}
              setForm={setForm}
              fieldTypes={fieldTypes}
              relatedTypes={relatedTypes}
              developerLocked={false}
              developerTouched={developerTouched}
              setDeveloperTouched={setDeveloperTouched}
            />
          </DialogContent>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createMutation.isPending}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </Dialog>

      <Dialog
        open={editField !== null}
        onOpenChange={(open) => {
          if (!open) {
            setEditField(null);
          }
        }}
        widthClassName="max-w-2xl"
      >
        <form onSubmit={handleEdit}>
          <DialogHeader>
            <DialogTitle>Edit field</DialogTitle>
          </DialogHeader>
          <DialogContent className="space-y-4">
            <FieldFormFields
              form={form}
              setForm={setForm}
              fieldTypes={fieldTypes}
              relatedTypes={relatedTypes}
              developerLocked
              developerTouched
              setDeveloperTouched={setDeveloperTouched}
            />
          </DialogContent>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setEditField(null)}>
              Cancel
            </Button>
            <Button type="submit" loading={editMutation.isPending}>
              Save
            </Button>
          </DialogFooter>
        </form>
      </Dialog>

      <ConfirmDialog
        open={deleteField !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteField(null);
          }
        }}
        title="Delete field?"
        body="Existing values for this field stay in stored content but the field will no longer appear on forms."
        onConfirm={() => {
          if (deleteField) {
            deleteMutation.mutate(deleteField.id);
          }
        }}
        pending={deleteMutation.isPending}
      />
    </div>
  );
}

function FieldFormFields({
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
      {hasChoices(form.fieldType) && (
        <ChoicesEditor
          choices={form.choices}
          onChange={(choices) => setForm({ ...form, choices })}
        />
      )}
      {isRelationship(form.fieldType) && (
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
      )}
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

function createPayload(form: FieldForm): JsonObject {
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

function editPayload(form: FieldForm): JsonObject {
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

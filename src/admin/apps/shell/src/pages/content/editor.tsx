import { adminApi, formatError } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  ConfirmDialog,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, formatWhen, readBoolean, readString } from "../entity";
import { ContentFieldControl } from "./field-controls";
import {
  emptyFieldValue,
  fieldValueForSave,
  parseContentMap,
  parseContentTypeSummary,
  parseFieldValue,
  parseNamedRefs,
  type ContentField,
  type ContentFieldValue,
} from "./fields-model";
import { ContentTypeNav } from "./nav";

type EditorValues = Record<string, ContentFieldValue>;

function valuesFromContent(fields: ContentField[], content: Record<string, unknown>): EditorValues {
  const values: EditorValues = {};
  for (const field of fields) {
    values[field.developerName] = parseFieldValue(field, content[field.developerName]);
  }
  return values;
}

function emptyValues(fields: ContentField[]): EditorValues {
  const values: EditorValues = {};
  for (const field of fields) {
    values[field.developerName] = emptyFieldValue(field);
  }
  return values;
}

function contentPayload(values: EditorValues): JsonObject {
  const content: JsonObject = {};
  for (const [key, value] of Object.entries(values)) {
    content[key] = fieldValueForSave(value);
  }
  return content;
}

export function NewContentItemPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  return <ItemEditor developerName={developerName} itemId={null} />;
}

export function EditContentItemPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  const itemId = typeof params.id === "string" ? params.id : "";
  return <ItemEditor developerName={developerName} itemId={itemId || null} />;
}

function ItemEditor({ developerName, itemId }: { developerName: string; itemId: string | null }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const isNew = itemId === null;
  const titleCrumb = isNew ? "New item" : "Edit item";
  useDocumentTitle([titleCrumb, developerName]);

  const [values, setValues] = useState<EditorValues>({});
  const [templateId, setTemplateId] = useState("");
  const [routePath, setRoutePath] = useState("");
  const [hydrated, setHydrated] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const items = adminApi.contentItems(developerName);

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const itemQuery = useQuery({
    queryKey: ["content-item", developerName, itemId],
    queryFn: () => items.get(itemId ?? ""),
    enabled: developerName.length > 0 && itemId !== null,
  });
  const templatesQuery = useQuery({
    queryKey: ["content-templates", developerName],
    queryFn: () => adminApi.contentTypes.templates(developerName),
    enabled: developerName.length > 0,
  });
  const revisionsQuery = useQuery({
    queryKey: ["content-revisions", developerName, itemId],
    queryFn: () => items.revisions(itemId ?? "", { pageSize: 50 }),
    enabled: developerName.length > 0 && itemId !== null,
  });

  const contentType = parseContentTypeSummary(typeQuery.data);
  const fields = contentType?.fields ?? [];
  const templates = parseNamedRefs(templatesQuery.data);
  const item = itemQuery.data;
  const itemRecord = item ? entityFields(item) : {};

  useEffect(() => {
    if (fields.length === 0 || hydrated) {
      return;
    }
    if (isNew) {
      setValues(emptyValues(fields));
      const first = templates[0];
      if (first && !templateId) {
        setTemplateId(first.id);
      }
      setHydrated(true);
      return;
    }
    if (!item) {
      return;
    }
    const draft = parseContentMap(itemRecord.draftContent);
    const published = parseContentMap(itemRecord.publishedContent);
    const source = Object.keys(draft).length > 0 ? draft : published;
    setValues(valuesFromContent(fields, source));
    setRoutePath(readString(itemRecord, "routePath"));
    const first = templates[0];
    if (first && !templateId) {
      setTemplateId(first.id);
    }
    setHydrated(true);
  }, [fields, hydrated, isNew, item, itemRecord, templateId, templates]);

  const setField = (developer: string, next: ContentFieldValue) => {
    setValues((current) => ({ ...current, [developer]: next }));
  };

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-item", developerName, itemId] });
    void queryClient.invalidateQueries({ queryKey: ["content-items", developerName] });
    void queryClient.invalidateQueries({ queryKey: ["content-revisions", developerName, itemId] });
  };

  const saveMutation = useMutation({
    mutationFn: async (saveAsDraft: boolean) => {
      const content = contentPayload(values);
      if (isNew) {
        return items.create({ saveAsDraft, templateId, content });
      }
      return items.update(itemId, { saveAsDraft, content });
    },
    onSuccess: (created) => {
      toast.success("Saved");
      invalidate();
      if (isNew && created.id) {
        void navigate({
          to: "/content/$developerName/items/$id",
          params: { developerName, id: created.id },
        });
      }
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const unpublishMutation = useMutation({
    mutationFn: () => items.unpublish(itemId ?? ""),
    onSuccess: () => {
      toast.success("Unpublished");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const discardMutation = useMutation({
    mutationFn: () => items.discardDraft(itemId ?? ""),
    onSuccess: () => {
      toast.success("Draft discarded");
      setHydrated(false);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: () => items.remove(itemId ?? ""),
    onSuccess: () => {
      toast.success("Item deleted");
      void navigate({ to: "/content/$developerName", params: { developerName } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const settingsMutation = useMutation({
    mutationFn: () => items.updateSettings(itemId ?? "", { templateId, routePath }),
    onSuccess: () => {
      toast.success("Settings saved");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const revertMutation = useMutation({
    mutationFn: (revisionId: string) => items.revert(revisionId),
    onSuccess: () => {
      toast.success("Reverted to revision");
      setHydrated(false);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const isPublished = readBoolean(itemRecord, "isPublished");
  const isDraft = readBoolean(itemRecord, "isDraft");
  const heading = contentType?.labelSingular || developerName || "Item";

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content item" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  const body = (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
      <Card>
        <CardContent className="space-y-4 pt-6">
          {!hydrated ? (
            <p className="text-sm text-muted-foreground">Loading fields…</p>
          ) : fields.length === 0 ? (
            <p className="text-sm text-muted-foreground">This content type has no fields yet.</p>
          ) : (
            fields.map((field) => {
              const value = values[field.developerName] ?? emptyFieldValue(field);
              return (
                <ContentFieldControl
                  key={field.id}
                  field={field}
                  value={value}
                  onChange={(next) => setField(field.developerName, next)}
                />
              );
            })
          )}
          {isNew && (
            <FormField label="Template" required htmlFor="item-template">
              {(control) => (
                <Select {...control} value={templateId} onChange={(event) => setTemplateId(event.target.value)}>
                  <option value="">Select a template</option>
                  {templates.map((template) => (
                    <option key={template.id} value={template.id}>
                      {template.label || template.developerName}
                    </option>
                  ))}
                </Select>
              )}
            </FormField>
          )}
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              loading={saveMutation.isPending}
              onClick={() => saveMutation.mutate(true)}
            >
              Save draft
            </Button>
            <Button type="button" loading={saveMutation.isPending} onClick={() => saveMutation.mutate(false)}>
              Publish
            </Button>
            {!isNew && isPublished && (
              <Button
                type="button"
                variant="outline"
                loading={unpublishMutation.isPending}
                onClick={() => unpublishMutation.mutate()}
              >
                Unpublish
              </Button>
            )}
            {!isNew && isDraft && (
              <Button
                type="button"
                variant="outline"
                loading={discardMutation.isPending}
                onClick={() => discardMutation.mutate()}
              >
                Discard draft
              </Button>
            )}
            {!isNew && (
              <Button type="button" variant="ghost" onClick={() => setDeleteOpen(true)}>
                Delete
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      <div className="space-y-6">
        {!isNew && (
          <Card>
            <CardHeader>
              <CardTitle>Status</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                {isPublished ? <Badge variant="success">Published</Badge> : <Badge variant="secondary">Unpublished</Badge>}
                {isDraft ? <Badge variant="warning">Draft</Badge> : null}
              </div>
              <p className="text-muted-foreground">Updated {formatWhen(itemRecord.lastModificationTime) || "—"}</p>
            </CardContent>
          </Card>
        )}

        {!isNew && (
          <Card>
            <CardHeader>
              <CardTitle>Settings</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <FormField label="Route path" required htmlFor="item-route">
                {(control) => (
                  <Input
                    {...control}
                    value={routePath}
                    onChange={(event) => setRoutePath(event.target.value)}
                  />
                )}
              </FormField>
              <FormField label="Template" required htmlFor="item-template-settings">
                {(control) => (
                  <Select {...control} value={templateId} onChange={(event) => setTemplateId(event.target.value)}>
                    <option value="">Select a template</option>
                    {templates.map((template) => (
                      <option key={template.id} value={template.id}>
                        {template.label || template.developerName}
                      </option>
                    ))}
                  </Select>
                )}
              </FormField>
              <Button type="button" variant="outline" loading={settingsMutation.isPending} onClick={() => settingsMutation.mutate()}>
                Save settings
              </Button>
            </CardContent>
          </Card>
        )}

        {!isNew && (
          <RevisionsList
            revisions={revisionsQuery.data?.items ?? []}
            pending={revertMutation.isPending}
            onRevert={(id) => revertMutation.mutate(id)}
          />
        )}
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={isNew ? `New ${heading}` : readString(itemRecord, "primaryField") || heading}
        description={isNew ? "Fill in the fields and save a draft or publish." : readString(itemRecord, "routePath")}
      />
      <ContentTypeNav developerName={developerName} />
      {isNew ? (
        <QueryGate query={typeQuery}>{() => body}</QueryGate>
      ) : (
        <QueryGate query={itemQuery}>{() => body}</QueryGate>
      )}
      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Delete this item?"
        body="It will move to trash and can be restored from there."
        onConfirm={() => deleteMutation.mutate()}
        pending={deleteMutation.isPending}
      />
    </div>
  );
}

function RevisionsList({
  revisions,
  pending,
  onRevert,
}: {
  revisions: { id: string }[];
  pending: boolean;
  onRevert: (id: string) => void;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Revisions</CardTitle>
      </CardHeader>
      <CardContent>
        {revisions.length === 0 ? (
          <p className="text-sm text-muted-foreground">No revisions yet. Publishing creates a revision.</p>
        ) : (
          <ul className="space-y-3">
            {revisions.map((revision) => {
              const fields = entityFields(revision);
              return (
                <li key={revision.id} className="flex items-center justify-between gap-2 text-sm">
                  <span>{formatWhen(fields.creationTime) || revision.id}</span>
                  <Button type="button" size="sm" variant="outline" loading={pending} onClick={() => onRevert(revision.id)}>
                    Revert
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

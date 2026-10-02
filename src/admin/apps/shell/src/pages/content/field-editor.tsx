import { adminApi, formatError } from "@raytha/api";
import { Button, Card, CardContent, DangerZone, PageHeader, QueryGate, toast } from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import {
  createFieldPayload,
  editFieldPayload,
  emptyFieldForm,
  FieldFormFields,
  formFromField,
  type FieldForm,
} from "./field-form";
import { parseContentFields, parseFieldTypeOptions, parseNamedRefs } from "./fields-model";

export function NewContentTypeFieldPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["New field", developerName]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FieldForm>(emptyFieldForm);
  const [developerTouched, setDeveloperTouched] = useState(false);
  const fieldTypesQuery = useQuery({
    queryKey: ["content-field-types"],
    queryFn: () => adminApi.contentTypes.fieldTypes(),
  });
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
  });

  const mutation = useMutation({
    mutationFn: () => adminApi.fields(developerName).create(createFieldPayload(form)),
    onSuccess: () => {
      toast.success("Field created");
      void queryClient.invalidateQueries({ queryKey: ["content-fields", developerName] });
      void navigate({ to: "/content-types/$developerName/fields", params: { developerName } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <ListBackLink
            to="/content-types/$developerName/fields"
            params={{ developerName }}
            listKey={`content-fields:${developerName}`}
            label="fields"
          />
        }
        title="New field"
      />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FieldFormFields
              form={form}
              setForm={setForm}
              fieldTypes={parseFieldTypeOptions(fieldTypesQuery.data)}
              relatedTypes={parseNamedRefs(typesQuery.data?.items)}
              developerLocked={false}
              developerTouched={developerTouched}
              setDeveloperTouched={setDeveloperTouched}
            />
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function EditContentTypeFieldPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Edit field", developerName]);
  const fieldsQuery = useQuery({
    queryKey: ["content-fields", developerName],
    queryFn: () => adminApi.fields(developerName).list({ pageSize: 200, orderBy: "FieldOrder asc" }),
    enabled: developerName.length > 0,
  });
  const field = parseContentFields(fieldsQuery.data?.items).find((item) => item.id === id);

  if (!developerName || !id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit field" />
        <p className="text-sm text-muted-foreground">Missing field id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <ListBackLink
            to="/content-types/$developerName/fields"
            params={{ developerName }}
            listKey={`content-fields:${developerName}`}
            label="fields"
          />
        }
        title="Edit field"
      />
      <QueryGate query={fieldsQuery}>
        {() =>
          field ? (
            <EditFieldForm developerName={developerName} fieldId={id} initial={formFromField(field)} />
          ) : (
            <p className="text-sm text-muted-foreground">That field was not found.</p>
          )
        }
      </QueryGate>
    </div>
  );
}

function EditFieldForm({
  developerName,
  fieldId,
  initial,
}: {
  developerName: string;
  fieldId: string;
  initial: FieldForm;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [form, setForm] = useState(initial);
  const fieldTypesQuery = useQuery({
    queryKey: ["content-field-types"],
    queryFn: () => adminApi.contentTypes.fieldTypes(),
  });
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
  });

  const mutation = useMutation({
    mutationFn: () => adminApi.fields(developerName).update(fieldId, editFieldPayload(form)),
    onSuccess: () => {
      toast.success("Field updated");
      void queryClient.invalidateQueries({ queryKey: ["content-fields", developerName] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: () => adminApi.fields(developerName).remove(fieldId),
    onSuccess: () => {
      toast.success("Field deleted");
      void queryClient.invalidateQueries({ queryKey: ["content-fields", developerName] });
      void queryClient.invalidateQueries({ queryKey: ["content-type", developerName] });
      void navigate({ to: "/content-types/$developerName/fields", params: { developerName } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FieldFormFields
              form={form}
              setForm={setForm}
              fieldTypes={parseFieldTypeOptions(fieldTypesQuery.data)}
              relatedTypes={parseNamedRefs(typesQuery.data?.items)}
              developerLocked
              developerTouched
              setDeveloperTouched={() => undefined}
            />
            <Button type="submit" loading={mutation.isPending}>
              Save
            </Button>
          </form>
        </CardContent>
      </Card>
      <DangerZone
        description="Remove this field and drop it from every view's columns. The primary field, or a field a view sorts or filters by, cannot be deleted until that changes."
        actionLabel="Delete field"
        confirmTitle={`Delete ${initial.label || "this field"}?`}
        confirmBody="Values already saved for this field stay in stored content, but the field no longer appears on forms."
        onConfirm={() => deleteMutation.mutate()}
        pending={deleteMutation.isPending}
      />
    </div>
  );
}

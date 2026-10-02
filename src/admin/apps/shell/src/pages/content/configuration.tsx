import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  DangerZone,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  Textarea,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { ListBackLink } from "../../components/list-back-link";
import { parseContentTypeSummary } from "./fields-model";
import { RouteTemplateField } from "./route-template";

type SettingsForm = {
  labelSingular: string;
  labelPlural: string;
  description: string;
  defaultRouteTemplate: string;
  primaryFieldId: string;
};

export function ContentTypeConfigurationPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Settings", developerName]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const canDelete = hasPermission(platformPermissions.contentTypes);
  const [form, setForm] = useState<SettingsForm | null>(null);
  const [formFor, setFormFor] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });

  const contentType = parseContentTypeSummary(query.data);

  if (contentType && contentType.id !== formFor) {
    setFormFor(contentType.id);
    setForm({
      labelSingular: contentType.labelSingular,
      labelPlural: contentType.labelPlural,
      description: contentType.description,
      defaultRouteTemplate: contentType.defaultRouteTemplate,
      primaryFieldId: contentType.primaryFieldId,
    });
  }

  const mutation = useMutation({
    mutationFn: (input: SettingsForm) => adminApi.contentTypes.updateByDeveloperName(developerName, input),
    onSuccess: () => {
      toast.success("Content type saved");
      void queryClient.invalidateQueries({ queryKey: ["content-type", developerName] });
      void queryClient.invalidateQueries({ queryKey: ["content-types"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: () => adminApi.contentTypes.removeByDeveloperName(developerName),
    onSuccess: () => {
      toast.success("Content type deleted");
      queryClient.removeQueries({ queryKey: ["content-type", developerName] });
      void queryClient.invalidateQueries({ queryKey: ["content-types"] });
      void navigate({ to: "/content-types" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (form) {
      mutation.mutate(form);
    }
  };

  const textFields = (contentType?.fields ?? []).filter((field) => field.fieldType === "single_line_text");

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content type settings" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <ListBackLink
            to="/content/$developerName"
            params={{ developerName }}
            listKey={`content-items:${developerName}`}
            label={contentType?.labelPlural || developerName}
          />
        }
        title={`${contentType?.labelPlural || developerName} settings`}
        description="Labels, primary field, and the default public route template."
      />
      <QueryGate query={query}>
        {() =>
          form ? (
            <>
              <Card>
              <CardContent className="pt-6">
                <form className="space-y-4" onSubmit={handleSubmit}>
                  <FormField label="Singular label" required htmlFor="ct-label-singular">
                    {(control) => (
                      <Input
                        {...control}
                        value={form.labelSingular}
                        onChange={(event) => setForm({ ...form, labelSingular: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField label="Plural label" required htmlFor="ct-label-plural">
                    {(control) => (
                      <Input
                        {...control}
                        value={form.labelPlural}
                        onChange={(event) => setForm({ ...form, labelPlural: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField label="Description" htmlFor="ct-description">
                    {(control) => (
                      <Textarea
                        {...control}
                        value={form.description}
                        onChange={(event) => setForm({ ...form, description: event.target.value })}
                      />
                    )}
                  </FormField>
                  <RouteTemplateField
                    id="ct-route"
                    value={form.defaultRouteTemplate}
                    onChange={(defaultRouteTemplate) => setForm({ ...form, defaultRouteTemplate })}
                    developerName={developerName}
                  />
                  <FormField
                    label="Primary field"
                    required
                    htmlFor="ct-primary-field"
                    hint="Must be a single line text field."
                  >
                    {(control) => (
                      <Select
                        {...control}
                        value={form.primaryFieldId}
                        onChange={(event) => setForm({ ...form, primaryFieldId: event.target.value })}
                      >
                        <option value="">Select a field</option>
                        {textFields.map((field) => (
                          <option key={field.id} value={field.id}>
                            {field.label || field.developerName}
                          </option>
                        ))}
                      </Select>
                    )}
                  </FormField>
                  <Button type="submit" loading={mutation.isPending}>
                    Save
                  </Button>
                </form>
              </CardContent>
            </Card>
            {canDelete ? (
              <DangerZone
                description="Permanently deletes this content type, its fields, its content items including trash, its views, and their public routes."
                actionLabel="Delete content type"
                confirmTitle={`Delete ${contentType?.labelPlural || developerName}?`}
                confirmBody="This cannot be undone. Delete is refused while this type owns the home page, or while another content type has a relationship field pointing at it."
                confirmLabel="Delete content type"
                onConfirm={() => deleteMutation.mutate()}
                pending={deleteMutation.isPending}
              />
            ) : null}
            </>
          ) : (
            <p className="text-sm text-muted-foreground">This content type could not be read.</p>
          )
        }
      </QueryGate>
    </div>
  );
}

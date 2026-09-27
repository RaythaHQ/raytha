import { adminApi, formatError, hasPermission, platformPermissions, problemFieldErrors } from "@raytha/api";
import { Button, Card, CardContent, FormField, Input, PageHeader, Textarea, toast } from "@raytha/ui";
import { useMutation } from "@tanstack/react-query";
import { Link, useNavigate } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { CrudListPage } from "./crud-list";
import { entityFields, pluralize, readString, toDeveloperName } from "./entity";
import { DEFAULT_ROUTE_TEMPLATE, RouteTemplateField } from "./content/route-template";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";

export function ContentTypesPage() {
  return (
    <CrudListPage
      title="Content types"
        queryKey={["content-types"]}
      listKey="content-types"
      noun="content type"
      list={adminApi.contentTypes.list}
      createPermission={platformPermissions.contentTypes}
      columns={[
        {
          header: "Label",
          cell: (entity) => {
            const fields = entityFields(entity);
            const developerName = readString(fields, "developerName");
            const label = readString(fields, "labelPlural", "labelSingular") || developerName || entity.id;
            if (!developerName) {
              return label;
            }
            return (
              <Link to="/content/$developerName" params={{ developerName }} className="text-primary hover:underline">
                {label}
              </Link>
            );
          },
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        { header: "Description", cell: (entity) => readString(entityFields(entity), "description") || "—" },
      ]}
      rowActions={(entity) => {
        const developerName = readString(entityFields(entity), "developerName");
        if (!developerName) {
          return [];
        }
        return [
          {
            id: "views",
            label: "Views",
            to: "/content/$developerName/views",
            params: { developerName },
          },
          {
            id: "fields",
            label: "Fields",
            to: "/content-types/$developerName/fields",
            params: { developerName },
          },
          {
            id: "settings",
            label: "Settings",
            to: "/content-types/$developerName/configuration",
            params: { developerName },
          },
        ];
      }}
      actions={
        hasPermission(platformPermissions.contentTypes) ? (
          <Link
            to="/content-types/new"
            className="inline-flex h-10 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600"
          >
            New content type
          </Link>
        ) : undefined
      }
    />
  );
}

type NewContentTypeForm = {
  labelSingular: string;
  labelPlural: string;
  developerName: string;
  description: string;
  defaultRouteTemplate: string;
};

const DEVELOPER_NAME_PATTERN = /^[a-z0-9]+(_[a-z0-9]+)*$/;

export function NewContentTypePage() {
  useDocumentTitle(["New content type"]);
  const navigate = useNavigate();
  const [form, setForm] = useState<NewContentTypeForm>({
    labelSingular: "",
    labelPlural: "",
    developerName: "",
    description: "",
    defaultRouteTemplate: DEFAULT_ROUTE_TEMPLATE,
  });
  const [pluralTouched, setPluralTouched] = useState(false);
  const [developerTouched, setDeveloperTouched] = useState(false);

  const mutation = useMutation({
    mutationFn: (input: NewContentTypeForm) => adminApi.contentTypes.create(input),
    onSuccess: (_created, input) => {
      toast.success(`${input.labelPlural} created`);
      void navigate({ to: "/content/$developerName", params: { developerName: input.developerName } });
    },
    onError: (error) => {
      if (Object.keys(problemFieldErrors(error)).length === 0) {
        toast.error(formatError(error));
      }
    },
  });
  const serverErrors = problemFieldErrors(mutation.error);
  const developerFormatError =
    form.developerName && !DEVELOPER_NAME_PATTERN.test(form.developerName)
      ? "Use lowercase letters, numbers, and single underscores."
      : undefined;

  const setLabelPlural = (labelPlural: string) =>
    setForm((current) => ({
      ...current,
      labelPlural,
      developerName: developerTouched ? current.developerName : toDeveloperName(labelPlural),
    }));

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (!developerFormatError) {
      mutation.mutate(form);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/content-types" listKey="content-types" label="content types" />}
        title="New content type"
        description="A content type is a kind of content, like blog posts or team members. It starts with a Title and a Content field and an “All” view."
      />
      <Card className="max-w-3xl">
        <CardContent className="pt-6">
          <form className="space-y-5" onSubmit={handleSubmit} noValidate>
            <div className="grid gap-5 sm:grid-cols-2">
              <FormField
                label="Singular label"
                required
                htmlFor="ct-label-singular"
                hint="One item, e.g. Blog post."
                error={serverErrors.LabelSingular}
              >
                {(control) => (
                  <Input
                    {...control}
                    value={form.labelSingular}
                    onChange={(event) => {
                      const labelSingular = event.target.value;
                      setForm((current) => ({ ...current, labelSingular }));
                      if (!pluralTouched) {
                        setLabelPlural(pluralize(labelSingular));
                      }
                    }}
                  />
                )}
              </FormField>
              <FormField
                label="Plural label"
                required
                htmlFor="ct-label-plural"
                hint="The list and sidebar name, e.g. Blog posts."
                error={serverErrors.LabelPlural}
              >
                {(control) => (
                  <Input
                    {...control}
                    value={form.labelPlural}
                    onChange={(event) => {
                      setPluralTouched(true);
                      setLabelPlural(event.target.value);
                    }}
                  />
                )}
              </FormField>
            </div>
            <FormField
              label="Developer name"
              required
              htmlFor="ct-developer-name"
              hint="Used in templates, the API, and URLs. It cannot be changed later."
              error={developerFormatError ?? serverErrors.DeveloperName}
            >
              {(control) => (
                <Input
                  {...control}
                  className="font-mono"
                  value={form.developerName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setForm((current) => ({ ...current, developerName: event.target.value }));
                  }}
                />
              )}
            </FormField>
            <FormField label="Description" htmlFor="ct-description" error={serverErrors.Description}>
              {(control) => (
                <Textarea
                  {...control}
                  value={form.description}
                  onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                />
              )}
            </FormField>
            <RouteTemplateField
              id="ct-route"
              value={form.defaultRouteTemplate}
              onChange={(defaultRouteTemplate) => setForm((current) => ({ ...current, defaultRouteTemplate }))}
              developerName={form.developerName}
              error={serverErrors.DefaultRouteTemplate}
            />
            {serverErrors[""] && (
              <p role="alert" className="rounded-lg border border-destructive/40 bg-destructive/5 px-3 py-2 text-sm text-destructive">
                {serverErrors[""]}
              </p>
            )}
            <div className="flex gap-2">
              <Button type="submit" loading={mutation.isPending}>
                Create content type
              </Button>
              <Button type="button" variant="outline" onClick={() => void navigate({ to: "/content-types" })}>
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export { ContentTypeHomePage, ContentViewItemsPage } from "./content/items";

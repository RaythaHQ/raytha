import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import { Badge, Button, Card, CardContent, FormField, Input, PageHeader, Textarea, toast } from "@raytha/ui";
import { useMutation } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { CrudListPage } from "./crud-list";
import { entityFields, formatWhen, readBoolean, readString, toDeveloperName } from "./entity";
import { ContentTypeNav } from "./content/nav";
import { useDocumentTitle } from "../lib/document-title";

export function ContentTypesPage() {
  return (
    <CrudListPage
      title="Content types"
      queryKey={["content-types"]}
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
          return null;
        }
        return (
          <>
            <Link
              to="/content/$developerName/views"
              params={{ developerName }}
              className="text-sm text-primary hover:underline"
            >
              Views
            </Link>
            <Link
              to="/content-types/$developerName/fields"
              params={{ developerName }}
              className="text-sm text-primary hover:underline"
            >
              Fields
            </Link>
            <Link
              to="/content-types/$developerName/configuration"
              params={{ developerName }}
              className="text-sm text-primary hover:underline"
            >
              Settings
            </Link>
          </>
        );
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

export function NewContentTypePage() {
  useDocumentTitle(["New content type"]);
  const navigate = useNavigate();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [description, setDescription] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.contentTypes.create({
        labelSingular: label,
        labelPlural: label.endsWith("s") ? label : `${label}s`,
        developerName: developerName || toDeveloperName(label),
        description,
        defaultRouteTemplate: "{ContentTypeDeveloperName}/{PrimaryField}",
      }),
    onSuccess: () => {
      toast.success("Content type created");
      const name = developerName || toDeveloperName(label);
      void navigate({ to: "/content/$developerName", params: { developerName: name } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
    <div className="space-y-6">
      <PageHeader title="New content type" description="Label and developer name are required. A default route template is applied." />
      <Card>
        <CardContent className="space-y-4 pt-6">
          <form className="space-y-4" onSubmit={handleSubmit}>
            <FormField label="Label" required htmlFor="content-type-label">
              {(control) => (
                <Input
                  {...control}
                  value={label}
                  onChange={(event) => {
                    const next = event.target.value;
                    setLabel(next);
                    if (!developerTouched) {
                      setDeveloperName(toDeveloperName(next));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField label="Developer name" required htmlFor="content-type-developer-name">
              {(control) => (
                <Input
                  {...control}
                  value={developerName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setDeveloperName(event.target.value);
                  }}
                />
              )}
            </FormField>
            <FormField label="Description" htmlFor="content-type-description">
              {(control) => (
                <Textarea
                  {...control}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                />
              )}
            </FormField>
            <div className="flex gap-2">
              <Button type="submit" loading={mutation.isPending}>
                Create
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

export function ContentItemsPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle([developerName || "Content items"]);

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content items" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  const items = adminApi.contentItems(developerName);

  return (
    <div className="space-y-6">
      <ContentTypeNav developerName={developerName} />
      <CrudListPage
        title={developerName}
        description="Items for this content type."
        queryKey={["content-items", developerName]}
        noun="item"
        list={items.list}
        remove={items.remove}
        columns={[
          {
            header: "Title",
            cell: (entity) => (
              <Link
                to="/content/$developerName/items/$id"
                params={{ developerName, id: entity.id }}
                className="text-primary hover:underline"
              >
                {readString(entityFields(entity), "primaryField", "title") || entity.id}
              </Link>
            ),
          },
          { header: "Path", cell: (entity) => readString(entityFields(entity), "routePath") || "—" },
          {
            header: "Status",
            cell: (entity) => <ItemStatus entity={entity} />,
          },
          { header: "Updated", cell: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—" },
        ]}
        rowActions={(entity) => (
          <Link
            to="/content/$developerName/items/$id"
            params={{ developerName, id: entity.id }}
            className="text-sm text-primary hover:underline"
          >
            Edit
          </Link>
        )}
        actions={
          <Link
            to="/content/$developerName/new"
            params={{ developerName }}
            className="inline-flex h-10 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600"
          >
            New item
          </Link>
        }
      />
    </div>
  );
}

function ItemStatus({ entity }: { entity: EntityRef }) {
  const fields = entityFields(entity);
  if (readBoolean(fields, "isPublished")) {
    return <Badge variant="success">Published</Badge>;
  }
  if (readBoolean(fields, "isDraft")) {
    return <Badge variant="warning">Draft</Badge>;
  }
  return <Badge variant="secondary">Unpublished</Badge>;
}

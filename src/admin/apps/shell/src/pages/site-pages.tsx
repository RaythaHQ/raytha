import { adminApi, platformPermissions } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import { Badge, FormField, Select } from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { CrudListPage } from "./crud-list";
import { entityFields, formatWhen, readBoolean, readString } from "./entity";

export { SitePageDetailPage } from "./site-pages/detail";
export { SitePageLayoutPage } from "./site-pages/layout";

export function SitePagesPage() {
  const themesQuery = useQuery({
    queryKey: ["themes", "picker"],
    queryFn: () => adminApi.themes.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });
  const themeId = themesQuery.data?.items[0]?.id ?? "";
  const templatesQuery = useQuery({
    queryKey: ["web-templates", themeId],
    queryFn: () => adminApi.webTemplates(themeId).list({ pageSize: 100 }),
    enabled: themeId.length > 0,
    placeholderData: keepPreviousData,
  });
  const templates = templatesQuery.data?.items ?? [];

  return (
    <CrudListPage
      title="Site Pages"
      description="Pages on the public website."
      queryKey={["site-pages"]}
      noun="site page"
      list={adminApi.sitePages.list}
      create={adminApi.sitePages.create}
      remove={adminApi.sitePages.remove}
      createPermission={platformPermissions.sitePages}
      createLabel="New page"
      createFields={[
        { key: "title", label: "Title", required: true },
        { key: "saveAsDraft", label: "Save as draft", type: "checkbox" },
      ]}
      extraCreateFields={(form, setForm) => (
        <FormField label="Template" required htmlFor="site-page-template">
          {(control) => (
            <Select
              {...control}
              value={typeof form.templateId === "string" ? form.templateId : ""}
              onChange={(event) => setForm({ ...form, templateId: event.target.value })}
            >
              <option value="">Select a template</option>
              {templates.map((template) => (
                <option key={template.id} value={template.id}>
                  {readString(entityFields(template), "label", "developerName") || template.id}
                </option>
              ))}
            </Select>
          )}
        </FormField>
      )}
      buildCreatePayload={(form) => {
        const payload: JsonObject = {
          title: form.title,
          saveAsDraft: form.saveAsDraft === true,
          templateId: form.templateId,
        };
        return payload;
      }}
      columns={[
        {
          header: "Title",
          cell: (entity) => {
            const title = readString(entityFields(entity), "title") || entity.id;
            return (
              <Link to="/site-pages/$id" params={{ id: entity.id }} className="text-primary hover:underline">
                {title}
              </Link>
            );
          },
        },
        { header: "Path", cell: (entity) => readString(entityFields(entity), "routePath") || "—" },
        {
          header: "Status",
          cell: (entity) => {
            const fields = entityFields(entity);
            if (readBoolean(fields, "isPublished")) {
              return <Badge variant="success">Published</Badge>;
            }
            if (readBoolean(fields, "isDraft")) {
              return <Badge variant="warning">Draft</Badge>;
            }
            return <Badge variant="secondary">Unpublished</Badge>;
          },
        },
        { header: "Updated", cell: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—" },
      ]}
      rowActions={(entity) => (
        <>
          <Link
            to="/site-pages/$id"
            params={{ id: entity.id }}
            className="inline-flex h-8 items-center rounded-md px-3 text-xs font-medium hover:bg-accent"
          >
            Edit
          </Link>
          <Link
            to="/site-pages/$id/layout"
            params={{ id: entity.id }}
            className="inline-flex h-8 items-center rounded-md px-3 text-xs font-medium hover:bg-accent"
          >
            Layout
          </Link>
        </>
      )}
    />
  );
}

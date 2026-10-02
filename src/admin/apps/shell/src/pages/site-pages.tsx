import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import { Badge, Button, Card, CardContent, Checkbox, FormField, Input, PageHeader, Select, toast } from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { Link, useNavigate } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { CrudListPage } from "./crud-list";
import { entityFields, formatWhen, jsonString, readBoolean, readString } from "./entity";
import { sitePageStatus } from "./site-pages/models";
import { SitePageStatusBadge } from "./site-pages/page-meta";

export { SitePageDetailPage } from "./site-pages/detail";
export { SitePageLayoutPage } from "./site-pages/layout";
export { NewSitePageWidgetPage, EditSitePageWidgetPage } from "./site-pages/widget-page";

export function SitePagesPage() {
  const configQuery = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });
  const homePageId = jsonString(configQuery.data ?? {}, "homePageId");
  const homePageType = jsonString(configQuery.data ?? {}, "homePageType");

  return (
    <CrudListPage
      title="Site Pages"
      description="Pages on the public website."
      queryKey={["site-pages"]}
      listKey="site-pages"
      noun="site page"
      list={adminApi.sitePages.list}
      createPermission={platformPermissions.sitePages}
      createLabel="New page"
      createTo="/site-pages/new"
      columns={[
        {
          header: "Title",
          cell: (entity) => {
            const title = readString(entityFields(entity), "title") || entity.id;
            const isHome = homePageType === "SitePage" && entity.id === homePageId;
            return (
              <span className="inline-flex flex-wrap items-center gap-2">
                <Link to="/site-pages/$id" params={{ id: entity.id }} className="text-primary hover:underline">
                  {title}
                </Link>
                {isHome ? <Badge variant="info">Home</Badge> : null}
              </span>
            );
          },
        },
        { header: "Path", cell: (entity) => readString(entityFields(entity), "routePath") || "—" },
        {
          header: "Status",
          cell: (entity) => {
            const fields = entityFields(entity);
            return (
              <SitePageStatusBadge status={sitePageStatus(readBoolean(fields, "isPublished"), readBoolean(fields, "isDraft"))} />
            );
          },
        },
        { header: "Updated", cell: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—" },
      ]}
      rowActions={(entity) => [
        {
          id: "layout",
          label: "Layout",
          to: "/site-pages/$id/layout",
          params: { id: entity.id },
        },
      ]}
    />
  );
}

export function NewSitePagePage() {
  useDocumentTitle(["New site page"]);
  const navigate = useNavigate();
  const [title, setTitle] = useState("");
  const [templateId, setTemplateId] = useState("");
  const [saveAsDraft, setSaveAsDraft] = useState(false);
  const templates = useActiveThemeTemplates();

  const mutation = useMutation({
    mutationFn: () => {
      const payload: JsonObject = { title, templateId, saveAsDraft };
      return adminApi.sitePages.create(payload);
    },
    onSuccess: (created) => {
      toast.success("Site page created");
      void navigate({ to: "/site-pages/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/site-pages" listKey="site-pages" label="site pages" />} title="New site page" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Title" required htmlFor="site-page-title">
              {(control) => (
                <Input {...control} value={title} onChange={(event) => setTitle(event.target.value)} />
              )}
            </FormField>
            <FormField label="Template" required htmlFor="site-page-template">
              {(control) => (
                <Select {...control} value={templateId} onChange={(event) => setTemplateId(event.target.value)}>
                  <option value="">Select a template</option>
                  {templates.map((template) => (
                    <option key={template.id} value={template.id}>
                      {readString(entityFields(template), "label", "developerName") || template.id}
                    </option>
                  ))}
                </Select>
              )}
            </FormField>
            <div className="flex items-center gap-2">
              <Checkbox id="site-page-draft" checked={saveAsDraft} onCheckedChange={setSaveAsDraft} />
              <label htmlFor="site-page-draft" className="text-sm">
                Save as draft
              </label>
            </div>
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function useActiveThemeTemplates() {
  const configQuery = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });
  const themeId = jsonString(configQuery.data ?? {}, "activeThemeId");
  const templatesQuery = useQuery({
    queryKey: ["web-templates", themeId],
    queryFn: () => adminApi.webTemplates(themeId).list({ pageSize: 100 }),
    enabled: themeId.length > 0,
    placeholderData: keepPreviousData,
  });
  return templatesQuery.data?.items ?? [];
}

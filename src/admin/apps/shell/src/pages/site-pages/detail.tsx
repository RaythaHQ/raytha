import { adminApi, formatError } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  AlertDialog,
  ConfirmDialog,
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
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, formatWhen, isRecord, jsonString, readString } from "../entity";
import { parseSitePage } from "./models";

export function SitePageDetailPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Site page"]);

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Site page" />
        <p className="text-sm text-muted-foreground">Missing page id.</p>
      </div>
    );
  }

  return <SitePageDetail id={id} />;
}

function SitePageDetail({ id }: { id: string }) {
  const queryClient = useQueryClient();
  const [revertId, setRevertId] = useState<string | null>(null);
  const [confirm, setConfirm] = useState<"publish" | "unpublish" | "discard" | "home" | null>(null);

  const pageQuery = useQuery({
    queryKey: ["site-pages", id],
    queryFn: () => adminApi.sitePages.get(id),
  });
  const configurationQuery = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });
  const revisionsQuery = useQuery({
    queryKey: ["site-pages", id, "revisions"],
    queryFn: () => adminApi.sitePages.revisions(id, { pageSize: 50 }),
    placeholderData: keepPreviousData,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
    void queryClient.invalidateQueries({ queryKey: ["configuration"] });
  };

  const publish = useMutation({
    mutationFn: () => adminApi.sitePages.publish(id),
    onSuccess: () => {
      toast.success("Page published");
      setConfirm(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });
  const unpublish = useMutation({
    mutationFn: () => adminApi.sitePages.unpublish(id),
    onSuccess: () => {
      toast.success("Page unpublished");
      setConfirm(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });
  const discardDraft = useMutation({
    mutationFn: () => adminApi.sitePages.discardDraft(id),
    onSuccess: () => {
      toast.success("Draft discarded");
      setConfirm(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });
  const setHome = useMutation({
    mutationFn: () => adminApi.sitePages.setAsHomePage(id),
    onSuccess: () => {
      toast.success("Set as home page");
      setConfirm(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });
  const revert = useMutation({
    mutationFn: (revisionId: string) => adminApi.sitePages.revertRevision(revisionId),
    onSuccess: () => {
      toast.success("Reverted to revision");
      setRevertId(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <QueryGate query={pageQuery}>
        {(entity) => {
          const page = parseSitePage(entity);
          const homePageId = jsonString(configurationQuery.data ?? {}, "homePageId");
          const homePageType = jsonString(configurationQuery.data ?? {}, "homePageType");
          const isHome = homePageType === "SitePage" && homePageId === page.id;
          return (
            <>
              <PageHeader
                title={page.title || "Site page"}
                description={page.routePath ? `/${page.routePath}` : undefined}
                actions={
                  <>
                    <Link
                      to="/site-pages/$id/layout"
                      params={{ id }}
                      className="inline-flex h-10 items-center rounded-lg border border-input bg-card px-4 text-sm font-medium shadow-card hover:bg-brand-50"
                    >
                      Edit layout
                    </Link>
                    {page.isDraft ? (
                      <Button type="button" variant="outline" onClick={() => setConfirm("discard")}>
                        Discard draft
                      </Button>
                    ) : null}
                    {page.isPublished ? (
                      <Button type="button" variant="outline" onClick={() => setConfirm("unpublish")}>
                        Unpublish
                      </Button>
                    ) : null}
                    <Button type="button" onClick={() => setConfirm("publish")}>
                      Publish
                    </Button>
                    {isHome ? (
                      <Badge variant="info">Home page</Badge>
                    ) : (
                      <Button type="button" variant="outline" onClick={() => setConfirm("home")}>
                        Set as home page
                      </Button>
                    )}
                  </>
                }
              />
              <div className="flex flex-wrap gap-2">
                {page.isPublished ? <Badge variant="success">Published</Badge> : <Badge variant="secondary">Unpublished</Badge>}
                {page.isDraft ? <Badge variant="warning">Draft</Badge> : null}
              </div>
              <SitePageSettingsForm
                key={`${page.id}-${page.title}-${page.routePath}-${page.webTemplateId}`}
                id={id}
                title={page.title}
                routePath={page.routePath}
                templateId={page.webTemplateId}
                isPublished={page.isPublished}
                isDraft={page.isDraft}
              />
            </>
          );
        }}
      </QueryGate>

      <Card>
        <CardHeader>
          <CardTitle>Revisions</CardTitle>
        </CardHeader>
        <CardContent>
          <QueryGate query={revisionsQuery}>
            {(data) =>
              data.items.length === 0 ? (
                <p className="text-sm text-muted-foreground">No revisions yet.</p>
              ) : (
                <Table aria-label="Revisions">
                  <TableHeader>
                    <TableRow>
                      <TableHead>When</TableHead>
                      <TableHead>By</TableHead>
                      <TableHead className="w-32">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.items.map((revision) => {
                      const fields = entityFields(revision);
                      const creator = fields.creatorUser;
                      const creatorName = isRecord(creator)
                        ? readString(creator, "fullName", "emailAddress")
                        : "";
                      return (
                        <TableRow key={revision.id}>
                          <TableCell>{formatWhen(fields.creationTime) || "—"}</TableCell>
                          <TableCell>{creatorName || "—"}</TableCell>
                          <TableCell>
                            <Button type="button" variant="outline" size="sm" onClick={() => setRevertId(revision.id)}>
                              Revert
                            </Button>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              )
            }
          </QueryGate>
        </CardContent>
      </Card>

      <AlertDialog
        open={confirm === "publish"}
        onOpenChange={(open) => {
          if (!open) setConfirm(null);
        }}
        title="Publish this page?"
        body="Draft widgets become the live page."
        confirmLabel="Publish"
        onConfirm={() => publish.mutate()}
        pending={publish.isPending}
      />
      <ConfirmDialog
        open={confirm === "unpublish"}
        onOpenChange={(open) => {
          if (!open) setConfirm(null);
        }}
        title="Unpublish this page?"
        body="The page will no longer be visible on the public site."
        confirmLabel="Unpublish"
        onConfirm={() => unpublish.mutate()}
        pending={unpublish.isPending}
      />
      <ConfirmDialog
        open={confirm === "discard"}
        onOpenChange={(open) => {
          if (!open) setConfirm(null);
        }}
        title="Discard draft?"
        body="Unpublished widget changes will be thrown away."
        confirmLabel="Discard"
        onConfirm={() => discardDraft.mutate()}
        pending={discardDraft.isPending}
      />
      <AlertDialog
        open={confirm === "home"}
        onOpenChange={(open) => {
          if (!open) setConfirm(null);
        }}
        title="Set as home page?"
        body="Visitors to the site root will see this page."
        confirmLabel="Set as home"
        onConfirm={() => setHome.mutate()}
        pending={setHome.isPending}
      />
      <ConfirmDialog
        open={revertId !== null}
        onOpenChange={(open) => {
          if (!open) setRevertId(null);
        }}
        title="Revert to this revision?"
        body="The current published widgets are saved as a new revision first."
        confirmLabel="Revert"
        onConfirm={() => {
          if (revertId) {
            revert.mutate(revertId);
          }
        }}
        pending={revert.isPending}
      />
    </div>
  );
}

function SitePageSettingsForm({
  id,
  title: initialTitle,
  routePath: initialRoutePath,
  templateId: initialTemplateId,
  isPublished,
  isDraft,
}: {
  id: string;
  title: string;
  routePath: string;
  templateId: string;
  isPublished: boolean;
  isDraft: boolean;
}) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState(initialTitle);
  const [routePath, setRoutePath] = useState(initialRoutePath);
  const [templateId, setTemplateId] = useState(initialTemplateId);

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

  const save = useMutation({
    mutationFn: async () => {
      const payload: JsonObject = {
        title,
        templateId,
        saveAsDraft: !isPublished || isDraft,
      };
      await adminApi.sitePages.update(id, payload);
      await adminApi.sitePages.updateSettings(id, { routePath });
    },
    onSuccess: () => {
      toast.success("Page settings saved");
      void queryClient.invalidateQueries({ queryKey: ["site-pages"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    save.mutate();
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Settings</CardTitle>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={handleSubmit}>
          <FormField label="Title" required htmlFor="site-page-title">
            {(control) => (
              <Input {...control} value={title} onChange={(event) => setTitle(event.target.value)} />
            )}
          </FormField>
          <FormField label="Path" required htmlFor="site-page-path" hint="URL path without a leading slash.">
            {(control) => (
              <Input {...control} value={routePath} onChange={(event) => setRoutePath(event.target.value)} />
            )}
          </FormField>
          <FormField label="Template" required htmlFor="site-page-template">
            {(control) => (
              <Select
                {...control}
                value={templateId}
                onChange={(event) => setTemplateId(event.target.value)}
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
          <div className="flex gap-2">
            <Button type="submit" loading={save.isPending}>
              Save settings
            </Button>
            <Link
              to="/site-pages"
              className="inline-flex h-10 items-center rounded-lg border border-input bg-card px-4 text-sm font-medium shadow-card hover:bg-brand-50"
            >
              Back to list
            </Link>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}


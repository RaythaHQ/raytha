import { adminApi, formatError } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  ConfirmDialog,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  RowActions,
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
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { Inbox, Star } from "lucide-react";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { rememberListQuery } from "../../lib/list-query";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, readString, toDeveloperName } from "../entity";
import { parseContentTypeSummary } from "./fields-model";
import { ContentTypeNav } from "./nav";

export function ContentViewsPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Views", developerName]);
  const queryClient = useQueryClient();
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const views = adminApi.views(developerName);

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const listQuery = useQuery({
    queryKey: ["content-views", developerName],
    queryFn: () => views.list({ pageSize: 100 }),
    enabled: developerName.length > 0,
    placeholderData: keepPreviousData,
  });
  const favoritesQuery = useQuery({
    queryKey: ["content-view-favorites", developerName],
    queryFn: () => views.favorites({ pageSize: 100 }),
    enabled: developerName.length > 0,
  });

  useEffect(() => {
    rememberListQuery(`content-views:${developerName}`, {});
  }, [developerName]);

  const contentType = parseContentTypeSummary(typeQuery.data);
  const favoriteIds = useMemo(() => {
    const ids = new Set<string>();
    for (const item of favoritesQuery.data?.items ?? []) {
      ids.add(item.id);
    }
    return ids;
  }, [favoritesQuery.data]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["content-views", developerName] });
    void queryClient.invalidateQueries({ queryKey: ["content-view-favorites", developerName] });
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => views.remove(id),
    onSuccess: () => {
      toast.success("View deleted");
      setDeleteId(null);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const favoriteMutation = useMutation({
    mutationFn: ({ id, setAsFavorite }: { id: string; setAsFavorite: boolean }) =>
      views.favorite(id, setAsFavorite),
    onSuccess: () => invalidate(),
    onError: (error) => toast.error(formatError(error)),
  });

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Views" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${contentType?.labelPlural || developerName} views`}
        description="Saved lists with columns, sort, and filters."
        actions={
          <Link
            to="/content/$developerName/views/new"
            params={{ developerName }}
            className="inline-flex h-10 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground shadow-card hover:bg-brand-600"
          >
            New view
          </Link>
        }
      />
      <ContentTypeNav developerName={developerName} />
      <QueryGate query={listQuery}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState icon={Inbox} title="No views" hint="Create a view to control columns and filters." />
          ) : (
            <Table aria-label="Views">
              <TableHeader>
                <TableRow>
                  <TableHead>Label</TableHead>
                  <TableHead>Developer name</TableHead>
                  <TableHead>Route</TableHead>
                  <TableHead className="w-56">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((view) => {
                  const fields = entityFields(view);
                  const viewLabel = readString(fields, "label") || view.id;
                  const isFavorite = favoriteIds.has(view.id);
                  return (
                    <TableRow key={view.id}>
                      <TableCell>
                        <Link
                          to="/content/$developerName/views/$viewId"
                          params={{ developerName, viewId: view.id }}
                          className="text-primary hover:underline"
                        >
                          {viewLabel}
                        </Link>
                      </TableCell>
                      <TableCell className="font-mono text-xs">{readString(fields, "developerName")}</TableCell>
                      <TableCell>{readString(fields, "routePath") || "—"}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            type="button"
                            size="sm"
                            variant="ghost"
                            aria-label={isFavorite ? "Remove favorite" : "Add favorite"}
                            onClick={() => favoriteMutation.mutate({ id: view.id, setAsFavorite: !isFavorite })}
                          >
                            <Star className={isFavorite ? "fill-current" : ""} />
                          </Button>
                          <RowActions
                            actions={[
                              {
                                id: "delete",
                                label: "Delete",
                                destructive: true,
                                onSelect: () => setDeleteId(view.id),
                              },
                            ]}
                          />
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )
        }
      </QueryGate>

      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteId(null);
          }
        }}
        title="Delete view?"
        body="This cannot be undone."
        onConfirm={() => {
          if (deleteId) {
            deleteMutation.mutate(deleteId);
          }
        }}
        pending={deleteMutation.isPending}
      />
    </div>
  );
}

export function NewContentViewPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["New view", developerName]);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [label, setLabel] = useState("");
  const [viewDeveloperName, setViewDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [description, setDescription] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.views(developerName).create({
        label,
        developerName: viewDeveloperName || toDeveloperName(label),
        description,
      }),
    onSuccess: (created) => {
      toast.success("View created");
      void queryClient.invalidateQueries({ queryKey: ["content-views", developerName] });
      void navigate({
        to: "/content/$developerName/views/$viewId",
        params: { developerName, viewId: created.id },
      });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="New view" />
      <ListBackLink
        to="/content/$developerName/views"
        params={{ developerName }}
        listKey={`content-views:${developerName}`}
        label="views"
      />
      <ContentTypeNav developerName={developerName} />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Label" required htmlFor="view-label">
              {(control) => (
                <Input
                  {...control}
                  value={label}
                  onChange={(event) => {
                    const next = event.target.value;
                    setLabel(next);
                    if (!developerTouched) {
                      setViewDeveloperName(toDeveloperName(next));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField label="Developer name" required htmlFor="view-developer-name">
              {(control) => (
                <Input
                  {...control}
                  value={viewDeveloperName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setViewDeveloperName(event.target.value);
                  }}
                />
              )}
            </FormField>
            <FormField label="Description" htmlFor="view-description">
              {(control) => (
                <Textarea {...control} value={description} onChange={(event) => setDescription(event.target.value)} />
              )}
            </FormField>
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

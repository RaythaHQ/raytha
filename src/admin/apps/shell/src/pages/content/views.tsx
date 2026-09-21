import { adminApi, formatError } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import {
  Button,
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
import { Link, useParams } from "@tanstack/react-router";
import { Inbox, Star } from "lucide-react";
import { useMemo, useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, readString, toDeveloperName } from "../entity";
import { parseContentTypeSummary } from "./fields-model";
import { ContentTypeNav } from "./nav";

export function ContentViewsPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Views", developerName]);
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [label, setLabel] = useState("");
  const [viewDeveloperName, setViewDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [description, setDescription] = useState("");

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

  const createMutation = useMutation({
    mutationFn: (input: JsonObject) => views.create(input),
    onSuccess: () => {
      toast.success("View created");
      setCreateOpen(false);
      setLabel("");
      setViewDeveloperName("");
      setDescription("");
      setDeveloperTouched(false);
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

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

  const handleCreate = (event: FormEvent) => {
    event.preventDefault();
    createMutation.mutate({
      label,
      developerName: viewDeveloperName || toDeveloperName(label),
      description,
    });
  };

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
          <Button type="button" onClick={() => setCreateOpen(true)}>
            New view
          </Button>
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
                        <div className="flex flex-wrap gap-2">
                          <Button
                            type="button"
                            size="sm"
                            variant="ghost"
                            aria-label={isFavorite ? "Remove favorite" : "Add favorite"}
                            onClick={() => favoriteMutation.mutate({ id: view.id, setAsFavorite: !isFavorite })}
                          >
                            <Star className={isFavorite ? "fill-current" : ""} />
                          </Button>
                          <Button type="button" size="sm" variant="ghost" onClick={() => setDeleteId(view.id)}>
                            Delete
                          </Button>
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

      <Dialog
        open={createOpen}
        onOpenChange={(open) => {
          setCreateOpen(open);
          if (!open) {
            setLabel("");
            setViewDeveloperName("");
            setDescription("");
            setDeveloperTouched(false);
          }
        }}
      >
        <form onSubmit={handleCreate}>
          <DialogHeader>
            <DialogTitle>New view</DialogTitle>
          </DialogHeader>
          <DialogContent className="space-y-4">
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
                <Textarea
                  {...control}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                />
              )}
            </FormField>
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

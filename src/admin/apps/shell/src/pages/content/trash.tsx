import { adminApi, formatError } from "@raytha/api";
import {
  Button,
  ConfirmDialog,
  EmptyState,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import { useState } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { entityFields, formatWhen, readString } from "../entity";
import { parseContentTypeSummary } from "./fields-model";
import { ContentTypeNav } from "./nav";

export function ContentTypeTrashPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Trash", developerName]);
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const items = adminApi.contentItems(developerName);

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const query = useQuery({
    queryKey: ["content-trash", developerName, search],
    queryFn: () => items.trash({ search: search || undefined, pageSize: 50 }),
    enabled: developerName.length > 0,
    placeholderData: keepPreviousData,
  });

  const contentType = parseContentTypeSummary(typeQuery.data);

  const restoreMutation = useMutation({
    mutationFn: (id: string) => items.restore(id),
    onSuccess: () => {
      toast.success("Item restored");
      void queryClient.invalidateQueries({ queryKey: ["content-trash", developerName] });
      void queryClient.invalidateQueries({ queryKey: ["content-items", developerName] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => items.deleteTrash(id),
    onSuccess: () => {
      toast.success("Item permanently deleted");
      setDeleteId(null);
      void queryClient.invalidateQueries({ queryKey: ["content-trash", developerName] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Trash" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${contentType?.labelPlural || developerName} trash`}
        description="Restore a deleted item or remove it permanently."
      />
      <ContentTypeNav developerName={developerName} />
      <QueryGate query={query}>
        {(data) => (
          <ListPanel
            toolbar={
              <>
                <ListSearch
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search trash"
                  aria-label="Search trash"
                />
                <ListStatus total={data.totalCount} page={data.pageNumber} noun="deleted items" />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState
                icon={Trash2}
                title="Trash is empty"
                hint={search ? "Nothing matches that search." : "Deleted items will show up here."}
              />
            ) : (
              <Table flush aria-label="Trash">
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead>Deleted</TableHead>
                    <TableHead className="w-48">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => {
                    const fields = entityFields(item);
                    return (
                      <TableRow key={item.id}>
                        <TableCell>{readString(fields, "primaryField") || item.id}</TableCell>
                        <TableCell>{formatWhen(fields.creationTime) || "—"}</TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-2">
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              loading={restoreMutation.isPending}
                              onClick={() => restoreMutation.mutate(item.id)}
                            >
                              Restore
                            </Button>
                            <Button type="button" size="sm" variant="ghost" onClick={() => setDeleteId(item.id)}>
                              Delete
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            )}
          </ListPanel>
        )}
      </QueryGate>
      <ConfirmDialog
        open={deleteId !== null}
        onOpenChange={(open) => {
          if (!open) {
            setDeleteId(null);
          }
        }}
        title="Delete permanently?"
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

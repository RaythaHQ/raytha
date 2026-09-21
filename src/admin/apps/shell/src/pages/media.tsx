import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Button,
  ConfirmDialog,
  EmptyState,
  FileUpload,
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
import { Image } from "lucide-react";
import { useState } from "react";
import { entityFields, formatWhen, readString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function MediaPage() {
  useDocumentTitle(["Media"]);
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const canManage = hasPermission(platformPermissions.media);

  const query = useQuery({
    queryKey: ["media", search],
    queryFn: () => adminApi.media.list({ search: search || undefined, pageSize: 50 }),
    placeholderData: keepPreviousData,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminApi.media.remove(id),
    onSuccess: () => {
      toast.success("File deleted");
      void queryClient.invalidateQueries({ queryKey: ["media"] });
      setDeleteId(null);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Media" description="Files in the media library." />
      {canManage && (
        <FileUpload
          onUploaded={() => {
            toast.success("Upload complete");
            void queryClient.invalidateQueries({ queryKey: ["media"] });
          }}
        />
      )}
      <QueryGate query={query}>
        {(data) => (
          <ListPanel
            toolbar={
              <>
                <ListSearch
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Search files"
                  aria-label="Search files"
                />
                <ListStatus total={data.totalCount} page={data.pageNumber} noun="files" />
              </>
            }
          >
            {data.items.length === 0 ? (
              <EmptyState icon={Image} title="No files" hint={search ? "Nothing matches that search." : "Upload a file to get started."} />
            ) : (
              <Table flush aria-label="Media">
                <TableHeader>
                  <TableRow>
                    <TableHead>File</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead>Size</TableHead>
                    <TableHead>Uploaded</TableHead>
                    {canManage && <TableHead className="w-28">Actions</TableHead>}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((item) => (
                    <MediaRow key={item.id} item={item} canManage={canManage} onDelete={setDeleteId} />
                  ))}
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
        title="Delete file?"
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

function MediaRow({
  item,
  canManage,
  onDelete,
}: {
  item: EntityRef;
  canManage: boolean;
  onDelete: (id: string) => void;
}) {
  const fields = entityFields(item);
  const name = readString(fields, "fileName", "name") || item.id;
  const url = readString(fields, "url");
  const length = fields.length;
  const size = typeof length === "number" ? formatBytes(length) : "—";

  return (
    <TableRow>
      <TableCell>
        {url ? (
          <a href={url} className="text-primary hover:underline" target="_blank" rel="noreferrer">
            {name}
          </a>
        ) : (
          name
        )}
      </TableCell>
      <TableCell>{readString(fields, "contentType") || "—"}</TableCell>
      <TableCell>{size}</TableCell>
      <TableCell>{formatWhen(fields.creationTime) || "—"}</TableCell>
      {canManage && (
        <TableCell>
          <Button type="button" variant="ghost" size="sm" onClick={() => onDelete(item.id)}>
            Delete
          </Button>
        </TableCell>
      )}
    </TableRow>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  const megabytes = bytes / (1024 * 1024);
  return megabytes >= 1 ? `${megabytes.toFixed(1)} MB` : `${(bytes / 1024).toFixed(1)} KB`;
}

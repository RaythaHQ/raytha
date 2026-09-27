import { adminApi, formatError, hasPermission, platformPermissions } from "@raytha/api";
import type { MediaItem, MediaItemUsage } from "@raytha/api";
import {
  Badge,
  Button,
  buttonVariants,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DangerZone,
  EmptyState,
  FileUpload,
  ListPanel,
  ListSearch,
  ListStatus,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation, useNavigate, useParams } from "@tanstack/react-router";
import type { LucideIcon } from "lucide-react";
import {
  ArrowUpRight,
  ChevronLeft,
  ChevronRight,
  Copy,
  File,
  FileArchive,
  FileAudio,
  FileCode2,
  FileImage,
  FileSpreadsheet,
  FileText,
  FileType2,
  FileVideo,
  Images,
  Palette,
  Upload,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { AppLink, ListBackLink } from "../components/list-back-link";
import { PermissionRequired } from "../components/permission-required";
import { useDocumentTitle } from "../lib/document-title";
import { listHref } from "../lib/list-query";
import { copyText } from "./editors/clipboard";
import { assetSnippet, type AssetSnippetKind } from "./editors/liquid-catalog";
import { formatWhen } from "./entity";

const LIST_KEY = "media";
const PAGE_SIZE = 48;

/** `contentType` is matched server-side as a case-insensitive substring. */
const TYPE_FILTERS = [
  { value: "", label: "All" },
  { value: "image/", label: "Images" },
  { value: "video/", label: "Video" },
  { value: "audio/", label: "Audio" },
  { value: "application/", label: "Documents" },
  { value: "text/", label: "Text" },
] as const;

type MediaSearch = { search?: string; contentType?: string; pageNumber?: number };

export function MediaPage() {
  useDocumentTitle(["Media"]);
  if (!hasPermission(platformPermissions.media)) {
    return <PermissionRequired title="Media" permissionLabel="Manage Media" />;
  }
  return <MediaLibrary />;
}

function MediaLibrary() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const applied = useMemo(() => mediaSearchFromString(location.searchStr), [location.searchStr]);
  const appliedKey = JSON.stringify(compactMediaSearch(applied));
  const appliedSearch = applied.search ?? "";
  const [draftSearch, setDraftSearch] = useState(appliedSearch);
  const [syncedSearch, setSyncedSearch] = useState(appliedSearch);
  const [uploadOpen, setUploadOpen] = useState(false);

  if (syncedSearch !== appliedSearch) {
    setSyncedSearch(appliedSearch);
    setDraftSearch(appliedSearch);
  }

  useEffect(() => {
    try {
      sessionStorage.setItem(`raytha.list:${LIST_KEY}`, appliedKey);
    } catch {
      // sessionStorage can be unavailable
    }
  }, [appliedKey]);

  const write = (next: MediaSearch) => void navigate({ to: ".", search: compactMediaSearch(next), replace: true });

  useEffect(() => {
    if (draftSearch === (applied.search ?? "")) {
      return;
    }
    const handle = window.setTimeout(() => {
      const next = { ...applied, search: draftSearch.trim() ? draftSearch : undefined, pageNumber: 1 };
      void navigate({ to: ".", search: compactMediaSearch(next), replace: true });
    }, 300);
    return () => window.clearTimeout(handle);
  }, [draftSearch, applied, navigate]);

  const query = useQuery({
    queryKey: ["media", appliedKey],
    queryFn: () =>
      adminApi.media.list({
        search: applied.search,
        contentType: applied.contentType,
        pageNumber: applied.pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  });

  const pageNumber = applied.pageNumber ?? 1;
  const pageCount = query.data ? Math.max(1, Math.ceil(query.data.totalCount / PAGE_SIZE)) : 1;
  const filtered = Boolean(applied.search || applied.contentType);
  const showUpload = uploadOpen || (query.data?.totalCount === 0 && !filtered);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Media"
        description="Every file uploaded to this site, including theme assets. Open a file to copy its template snippet or see where it is used."
        actions={
          !showUpload && (
            <Button type="button" onClick={() => setUploadOpen(true)}>
              <Upload aria-hidden />
              Upload files
            </Button>
          )
        }
      />
      {showUpload && (
        <Card>
          <CardHeader className="flex-row items-start justify-between gap-4 space-y-0">
            <div className="space-y-1">
              <CardTitle>Upload files</CardTitle>
              <CardDescription>Uploaded files appear in the library below.</CardDescription>
            </div>
            {uploadOpen && (
              <Button type="button" variant="ghost" size="icon" aria-label="Close upload" onClick={() => setUploadOpen(false)}>
                <X aria-hidden />
              </Button>
            )}
          </CardHeader>
          <CardContent>
            <FileUpload
              onUploaded={() => {
                toast.success("Upload complete");
                void queryClient.invalidateQueries({ queryKey: ["media"] });
              }}
            />
          </CardContent>
        </Card>
      )}
      <ListPanel
        toolbar={
          <>
            <ListSearch
              value={draftSearch}
              onChange={(event) => setDraftSearch(event.target.value)}
              placeholder="Search by file name"
              aria-label="Search files"
            />
            <div role="group" aria-label="Filter by type" className="flex flex-wrap gap-1">
              {TYPE_FILTERS.map((filter) => {
                const active = (applied.contentType ?? "") === filter.value;
                return (
                  <Button
                    key={filter.value || "all"}
                    type="button"
                    size="sm"
                    variant={active ? "secondary" : "ghost"}
                    aria-pressed={active}
                    onClick={() => write({ ...applied, contentType: filter.value || undefined, pageNumber: 1 })}
                  >
                    {filter.label}
                  </Button>
                );
              })}
            </div>
            {query.data && <ListStatus total={query.data.totalCount} page={pageNumber} noun="files" />}
          </>
        }
      >
        <QueryGate query={query}>
          {(data) =>
            data.items.length === 0 ? (
              <EmptyState
                icon={Images}
                title={filtered ? "No matching files" : "No files yet"}
                hint={filtered ? "Try a different search or type." : "Upload a file to start the library."}
              />
            ) : (
              <div className="space-y-4 p-4">
                <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 xl:grid-cols-6" aria-label="Files">
                  {data.items.map((item) => (
                    <li key={item.id}>
                      <MediaTile item={item} />
                    </li>
                  ))}
                </ul>
                {pageCount > 1 && (
                  <nav aria-label="Pages" className="flex items-center justify-end gap-2 text-[13px] text-muted-foreground">
                    <span>
                      Page {pageNumber} of {pageCount}
                    </span>
                    <Button
                      type="button"
                      size="icon"
                      variant="outline"
                      aria-label="Previous page"
                      disabled={pageNumber <= 1}
                      onClick={() => write({ ...applied, pageNumber: pageNumber - 1 })}
                    >
                      <ChevronLeft aria-hidden />
                    </Button>
                    <Button
                      type="button"
                      size="icon"
                      variant="outline"
                      aria-label="Next page"
                      disabled={pageNumber >= pageCount}
                      onClick={() => write({ ...applied, pageNumber: pageNumber + 1 })}
                    >
                      <ChevronRight aria-hidden />
                    </Button>
                  </nav>
                )}
              </div>
            )
          }
        </QueryGate>
      </ListPanel>
    </div>
  );
}

function MediaTile({ item }: { item: MediaItem }) {
  return (
    <AppLink
      href={listHref("/media/$id", { id: item.id })}
      className="group flex h-full flex-col overflow-hidden rounded-lg border border-border bg-card shadow-xs transition hover:border-border-strong hover:shadow-card focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
    >
      <div className="flex aspect-[4/3] items-center justify-center overflow-hidden border-b border-border bg-muted/50">
        {isImage(item) ? (
          <img
            src={item.url}
            alt=""
            loading="lazy"
            className="size-full object-cover transition-transform group-hover:scale-[1.02]"
          />
        ) : (
          <MediaTypeIcon item={item} className="size-10 text-muted-foreground" strokeWidth={1.5} />
        )}
      </div>
      <div className="min-w-0 space-y-0.5 px-3 py-2">
        <p className="truncate text-[13px] font-medium text-foreground" title={item.fileName}>
          {item.fileName}
        </p>
        <p className="truncate text-xs text-muted-foreground">
          {typeLabel(item)} · {formatBytes(item.length)}
        </p>
      </div>
    </AppLink>
  );
}

export function MediaDetailPage() {
  if (!hasPermission(platformPermissions.media)) {
    return <PermissionRequired title="Media" permissionLabel="Manage Media" />;
  }
  return <MediaDetail />;
}

function MediaDetail() {
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const item = useQuery({ queryKey: ["media", "item", id], queryFn: () => adminApi.media.get(id), enabled: id.length > 0 });
  const usage = useQuery({ queryKey: ["media", "item", id, "usage"], queryFn: () => adminApi.media.usage(id), enabled: id.length > 0 });
  useDocumentTitle([item.data?.fileName ?? "File", "Media"]);

  return (
    <QueryGate query={item}>
      {(data) => <MediaDetailView item={data} usage={usage.data} usagePending={usage.isPending} />}
    </QueryGate>
  );
}

function MediaDetailView({
  item,
  usage,
  usagePending,
}: {
  item: MediaItem;
  usage: MediaItemUsage | undefined;
  usagePending: boolean;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const remove = useMutation({
    mutationFn: () => adminApi.media.remove(item.id),
    onSuccess: () => {
      toast.success(`Deleted ${item.fileName}`);
      queryClient.removeQueries({ queryKey: ["media", "item", item.id] });
      void queryClient.invalidateQueries({ queryKey: ["media"] });
      void navigate({ to: "/media" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const templateCount = usage?.webTemplates.length ?? 0;
  const themeCount = usage?.themes.length ?? 0;

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/media" listKey={LIST_KEY} label="media" />}
        title={item.fileName}
        meta={
          <>
            <Badge variant="secondary">{typeLabel(item)}</Badge>
            <span>{formatBytes(item.length)}</span>
            <span>Uploaded {formatWhen(item.creationTime)}</span>
          </>
        }
        actions={
          <a href={item.url} target="_blank" rel="noreferrer" className={buttonVariants({ variant: "outline" })}>
            <ArrowUpRight aria-hidden />
            Open file
          </a>
        }
      />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)]">
        <Card className="overflow-hidden">
          <MediaPreview item={item} />
        </Card>
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Details</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-6 gap-y-2.5 text-[13px]">
                <dt className="text-muted-foreground">File name</dt>
                <dd className="break-all text-foreground">{item.fileName}</dd>
                <dt className="text-muted-foreground">Type</dt>
                <dd className="text-foreground">
                  <code className="font-mono text-xs">{item.contentType}</code>
                </dd>
                <dt className="text-muted-foreground">Size</dt>
                <dd className="text-foreground">
                  {formatBytes(item.length)}
                  {item.length >= 1024 && (
                    <span className="text-muted-foreground"> ({item.length.toLocaleString()} bytes)</span>
                  )}
                </dd>
                <dt className="text-muted-foreground">Uploaded</dt>
                <dd className="text-foreground">{formatWhen(item.creationTime)}</dd>
                <dt className="text-muted-foreground">Storage</dt>
                <dd className="text-foreground">{item.fileStorageProvider}</dd>
                <dt className="text-muted-foreground">Object key</dt>
                <dd className="flex min-w-0 items-start gap-1.5">
                  <code className="min-w-0 break-all font-mono text-xs text-foreground">{item.objectKey}</code>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="-mt-1.5 size-7 shrink-0"
                    aria-label="Copy object key"
                    onClick={() => void copyText(item.objectKey, "Copied object key")}
                  >
                    <Copy aria-hidden />
                  </Button>
                </dd>
              </dl>
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Use in a template</CardTitle>
              <CardDescription>Paste a snippet into any Liquid template.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <SnippetRow
                objectKey={item.objectKey}
                kind="public"
                title="Public URL"
                hint="The file's direct address. Best for images and assets on public storage."
              />
              <SnippetRow
                objectKey={item.objectKey}
                kind="redirect"
                title="Redirect URL"
                hint="A Raytha link that redirects to the file. Works with private cloud storage."
              />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Where it is used</CardTitle>
              <CardDescription>Themes that include this file and web templates that mention its object key.</CardDescription>
            </CardHeader>
            <CardContent>
              <UsageList usage={usage} pending={usagePending} />
            </CardContent>
          </Card>
        </div>
      </div>
      <DangerZone
        actionLabel="Delete file"
        description="Removes the file from storage. Templates, content items, and rich text that link to it will show a broken link or image."
        confirmTitle={`Delete ${item.fileName}?`}
        confirmBody={
          <div className="space-y-2">
            <p>The file is removed from storage and cannot be recovered.</p>
            {templateCount + themeCount > 0 ? (
              <p className="font-medium text-foreground">
                It is used by {usageSummary(themeCount, templateCount)}. Those pages will break until you replace it.
              </p>
            ) : (
              <p>Content items are not scanned, so check any rich text or attachment fields that may link to it.</p>
            )}
          </div>
        }
        confirmLabel="Delete file"
        onConfirm={() => remove.mutate()}
        pending={remove.isPending}
      />
    </div>
  );
}

function MediaPreview({ item }: { item: MediaItem }) {
  const kind = mediaKind(item);
  if (kind === "image") {
    return (
      <div className="flex min-h-72 items-center justify-center bg-[repeating-conic-gradient(var(--color-muted)_0%_25%,transparent_0%_50%)] bg-[length:20px_20px] p-6">
        <img src={item.url} alt={item.fileName} className="max-h-[32rem] max-w-full rounded-md object-contain shadow-card" />
      </div>
    );
  }
  if (kind === "video") {
    return (
      <div className="bg-black">
        <video src={item.url} controls preload="metadata" className="max-h-[32rem] w-full">
          <track kind="captions" />
        </video>
      </div>
    );
  }
  return (
    <div className="flex min-h-72 flex-col items-center justify-center gap-4 bg-muted/40 p-8 text-center">
      <MediaTypeIcon item={item} className="size-16 text-muted-foreground" strokeWidth={1.25} />
      {kind === "audio" ? (
        <audio src={item.url} controls preload="metadata" className="w-full max-w-md">
          <track kind="captions" />
        </audio>
      ) : (
        <p className="text-[13px] text-muted-foreground">No preview for {typeLabel(item)} files. Open the file to view it.</p>
      )}
    </div>
  );
}

function SnippetRow({
  objectKey,
  kind,
  title,
  hint,
}: {
  objectKey: string;
  kind: AssetSnippetKind;
  title: string;
  hint: string;
}) {
  const snippet = assetSnippet(objectKey, kind);
  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between gap-2">
        <p className="text-[13px] font-medium text-foreground">{title}</p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          aria-label={`Copy ${title.toLowerCase()} snippet`}
          onClick={() => void copyText(snippet, `Copied ${title.toLowerCase()} snippet`)}
        >
          <Copy aria-hidden />
          Copy
        </Button>
      </div>
      <code className="block break-all rounded-md border border-border bg-muted/60 px-2.5 py-1.5 font-mono text-xs text-foreground">
        {snippet}
      </code>
      <p className="text-xs text-muted-foreground">{hint}</p>
    </div>
  );
}

function UsageList({ usage, pending }: { usage: MediaItemUsage | undefined; pending: boolean }) {
  if (pending) {
    return <p className="text-[13px] text-muted-foreground">Checking themes and templates…</p>;
  }
  if (!usage) {
    return <p className="text-[13px] text-muted-foreground">Usage could not be loaded.</p>;
  }
  if (usage.themes.length === 0 && usage.webTemplates.length === 0) {
    return <p className="text-[13px] text-muted-foreground">No theme or web template references this file.</p>;
  }
  return (
    <ul className="space-y-1.5 text-[13px]">
      {usage.themes.map((theme) => (
        <li key={`theme-${theme.id}`}>
          <UsageLink href={listHref("/themes/$themeId", { themeId: theme.id })} icon={Palette} label={theme.title} kind="Theme asset" />
        </li>
      ))}
      {usage.webTemplates.map((template) => (
        <li key={`template-${template.id}`}>
          <UsageLink
            href={listHref("/themes/$themeId/web-templates/$id", { themeId: template.themeId, id: template.id })}
            icon={FileCode2}
            label={template.label}
            kind={`Template in ${template.themeTitle}`}
          />
        </li>
      ))}
    </ul>
  );
}

function UsageLink({ href, icon: Icon, label, kind }: { href: string; icon: LucideIcon; label: string; kind: string }) {
  return (
    <AppLink
      href={href}
      className="flex items-center gap-2.5 rounded-md px-2 py-1.5 -mx-2 hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring"
    >
      <Icon aria-hidden className="size-4 shrink-0 text-muted-foreground" />
      <span className="min-w-0 truncate font-medium text-foreground">{label}</span>
      <span className="ml-auto shrink-0 text-xs text-muted-foreground">{kind}</span>
    </AppLink>
  );
}

function usageSummary(themes: number, templates: number): string {
  const parts: string[] = [];
  if (themes > 0) {
    parts.push(themes === 1 ? "1 theme" : `${themes} themes`);
  }
  if (templates > 0) {
    parts.push(templates === 1 ? "1 web template" : `${templates} web templates`);
  }
  return parts.join(" and ");
}

type MediaKind = "image" | "video" | "audio" | "pdf" | "spreadsheet" | "archive" | "code" | "font" | "text" | "other";

const FONT_EXTENSIONS = new Set(["woff", "woff2", "ttf", "otf", "eot"]);

function mediaKind(item: Pick<MediaItem, "contentType" | "fileName">): MediaKind {
  const type = item.contentType.toLowerCase();
  if (type.startsWith("font/") || FONT_EXTENSIONS.has(fileExtension(item.fileName).toLowerCase())) {
    return "font";
  }
  if (type.startsWith("image/")) {
    return "image";
  }
  if (type.startsWith("video/")) {
    return "video";
  }
  if (type.startsWith("audio/")) {
    return "audio";
  }
  if (type === "application/pdf") {
    return "pdf";
  }
  if (type.includes("spreadsheet") || type.includes("excel") || type === "text/csv") {
    return "spreadsheet";
  }
  if (type.includes("zip") || type.includes("compressed") || type.includes("tar")) {
    return "archive";
  }
  if (type.includes("javascript") || type.includes("json") || type.includes("xml") || type === "text/css" || type === "text/html") {
    return "code";
  }
  if (type.startsWith("text/") || type.includes("word") || type.includes("document")) {
    return "text";
  }
  return "other";
}

const kindIcons: Record<MediaKind, LucideIcon> = {
  image: FileImage,
  video: FileVideo,
  audio: FileAudio,
  pdf: FileText,
  spreadsheet: FileSpreadsheet,
  archive: FileArchive,
  code: FileCode2,
  font: FileType2,
  text: FileText,
  other: File,
};

function MediaTypeIcon({ item, className, strokeWidth }: { item: MediaItem; className: string; strokeWidth: number }) {
  const kind = mediaKind(item);
  const Icon = kindIcons[kind];
  return <Icon aria-hidden className={className} strokeWidth={strokeWidth} />;
}

function isImage(item: MediaItem): boolean {
  return mediaKind(item) === "image" && item.url.length > 0;
}

function typeLabel(item: MediaItem): string {
  const extension = fileExtension(item.fileName);
  if (extension) {
    return extension.toUpperCase();
  }
  const subtype = item.contentType.split("/")[1]?.split(/[;+]/)[0];
  return subtype ? subtype.toUpperCase() : "File";
}

function fileExtension(fileName: string): string {
  const dot = fileName.lastIndexOf(".");
  return dot > 0 && dot < fileName.length - 1 ? fileName.slice(dot + 1) : "";
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  const megabytes = bytes / (1024 * 1024);
  return megabytes >= 1 ? `${megabytes.toFixed(1)} MB` : `${(bytes / 1024).toFixed(1)} KB`;
}

function mediaSearchFromString(searchString: string): MediaSearch {
  const params = new URLSearchParams(searchString.startsWith("?") ? searchString.slice(1) : searchString);
  const result: MediaSearch = {};
  const search = params.get("search");
  if (search) {
    result.search = search;
  }
  const contentType = params.get("contentType");
  if (contentType && TYPE_FILTERS.some((filter) => filter.value === contentType)) {
    result.contentType = contentType;
  }
  const page = Number(params.get("pageNumber"));
  if (Number.isFinite(page) && page > 1) {
    result.pageNumber = Math.floor(page);
  }
  return result;
}

function compactMediaSearch(query: MediaSearch): Record<string, string> {
  const search: Record<string, string> = {};
  if (query.search) {
    search.search = query.search;
  }
  if (query.contentType) {
    search.contentType = query.contentType;
  }
  if (query.pageNumber !== undefined && query.pageNumber > 1) {
    search.pageNumber = String(query.pageNumber);
  }
  return search;
}
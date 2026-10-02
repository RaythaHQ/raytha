import { adminApi } from "@raytha/api";
import type { ThemeMediaItem } from "@raytha/api";
import {
  Button,
  cn,
  EmptyState,
  FileUpload,
  Input,
  ListPanel,
  QueryGate,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  toast,
} from "@raytha/ui";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { ArrowUpRight, Copy, FileCode2, FileText, FileType2, Images, Link2, Search, Upload, X } from "lucide-react";
import { useState, type KeyboardEvent, type ReactNode } from "react";
import { AppLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { copyText } from "./clipboard";
import { assetSnippet, type AssetSnippetKind } from "./liquid-catalog";
import { ThemeSectionHeader, useThemeSummary } from "./theme-section-header";

const snippetLabels: Record<AssetSnippetKind, string> = { public: "public URL", redirect: "redirect URL" };

function useThemeAssets(themeId: string) {
  return useQuery({
    queryKey: ["themes", themeId, "media"],
    queryFn: () => adminApi.themes.media(themeId),
    enabled: themeId.length > 0,
  });
}

function useAssetUploads(themeId: string) {
  const queryClient = useQueryClient();
  return (count: number) => {
    toast.success(count === 1 ? "Asset uploaded" : `${count} assets uploaded`);
    void queryClient.invalidateQueries({ queryKey: ["themes", themeId, "media"] });
  };
}

function copyAssetSnippet(asset: ThemeMediaItem, kind: AssetSnippetKind): void {
  void copyText(assetSnippet(asset.objectKey, kind), `Copied ${snippetLabels[kind]} snippet`);
}

export function ThemeAssetsPage() {
  const params = useParams({ strict: false });
  const themeId = "themeId" in params && typeof params.themeId === "string" ? params.themeId : "";
  const theme = useThemeSummary(themeId);
  useDocumentTitle(["Assets", theme?.title ?? "Theme"]);
  const assets = useThemeAssets(themeId);
  const onUploaded = useAssetUploads(themeId);
  const [uploadOpen, setUploadOpen] = useState<boolean | null>(null);
  if (uploadOpen === null && assets.data) {
    setUploadOpen(assets.data.length === 0);
  }
  const showUpload = uploadOpen === true;

  return (
    <div className="space-y-6">
      <ThemeSectionHeader
        themeId={themeId}
        active="assets"
        actions={
          <Button
            type="button"
            variant={showUpload ? "outline" : "default"}
            aria-expanded={showUpload}
            onClick={() => setUploadOpen(!showUpload)}
          >
            {showUpload ? <X aria-hidden /> : <Upload aria-hidden />}
            {showUpload ? "Close upload" : "Upload assets"}
          </Button>
        }
      />
      {showUpload ? (
        <div className="rounded-xl border border-border bg-card p-4 shadow-card">
          <FileUpload
            themeId={themeId}
            height={240}
            note="Files are stored with this theme."
            onUploaded={(files) => onUploaded(files.length)}
          />
        </div>
      ) : null}
      <QueryGate query={assets}>
        {(items) =>
          items.length === 0 ? (
            <EmptyState
              icon={Images}
              title="No assets yet"
              hint="Upload images, stylesheets, scripts, and fonts, then reference them from templates with a Liquid snippet."
            />
          ) : (
            <ListPanel
              toolbar={
                <p className="px-1 text-sm text-muted-foreground">
                  {items.length === 1 ? "1 asset" : `${items.length} assets`}. Copy a snippet and paste it into a
                  template.
                </p>
              }
            >
              <Table flush aria-label="Theme assets">
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-16">
                      <span className="sr-only">Preview</span>
                    </TableHead>
                    <TableHead>File</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead className="text-right">Size</TableHead>
                    <TableHead>Liquid snippet</TableHead>
                    <TableHead className="w-12">
                      <span className="sr-only">Details</span>
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((asset) => (
                    <TableRow key={asset.id}>
                      <TableCell>
                        <AssetThumb asset={asset} className="size-10" />
                      </TableCell>
                      <TableCell className="max-w-[22rem]">
                        <div className="truncate font-medium" title={asset.fileName}>
                          {asset.fileName}
                        </div>
                        <code className="block truncate text-xs text-muted-foreground" title={asset.objectKey}>
                          {asset.objectKey}
                        </code>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{asset.contentType || "Unknown"}</TableCell>
                      <TableCell className="text-right tabular-nums text-muted-foreground">
                        {formatBytes(asset.length)}
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-2">
                          {(["public", "redirect"] as const).map((kind) => (
                            <Button
                              key={kind}
                              type="button"
                              variant="outline"
                              size="sm"
                              aria-label={`Copy ${snippetLabels[kind]} snippet for ${asset.fileName}`}
                              onClick={() => copyAssetSnippet(asset, kind)}
                            >
                              <Copy aria-hidden />
                              {kind === "public" ? "Public" : "Redirect"}
                            </Button>
                          ))}
                        </div>
                      </TableCell>
                      <TableCell>
                        <DetailsLink asset={asset} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </ListPanel>
          )
        }
      </QueryGate>
    </div>
  );
}

/** Compact asset list for the template editor side panel. */
export function ThemeAssetsPanel({
  themeId,
  onInsert,
  searchId,
  onEscape,
}: {
  themeId: string;
  onInsert: (text: string) => void;
  searchId: string;
  onEscape: () => void;
}) {
  const assets = useThemeAssets(themeId);
  const onUploaded = useAssetUploads(themeId);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [query, setQuery] = useState("");
  const items = assets.data ?? [];
  const needle = query.trim().toLowerCase();
  const visible = needle ? items.filter((asset) => asset.fileName.toLowerCase().includes(needle)) : items;

  const onSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter" && visible.length > 0) {
      event.preventDefault();
      onInsert(assetSnippet(visible[0].objectKey, "public"));
    } else if (event.key === "Escape") {
      event.preventDefault();
      if (query) {
        setQuery("");
      } else {
        onEscape();
      }
    }
  };

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="space-y-2 border-b border-border p-3">
        <div className="flex items-center gap-2">
          <div className="relative min-w-0 flex-1">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden />
            <Input
              id={searchId}
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              onKeyDown={onSearchKeyDown}
              placeholder="Filter assets"
              aria-label="Filter assets"
              className="h-8 pl-8 text-[13px]"
            />
          </div>
          <Button
            type="button"
            variant={uploadOpen ? "secondary" : "outline"}
            size="sm"
            aria-expanded={uploadOpen}
            onClick={() => setUploadOpen((open) => !open)}
          >
            <Upload aria-hidden />
            Upload
          </Button>
        </div>
        <p className="text-xs text-muted-foreground">Click a file to insert its public URL at the cursor.</p>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        {uploadOpen ? (
          <div className="border-b border-border p-3">
            <FileUpload themeId={themeId} height={220} onUploaded={(files) => onUploaded(files.length)} />
          </div>
        ) : null}
        {assets.isPending ? (
          <p className="p-4 text-sm text-muted-foreground">Loading assets…</p>
        ) : items.length === 0 ? (
          <div className="p-4">
            <EmptyState
              icon={Images}
              title="No assets yet"
              hint="Upload files to this theme to reference them here."
              className="py-8"
            />
          </div>
        ) : visible.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">No assets match “{query}”.</p>
        ) : (
          <ul className="divide-y divide-border">
            {visible.map((asset) => (
              <li key={asset.id} className="group flex items-center gap-1 pr-2 hover:bg-muted/60 focus-within:bg-muted/60">
                <button
                  type="button"
                  className="flex min-w-0 flex-1 items-center gap-3 px-3 py-2 text-left focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
                  onClick={() => onInsert(assetSnippet(asset.objectKey, "public"))}
                  aria-label={`Insert public URL snippet for ${asset.fileName}`}
                >
                  <AssetThumb asset={asset} className="size-8" />
                  <span className="min-w-0">
                    <span className="block truncate text-[13px] font-medium">{asset.fileName}</span>
                    <span className="block truncate text-xs text-muted-foreground">
                      {formatBytes(asset.length)} · {asset.contentType || "file"}
                    </span>
                  </span>
                </button>
                <IconAction label={`Copy public URL snippet for ${asset.fileName}`} onClick={() => copyAssetSnippet(asset, "public")}>
                  <Copy aria-hidden />
                </IconAction>
                <IconAction
                  label={`Copy redirect URL snippet for ${asset.fileName}`}
                  onClick={() => copyAssetSnippet(asset, "redirect")}
                >
                  <Link2 aria-hidden />
                </IconAction>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function IconAction({ label, onClick, children }: { label: string; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      onClick={onClick}
      className="flex size-7 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-card hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring [&_svg]:size-3.5"
    >
      {children}
    </button>
  );
}

function DetailsLink({ asset }: { asset: ThemeMediaItem }) {
  return (
    <AppLink
      href={`/raytha/media/${encodeURIComponent(asset.id)}`}
      className="inline-flex size-8 items-center justify-center rounded-md text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
    >
      <ArrowUpRight className="size-4" aria-hidden />
      <span className="sr-only">{`Open ${asset.fileName} in the media library`}</span>
    </AppLink>
  );
}

function AssetThumb({ asset, className }: { asset: ThemeMediaItem; className?: string }) {
  const frame = cn(
    "flex shrink-0 items-center justify-center overflow-hidden rounded-md border border-border bg-muted text-muted-foreground",
    className,
  );
  if (asset.contentType.startsWith("image/") && asset.url) {
    return (
      <span className={frame}>
        <img src={asset.url} alt="" loading="lazy" className="size-full object-cover" />
      </span>
    );
  }
  const Icon = /css|javascript|json|html|xml/.test(asset.contentType)
    ? FileCode2
    : /font/.test(asset.contentType)
      ? FileType2
      : FileText;
  return (
    <span className={frame}>
      <Icon className="size-4" aria-hidden />
    </span>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  const units = ["KB", "MB", "GB"];
  let value = bytes / 1024;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }
  return `${value >= 10 ? value.toFixed(0) : value.toFixed(1)} ${units[unit]}`;
}

import { adminApi } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import { Card, CardContent, CardHeader, CardTitle, PageHeader, QueryGate } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { isRecord, jsonBoolean, jsonNumber, jsonString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function DashboardPage() {
  useDocumentTitle(["Dashboard"]);
  const query = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => adminApi.dashboard(),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Dashboard" description="Overview of your site." />
      <QueryGate query={query}>{(data) => <DashboardMetrics data={data} />}</QueryGate>
    </div>
  );
}

function DashboardMetrics({ data }: { data: JsonObject }) {
  const fileStorage = isRecord(data.fileStorage) ? data.fileStorage : {};
  const database = isRecord(data.database) ? data.database : {};

  const usedBytes = jsonNumber(fileStorage, "usedBytes") ?? 0;
  const maxFileBytes = jsonNumber(fileStorage, "maxFileSizeBytes");
  const filePercent = jsonNumber(fileStorage, "percentUsed") ?? 0;
  const dbPercent = jsonNumber(database, "percentUsed") ?? 0;
  const usedMb = jsonNumber(database, "usedMb") ?? 0;
  const maxMb = jsonNumber(database, "maxMb") ?? 0;
  const usedGb = jsonNumber(fileStorage, "usedGb") ?? 0;
  const maxGb = jsonNumber(fileStorage, "maxGb") ?? 0;
  const mimeTypes = jsonString(fileStorage, "allowedMimeTypes")
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean);

  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      <MetricCard title="Content items" value={String(jsonNumber(data, "totalContentItems") ?? 0)} />
      <MetricCard title="Users" value={String(jsonNumber(data, "totalUsers") ?? 0)} />
      <MetricCard
        title="File storage"
        value={`${formatBytes(usedBytes)} of ${formatGigabytes(maxGb)}`}
        hint={`${formatPercent(filePercent)} used · ${jsonString(fileStorage, "providerName") || "storage"}`}
        progress={filePercent}
      />
      <MetricCard
        title="Database"
        value={`${formatMegabytes(usedMb)} of ${formatMegabytes(maxMb)}`}
        hint={`${formatPercent(dbPercent)} used`}
        progress={dbPercent}
      />
      <MetricCard
        title="Largest upload"
        value={maxFileBytes === undefined ? "—" : formatBytes(maxFileBytes)}
        hint={
          jsonBoolean(fileStorage, "useDirectUploadToCloud")
            ? "Direct cloud uploads enabled"
            : "Uploads go through the server"
        }
      />
      <Card className="sm:col-span-2 xl:col-span-1">
        <CardHeader>
          <CardTitle className="text-sm font-medium text-muted-foreground">Allowed file types</CardTitle>
        </CardHeader>
        <CardContent>
          {mimeTypes.length === 0 ? (
            <p className="text-sm text-muted-foreground">No restrictions reported.</p>
          ) : (
            <ul className="flex flex-wrap gap-1.5">
              {mimeTypes.map((type) => (
                <li
                  key={type}
                  className="rounded-md bg-muted px-2 py-0.5 font-mono text-xs text-foreground"
                >
                  {type}
                </li>
              ))}
            </ul>
          )}
          {usedGb > 0 && usedGb < 0.01 ? (
            <p className="mt-3 text-xs text-muted-foreground">{formatGigabytes(usedGb)} on disk</p>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}

function MetricCard({
  title,
  value,
  hint,
  progress,
}: {
  title: string;
  value: string;
  hint?: string;
  progress?: number;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-2">
        <p className="break-words font-display text-2xl font-semibold">{value}</p>
        {hint ? <p className="text-sm text-muted-foreground">{hint}</p> : null}
        {progress !== undefined ? (
          <div
            className="h-1.5 overflow-hidden rounded-full bg-muted"
            role="progressbar"
            aria-valuenow={Math.round(progress)}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label={`${title} usage`}
          >
            <div
              className="h-full rounded-full bg-primary transition-[width]"
              style={{ width: `${Math.min(100, Math.max(0, progress))}%` }}
            />
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}

function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) {
    return "—";
  }
  if (bytes < 1000) {
    return `${bytes} B`;
  }
  const units = ["KB", "MB", "GB", "TB"];
  let value = bytes;
  let unitIndex = -1;
  while (value >= 1000 && unitIndex < units.length - 1) {
    value /= 1000;
    unitIndex += 1;
  }
  const digits = value >= 100 ? 0 : value >= 10 ? 1 : 2;
  return `${value.toFixed(digits)} ${units[unitIndex]}`;
}

function formatMegabytes(mb: number): string {
  if (!Number.isFinite(mb)) {
    return "—";
  }
  if (mb >= 1000) {
    return formatGigabytes(mb / 1000);
  }
  return `${trimNumber(mb)} MB`;
}

function formatGigabytes(gb: number): string {
  if (!Number.isFinite(gb)) {
    return "—";
  }
  return `${trimNumber(gb)} GB`;
}

function formatPercent(value: number): string {
  if (!Number.isFinite(value)) {
    return "0%";
  }
  return `${trimNumber(value)}%`;
}

function trimNumber(value: number): string {
  if (Math.abs(value) >= 100) {
    return value.toFixed(0);
  }
  if (Math.abs(value) >= 10) {
    return value.toFixed(1).replace(/\.0$/, "");
  }
  return value.toFixed(2).replace(/\.?0+$/, "") || "0";
}

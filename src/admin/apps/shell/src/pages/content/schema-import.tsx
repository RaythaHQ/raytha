import { adminApi, formatError, problemMessages } from "@raytha/api";
import type { JsonObject, SchemaChange, SchemaImportResult } from "@raytha/api";
import { Badge, Button, Card, CardContent, FileDrop, toast } from "@raytha/ui";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { readSchemaFile, type SchemaFileResult } from "./schema-file";

const KIND_LABEL: Record<SchemaChange["kind"], string> = {
  content_type: "Content type",
  field: "Field",
  view: "View",
};

function plural(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? "" : "s"}`;
}

function groupByContentType(changes: SchemaChange[]): [string, SchemaChange[]][] {
  const groups = new Map<string, SchemaChange[]>();
  for (const change of changes) {
    groups.set(change.contentType, [...(groups.get(change.contentType) ?? []), change]);
  }
  return [...groups];
}

/**
 * Import mode of the new-content-type page. Choosing a file asks the server for a dry run, which
 * is shown as the preview; Apply imports the same document. The server enforces the permission and
 * is all-or-nothing, so a preview with errors can never half-apply.
 */
export function ImportSchemaPanel() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [file, setFile] = useState<SchemaFileResult | null>(null);
  const [document, setDocument] = useState<JsonObject | null>(null);

  const preview = useMutation({
    mutationFn: (schema: JsonObject) => adminApi.contentTypes.importSchema(schema, true),
  });

  const apply = useMutation({
    mutationFn: (schema: JsonObject) => adminApi.contentTypes.importSchema(schema, false),
    onSuccess: (result) => {
      toast.success(`Schema imported: ${result.created} created, ${result.updated} updated`);
      void queryClient.invalidateQueries({ queryKey: ["content-types"] });
      void queryClient.invalidateQueries({ queryKey: ["content-type"] });
      void navigate({ to: "/content-types" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleFile = (chosen: File | null) => {
    preview.reset();
    apply.reset();
    setDocument(null);
    if (!chosen) {
      setFile(null);
      return;
    }
    void chosen.text().then((text) => {
      const read = readSchemaFile(text);
      setFile(read);
      if (read.ok) {
        setDocument(read.document);
        preview.mutate(read.document);
      }
    });
  };

  const result = preview.data;
  const errors = preview.isError ? problemMessages(preview.error) : [];
  const failure = preview.isError && errors.length === 0 ? formatError(preview.error) : null;
  const changeCount = result ? result.created + result.updated : 0;

  return (
    <Card className="max-w-3xl">
      <CardContent className="space-y-5 pt-6">
        <p className="text-sm text-muted-foreground">
          Upload a schema file exported from Raytha. Content types, fields, choices, and views are matched by developer
          name: missing ones are created and existing ones are updated. Nothing is deleted, and a field&apos;s type is
          never changed.
        </p>
        <FileDrop onFileChange={handleFile} allowedFileTypes={[".json", "application/json"]} note="A .json schema file." />

        {file && !file.ok ? <ErrorBox messages={[file.error]} /> : null}
        {file?.ok ? (
          <p className="text-sm text-muted-foreground">
            {plural(file.contentTypes, "content type")}, {plural(file.fields, "field")}, {plural(file.views, "view")}.
          </p>
        ) : null}

        {preview.isPending ? <p className="text-sm text-muted-foreground">Checking the file against this site…</p> : null}
        {errors.length > 0 ? (
          <ErrorBox heading="Nothing was imported. Fix these and choose the file again:" messages={errors} />
        ) : null}
        {failure ? <ErrorBox messages={[failure]} /> : null}
        {result ? <Preview result={result} /> : null}

        <div className="flex gap-2">
          <Button
            type="button"
            disabled={!document || !result || changeCount === 0}
            loading={apply.isPending}
            onClick={() => document && apply.mutate(document)}
          >
            Import schema
          </Button>
          <Button type="button" variant="outline" onClick={() => void navigate({ to: "/content-types" })}>
            Cancel
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function ErrorBox({ heading, messages }: { heading?: string; messages: string[] }) {
  return (
    <div role="alert" className="space-y-1.5 rounded-lg border border-destructive/40 bg-destructive/5 px-3 py-2 text-sm text-destructive">
      {heading ? <p className="font-medium">{heading}</p> : null}
      <ul className="list-disc space-y-0.5 pl-5">
        {messages.map((message) => (
          <li key={message}>{message}</li>
        ))}
      </ul>
    </div>
  );
}

function Preview({ result }: { result: SchemaImportResult }) {
  if (result.created + result.updated === 0) {
    return (
      <div className="space-y-2">
        <p className="text-sm font-medium">This site already matches the file. There is nothing to import.</p>
        <Warnings warnings={result.warnings} />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <p className="text-sm font-medium">This import will:</p>
        <Badge variant="success">{result.created} created</Badge>
        <Badge variant="info">{result.updated} updated</Badge>
        <Badge variant="neutral">{result.unchanged} unchanged</Badge>
      </div>
      <Warnings warnings={result.warnings} />
      <div className="space-y-3">
        {groupByContentType(result.changes).map(([contentType, changes]) => (
          <div key={contentType} className="rounded-lg border border-border">
            <p className="border-b border-border bg-muted/50 px-3 py-1.5 font-mono text-xs">{contentType}</p>
            <ul className="divide-y divide-border">
              {changes.map((change) => (
                <li
                  key={`${change.kind}:${change.name ?? ""}`}
                  className="flex flex-wrap items-baseline gap-x-2 px-3 py-1.5 text-sm"
                >
                  <Badge variant={change.action === "created" ? "success" : "info"}>{change.action}</Badge>
                  <span>{KIND_LABEL[change.kind]}</span>
                  {change.name ? <code className="font-mono text-xs">{change.name}</code> : null}
                  {change.details.length > 0 ? (
                    <span className="text-xs text-muted-foreground">changes {change.details.join(", ")}</span>
                  ) : null}
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </div>
  );
}

function Warnings({ warnings }: { warnings: string[] }) {
  if (warnings.length === 0) {
    return null;
  }
  return (
    <div className="space-y-1 rounded-lg border border-warning-border bg-warning-soft px-3 py-2 text-sm text-warning">
      <p className="font-medium">Heads up</p>
      <ul className="list-disc space-y-0.5 pl-5">
        {warnings.map((warning) => (
          <li key={warning}>{warning}</li>
        ))}
      </ul>
    </div>
  );
}

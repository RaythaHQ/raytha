import { adminApi, CSV_IMPORT_METHODS, formatError } from "@raytha/api";
import type { CsvImportMethod } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  cn,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { useState } from "react";
import { BackgroundTaskStatus } from "../../components/background-task-status";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { fileToBase64, importMethodLabel } from "./csv";
import { parseContentTypeSummary, type ContentTypeSummary } from "./fields-model";

const IMPORT_METHOD_HINTS: Record<CsvImportMethod, string> = {
  add_new_records_only: "Every row becomes a new item. Rows whose Id already exists are skipped.",
  update_existing_records_only: "Rows update the item with the matching Id. Rows without a match are skipped.",
  upsert_all_records: "Rows with a matching Id update that item; everything else is added.",
};

export function ContentTypeImportPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Import CSV", developerName]);

  const typeQuery = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });
  const contentType = parseContentTypeSummary(typeQuery.data);
  const plural = contentType?.labelPlural || developerName;

  return (
    <div className="space-y-6">
      <PageHeader
        back={
          <ListBackLink
            to="/content/$developerName"
            params={{ developerName }}
            listKey={`content-items:${developerName}`}
            label={plural}
          />
        }
        title={`Import ${plural} from CSV`}
        description="The import runs in the background. You can leave this page; progress stays in Background tasks."
      />
      <QueryGate query={typeQuery}>
        {() =>
          contentType ? (
            <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
              <ImportForm contentType={contentType} />
              <ColumnsHelp contentType={contentType} />
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">This content type could not be read.</p>
          )
        }
      </QueryGate>
    </div>
  );
}

function ImportForm({ contentType }: { contentType: ContentTypeSummary }) {
  const [importMethod, setImportMethod] = useState<CsvImportMethod>("add_new_records_only");
  const [file, setFile] = useState<File | null>(null);
  const [taskId, setTaskId] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: async (importAsDraft: boolean) => {
      if (!file) {
        throw new Error("Choose a CSV file.");
      }
      const csvAsBytes = await fileToBase64(file);
      return adminApi.contentItems(contentType.developerName).importCsv({
        contentTypeId: contentType.id,
        importMethod,
        importAsDraft,
        csvAsBytes,
      });
    },
    onSuccess: (result) => {
      toast.success("Import started");
      setTaskId(result.id);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <Card>
      <CardContent className="space-y-5 pt-6">
        <fieldset className="space-y-2">
          <legend className="mb-2 text-sm font-medium">Import method</legend>
          {CSV_IMPORT_METHODS.map((method) => (
            <label
              key={method}
              htmlFor={`csv-import-method-${method}`}
              className={cn(
                "grid cursor-pointer grid-cols-[auto_1fr] gap-x-3 rounded-lg border px-3 py-2.5 transition-colors",
                importMethod === method ? "border-primary bg-primary/5" : "border-border hover:bg-muted/50",
              )}
            >
              <input
                id={`csv-import-method-${method}`}
                type="radio"
                name="csv-import-method"
                value={method}
                checked={importMethod === method}
                onChange={() => setImportMethod(method)}
                className="row-span-2 mt-1 accent-primary"
              />
              <span className="text-sm font-medium">{importMethodLabel(method)}</span>
              <span className="text-xs text-muted-foreground">{IMPORT_METHOD_HINTS[method]}</span>
            </label>
          ))}
        </fieldset>
        <FormField label="CSV file" required htmlFor="csv-import-file" hint="UTF-8 CSV with a header row.">
          {(control) => (
            <Input
              {...control}
              type="file"
              accept=".csv,text/csv"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            />
          )}
        </FormField>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            disabled={!file}
            loading={mutation.isPending && mutation.variables === false}
            onClick={() => mutation.mutate(false)}
          >
            Import and publish
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={!file}
            loading={mutation.isPending && mutation.variables === true}
            onClick={() => mutation.mutate(true)}
          >
            Import as drafts
          </Button>
        </div>
        {taskId ? <BackgroundTaskStatus taskId={taskId} /> : null}
      </CardContent>
    </Card>
  );
}

function ColumnsHelp({ contentType }: { contentType: ContentTypeSummary }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Expected columns</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <p className="text-muted-foreground">Header names are developer names. Unknown columns are ignored.</p>
        <ul className="space-y-1.5">
          <ColumnRow name="Template" note="required, template developer name" />
          <ColumnRow name="Id" note="required to update" />
          {contentType.fields.map((field) => (
            <ColumnRow key={field.id} name={field.developerName} note={field.isRequired ? "required" : undefined} />
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}

function ColumnRow({ name, note }: { name: string; note?: string }) {
  return (
    <li className="flex items-baseline justify-between gap-2">
      <code className="font-mono text-xs">{name}</code>
      {note && <span className="text-xs text-muted-foreground">{note}</span>}
    </li>
  );
}

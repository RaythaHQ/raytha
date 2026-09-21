import { adminApi, CSV_IMPORT_METHODS, formatError } from "@raytha/api";
import type { CsvImportMethod } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  Checkbox,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  Textarea,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { useEffect, useState, type FormEvent } from "react";
import { BackgroundTaskStatus } from "../../components/background-task-status";
import { useDocumentTitle } from "../../lib/document-title";
import { fileToBase64, importMethodLabel, parseCsvImportMethod } from "./csv";
import { parseContentTypeSummary } from "./fields-model";
import { ContentTypeNav } from "./nav";

type SettingsForm = {
  labelSingular: string;
  labelPlural: string;
  description: string;
  defaultRouteTemplate: string;
  primaryFieldId: string;
};

export function ContentTypeConfigurationPage() {
  const params = useParams({ strict: false });
  const developerName = typeof params.developerName === "string" ? params.developerName : "";
  useDocumentTitle(["Settings", developerName]);
  const queryClient = useQueryClient();
  const [form, setForm] = useState<SettingsForm | null>(null);

  const query = useQuery({
    queryKey: ["content-type", developerName],
    queryFn: () => adminApi.contentTypes.byDeveloperName(developerName),
    enabled: developerName.length > 0,
  });

  const contentType = parseContentTypeSummary(query.data);

  useEffect(() => {
    if (!contentType) {
      return;
    }
    setForm({
      labelSingular: contentType.labelSingular,
      labelPlural: contentType.labelPlural,
      description: contentType.description,
      defaultRouteTemplate: contentType.defaultRouteTemplate,
      primaryFieldId: contentType.primaryFieldId,
    });
  }, [contentType]);

  const mutation = useMutation({
    mutationFn: (input: SettingsForm) => adminApi.contentTypes.updateByDeveloperName(developerName, input),
    onSuccess: () => {
      toast.success("Content type saved");
      void queryClient.invalidateQueries({ queryKey: ["content-type", developerName] });
      void queryClient.invalidateQueries({ queryKey: ["content-types"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (form) {
      mutation.mutate(form);
    }
  };

  const textFields = (contentType?.fields ?? []).filter((field) => field.fieldType === "single_line_text");

  if (!developerName) {
    return (
      <div className="space-y-6">
        <PageHeader title="Content type settings" />
        <p className="text-sm text-muted-foreground">Pick a content type from the list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${contentType?.labelPlural || developerName} settings`}
        description="Labels, primary field, and the default public route template."
      />
      <ContentTypeNav developerName={developerName} />
      <QueryGate query={query}>
        {() =>
          form ? (
            <Card>
              <CardContent className="pt-6">
                <form className="space-y-4" onSubmit={handleSubmit}>
                  <FormField label="Singular label" required htmlFor="ct-label-singular">
                    {(control) => (
                      <Input
                        {...control}
                        value={form.labelSingular}
                        onChange={(event) => setForm({ ...form, labelSingular: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField label="Plural label" required htmlFor="ct-label-plural">
                    {(control) => (
                      <Input
                        {...control}
                        value={form.labelPlural}
                        onChange={(event) => setForm({ ...form, labelPlural: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField label="Description" htmlFor="ct-description">
                    {(control) => (
                      <Textarea
                        {...control}
                        value={form.description}
                        onChange={(event) => setForm({ ...form, description: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField
                    label="Default route template"
                    required
                    htmlFor="ct-route"
                    hint="Tokens: {ContentTypeDeveloperName}, {PrimaryField}, {Id}, {CurrentYear}, {CurrentMonth}"
                  >
                    {(control) => (
                      <Input
                        {...control}
                        value={form.defaultRouteTemplate}
                        onChange={(event) => setForm({ ...form, defaultRouteTemplate: event.target.value })}
                      />
                    )}
                  </FormField>
                  <FormField
                    label="Primary field"
                    required
                    htmlFor="ct-primary-field"
                    hint="Must be a single line text field."
                  >
                    {(control) => (
                      <Select
                        {...control}
                        value={form.primaryFieldId}
                        onChange={(event) => setForm({ ...form, primaryFieldId: event.target.value })}
                      >
                        <option value="">Select a field</option>
                        {textFields.map((field) => (
                          <option key={field.id} value={field.id}>
                            {field.label || field.developerName}
                          </option>
                        ))}
                      </Select>
                    )}
                  </FormField>
                  <Button type="submit" loading={mutation.isPending}>
                    Save
                  </Button>
                </form>
              </CardContent>
            </Card>
          ) : (
            <p className="text-sm text-muted-foreground">This content type could not be read.</p>
          )
        }
      </QueryGate>
      {contentType ? <CsvImportCard contentTypeId={contentType.id} developerName={developerName} /> : null}
    </div>
  );
}

function CsvImportCard({ contentTypeId, developerName }: { contentTypeId: string; developerName: string }) {
  const [importMethod, setImportMethod] = useState<CsvImportMethod>("add_new_records_only");
  const [importAsDraft, setImportAsDraft] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [taskId, setTaskId] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: async () => {
      if (!file) {
        throw new Error("Choose a CSV file.");
      }
      const csvAsBytes = await fileToBase64(file);
      return adminApi.contentItems(developerName).importCsv({
        contentTypeId,
        importMethod,
        importAsDraft,
        csvAsBytes,
      });
    },
    onSuccess: (result) => {
      toast.success(`Import started. Task ${result.id}`);
      setTaskId(result.id);
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div>
          <h2 className="text-lg font-semibold">Import CSV</h2>
          <p className="text-sm text-muted-foreground">
            Posts the file bytes to the import command. Include a <code>Template</code> column; an{" "}
            <code>Id</code> column is required when updating existing records.
          </p>
        </div>
        <FormField label="Import method" required htmlFor="csv-import-method">
          {(control) => (
            <Select
              {...control}
              value={importMethod}
              onChange={(event) => {
                const next = parseCsvImportMethod(event.target.value);
                if (next) {
                  setImportMethod(next);
                }
              }}
            >
              {CSV_IMPORT_METHODS.map((method) => (
                <option key={method} value={method}>
                  {importMethodLabel(method)}
                </option>
              ))}
            </Select>
          )}
        </FormField>
        <div className="flex items-center gap-2">
          <Checkbox
            id="csv-import-draft"
            checked={importAsDraft}
            onCheckedChange={(checked) => setImportAsDraft(checked)}
          />
          <label htmlFor="csv-import-draft" className="text-sm">
            Import as draft
          </label>
        </div>
        <FormField label="CSV file" required htmlFor="csv-import-file">
          {(control) => (
            <Input
              {...control}
              type="file"
              accept=".csv,text/csv"
              onChange={(event) => {
                const next = event.target.files?.[0] ?? null;
                setFile(next);
              }}
            />
          )}
        </FormField>
        <Button type="button" loading={mutation.isPending} onClick={() => mutation.mutate()}>
          Import CSV
        </Button>
        {taskId ? <BackgroundTaskStatus taskId={taskId} /> : null}
      </CardContent>
    </Card>
  );
}

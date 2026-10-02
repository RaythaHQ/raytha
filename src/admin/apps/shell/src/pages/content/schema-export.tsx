import { adminApi, formatError } from "@raytha/api";
import { Button, toast } from "@raytha/ui";
import { useMutation } from "@tanstack/react-query";
import { Download } from "lucide-react";
import { downloadJson } from "../../lib/download";
import { schemaFileName } from "./schema-file";

/** Downloads every content type, field, choice, and view as one JSON file. */
export function ExportSchemaButton() {
  const mutation = useMutation({
    mutationFn: adminApi.contentTypes.exportSchema,
    onSuccess: (schema) => downloadJson(schemaFileName(), schema),
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <Button
      type="button"
      variant="outline"
      loading={mutation.isPending}
      title="Download every content type, field, and view as a JSON file"
      onClick={() => mutation.mutate()}
    >
      <Download aria-hidden />
      Export schema
    </Button>
  );
}

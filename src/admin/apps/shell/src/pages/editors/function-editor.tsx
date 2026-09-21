import { adminApi, formatError } from "@raytha/api";
import type { FunctionDetail, TemplateRevision } from "@raytha/api";
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
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { useEffect, useState, type FormEvent } from "react";
import { useDocumentTitle } from "../../lib/document-title";
import { CodeEditor } from "./code-editor";
import { RevisionsPanel } from "./revisions-panel";

const FUNCTION_TRIGGERS = [
  { value: "http_request", label: "HTTP request" },
  { value: "liquid_template", label: "Liquid template" },
  { value: "content_item_created", label: "Content item created" },
  { value: "content_item_updated", label: "Content item updated" },
  { value: "content_item_deleted", label: "Content item deleted" },
] as const;

export function FunctionEditorPage() {
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["function", id],
    queryFn: () => adminApi.functions.get(id),
    enabled: id.length > 0,
  });
  const revisions = useQuery({
    queryKey: ["function-revisions", id],
    queryFn: () => adminApi.functions.revisions(id, { pageSize: 50 }),
    enabled: id.length > 0,
  });

  useDocumentTitle([query.data?.name ?? "Function"]);

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Function" />
        <p className="text-sm text-muted-foreground">Pick a function from the list.</p>
      </div>
    );
  }

  return (
    <QueryGate query={query}>
      {(fn) => <FunctionEditor fn={fn} revisions={revisions.data?.items ?? []} />}
    </QueryGate>
  );
}

function FunctionEditor({ fn, revisions }: { fn: FunctionDetail; revisions: TemplateRevision[] }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(fn.name);
  const [triggerType, setTriggerType] = useState(fn.triggerType || FUNCTION_TRIGGERS[0].value);
  const [isActive, setIsActive] = useState(fn.isActive);
  const [code, setCode] = useState(fn.code);

  useEffect(() => {
    setName(fn.name);
    setTriggerType(fn.triggerType || FUNCTION_TRIGGERS[0].value);
    setIsActive(fn.isActive);
    setCode(fn.code);
  }, [fn]);

  const save = useMutation({
    mutationFn: () => adminApi.functions.update(fn.id, { name, triggerType, isActive, code }),
    onSuccess: () => {
      toast.success("Function saved");
      void queryClient.invalidateQueries({ queryKey: ["function", fn.id] });
      void queryClient.invalidateQueries({ queryKey: ["function-revisions", fn.id] });
      void queryClient.invalidateQueries({ queryKey: ["functions"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const revert = useMutation({
    mutationFn: (revisionId: string) => adminApi.functions.revert(revisionId),
    onSuccess: () => {
      toast.success("Reverted to that revision");
      void queryClient.invalidateQueries({ queryKey: ["function", fn.id] });
      void queryClient.invalidateQueries({ queryKey: ["function-revisions", fn.id] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    save.mutate();
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={fn.name || fn.developerName || "Function"}
        description={fn.developerName}
        actions={
          <Button type="submit" form="function-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <p className="text-sm">
        <Link to="/functions" className="text-primary hover:underline">
          Back to functions
        </Link>
      </p>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <Card>
          <CardContent className="space-y-4 pt-6">
            <form id="function-form" className="space-y-4" onSubmit={handleSubmit}>
              <FormField label="Name" required htmlFor="function-name">
                {(control) => <Input {...control} value={name} onChange={(event) => setName(event.target.value)} />}
              </FormField>
              <FormField label="Trigger" required htmlFor="function-trigger">
                {(control) => (
                  <Select {...control} value={triggerType} onChange={(event) => setTriggerType(event.target.value)}>
                    {FUNCTION_TRIGGERS.map((trigger) => (
                      <option key={trigger.value} value={trigger.value}>
                        {trigger.label}
                      </option>
                    ))}
                  </Select>
                )}
              </FormField>
              <div className="flex items-center gap-2">
                <Checkbox id="function-active" checked={isActive} onCheckedChange={setIsActive} />
                <label htmlFor="function-active" className="text-sm">
                  Active
                </label>
              </div>
              <CodeEditor value={code} onChange={setCode} language="javascript" ariaLabel="Function code" />
            </form>
          </CardContent>
        </Card>
        <RevisionsPanel
          revisions={revisions}
          pendingId={revert.isPending ? (revert.variables ?? null) : null}
          onRevert={(revisionId) => revert.mutate(revisionId)}
        />
      </div>
    </div>
  );
}

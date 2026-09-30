import { adminApi, formatError } from "@raytha/api";
import type { FunctionDetail, TemplateRevision } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  Checkbox,
  DangerZone,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  Select,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "@tanstack/react-router";
import type { ReactCodeMirrorRef } from "@uiw/react-codemirror";
import { useRef, useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { CodeEditor, insertAtCursor } from "./code-editor";
import { FunctionPublicPath, publicPathFor } from "./function-public-path";
import { FUNCTION_TRIGGERS, missingEntryPoint, triggerFor } from "./function-reference";
import { FunctionReferencePanel } from "./function-reference-panel";
import { RevisionsPanel } from "./revisions-panel";

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
  const navigate = useNavigate();
  const [name, setName] = useState(fn.name);
  const editorRef = useRef<ReactCodeMirrorRef>(null);
  const [rail, setRail] = useState("reference");
  const [triggerType, setTriggerType] = useState(triggerFor(fn.triggerType).value);
  const [isActive, setIsActive] = useState(fn.isActive);
  const [code, setCode] = useState(fn.code);
  const [routePath, setRoutePath] = useState(fn.routePath);
  const [loaded, setLoaded] = useState(fn);
  const entryWarning = missingEntryPoint(triggerType, code);

  if (loaded !== fn) {
    setLoaded(fn);
    setName(fn.name);
    setTriggerType(triggerFor(fn.triggerType).value);
    setIsActive(fn.isActive);
    setCode(fn.code);
    setRoutePath(fn.routePath);
  }

  const save = useMutation({
    mutationFn: () =>
      adminApi.functions.update(fn.id, {
        name,
        triggerType,
        isActive,
        code,
        routePath: publicPathFor(triggerType, routePath),
      }),
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

  const remove = useMutation({
    mutationFn: () => adminApi.functions.remove(fn.id),
    onSuccess: () => {
      toast.success("Function deleted");
      void queryClient.invalidateQueries({ queryKey: ["functions"] });
      void navigate({ to: "/functions" });
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
        back={<ListBackLink to="/functions" listKey="functions" label="functions" />}
        title={fn.name || fn.developerName || "Function"}
        description={fn.developerName}
        actions={
          <Button type="submit" form="function-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_24rem] lg:items-start">
        <Card>
          <CardContent className="space-y-4 pt-6">
            <form id="function-form" className="space-y-4" onSubmit={handleSubmit}>
              <FormField label="Name" required htmlFor="function-name">
                {(control) => <Input {...control} value={name} onChange={(event) => setName(event.target.value)} />}
              </FormField>
              <FormField label="Trigger" required htmlFor="function-trigger">
                {(control) => (
                  <Select
                    {...control}
                    value={triggerType}
                    onChange={(event) => setTriggerType(triggerFor(event.target.value).value)}
                  >
                    {FUNCTION_TRIGGERS.map((trigger) => (
                      <option key={trigger.value} value={trigger.value}>
                        {trigger.label}
                      </option>
                    ))}
                  </Select>
                )}
              </FormField>
              <FunctionPublicPath
                id="function-public-path"
                trigger={triggerType}
                value={routePath}
                savedPath={fn.routePath}
                onChange={setRoutePath}
              />
              <div className="flex items-center gap-2">
                <Checkbox id="function-active" checked={isActive} onCheckedChange={setIsActive} />
                <label htmlFor="function-active" className="text-sm">
                  Active
                </label>
              </div>
              <div className="space-y-2">
                <CodeEditor
                  value={code}
                  onChange={setCode}
                  language="javascript"
                  editorRef={editorRef}
                  ariaLabel="Function code"
                />
                {entryWarning ? <p className="text-sm text-warning">{entryWarning}</p> : null}
              </div>
            </form>
          </CardContent>
        </Card>
        <Tabs value={rail} onValueChange={setRail} className="space-y-3">
          <TabsList aria-label="Side panel">
            <TabsTrigger value="reference">Reference</TabsTrigger>
            <TabsTrigger value="revisions">Revisions ({revisions.length})</TabsTrigger>
          </TabsList>
          <TabsContent value="reference">
            <FunctionReferencePanel
              trigger={triggerType}
              developerName={fn.developerName}
              onInsert={(text) => insertAtCursor(editorRef.current, text)}
            />
          </TabsContent>
          <TabsContent value="revisions">
            <RevisionsPanel
              revisions={revisions}
              pendingId={revert.isPending ? (revert.variables ?? null) : null}
              onRevert={(revisionId) => revert.mutate(revisionId)}
            />
          </TabsContent>
        </Tabs>
      </div>
      <DangerZone
        description="Delete this function. This cannot be undone."
        actionLabel="Delete function"
        confirmTitle="Delete function?"
        confirmBody="This cannot be undone."
        onConfirm={() => remove.mutate()}
        pending={remove.isPending}
      />
    </div>
  );
}

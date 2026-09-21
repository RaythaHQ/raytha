import { adminApi, formatError } from "@raytha/api";
import type { EmailTemplateDetail, TemplateRevision } from "@raytha/api";
import { Button, Card, CardContent, FormField, Input, PageHeader, QueryGate, toast } from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { useEffect, useRef, useState, type FormEvent } from "react";
import type { ReactCodeMirrorRef } from "@uiw/react-codemirror";
import { useDocumentTitle } from "../../lib/document-title";
import { CodeEditor, insertAtCursor } from "./code-editor";
import { RevisionsPanel } from "./revisions-panel";

export function EmailTemplateEditorPage() {
  const params = useParams({ strict: false });
  const id = "id" in params && typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["email-template", id],
    queryFn: () => adminApi.emailTemplates.get(id),
    enabled: id.length > 0,
  });
  const revisions = useQuery({
    queryKey: ["email-template-revisions", id],
    queryFn: () => adminApi.emailTemplates.revisions(id, { pageSize: 50 }),
    enabled: id.length > 0,
  });

  useDocumentTitle([query.data?.subject ?? "Email template"]);

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Email template" />
        <p className="text-sm text-muted-foreground">Pick a template from the list.</p>
      </div>
    );
  }

  return (
    <QueryGate query={query}>
      {(template) => (
        <EmailTemplateEditor
          template={template}
          revisions={revisions.data?.items ?? []}
        />
      )}
    </QueryGate>
  );
}

function EmailTemplateEditor({
  template,
  revisions,
}: {
  template: EmailTemplateDetail;
  revisions: TemplateRevision[];
}) {
  const queryClient = useQueryClient();
  const editorRef = useRef<ReactCodeMirrorRef>(null);
  const [subject, setSubject] = useState(template.subject);
  const [content, setContent] = useState(template.content);
  const [cc, setCc] = useState(template.cc);
  const [bcc, setBcc] = useState(template.bcc);

  useEffect(() => {
    setSubject(template.subject);
    setContent(template.content);
    setCc(template.cc);
    setBcc(template.bcc);
  }, [template]);

  const save = useMutation({
    mutationFn: () =>
      adminApi.emailTemplates.update(template.id, { subject, content, cc, bcc }),
    onSuccess: () => {
      toast.success("Email template saved");
      void queryClient.invalidateQueries({ queryKey: ["email-template", template.id] });
      void queryClient.invalidateQueries({ queryKey: ["email-template-revisions", template.id] });
      void queryClient.invalidateQueries({ queryKey: ["email-templates"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const revert = useMutation({
    mutationFn: (revisionId: string) => adminApi.emailTemplates.revert(revisionId),
    onSuccess: () => {
      toast.success("Reverted to that revision");
      void queryClient.invalidateQueries({ queryKey: ["email-template", template.id] });
      void queryClient.invalidateQueries({ queryKey: ["email-template-revisions", template.id] });
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
        title={template.subject || template.developerName || "Email template"}
        description={template.developerName}
        actions={
          <Button type="submit" form="email-template-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <p className="text-sm">
        <Link to="/email-templates" className="text-primary hover:underline">
          Back to email templates
        </Link>
      </p>
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_18rem]">
        <Card>
          <CardContent className="space-y-4 pt-6">
            <form id="email-template-form" className="space-y-4" onSubmit={handleSubmit}>
              <FormField label="Subject" required htmlFor="email-subject">
                {(control) => <Input {...control} value={subject} onChange={(event) => setSubject(event.target.value)} />}
              </FormField>
              <FormField label="CC" htmlFor="email-cc">
                {(control) => <Input {...control} value={cc} onChange={(event) => setCc(event.target.value)} />}
              </FormField>
              <FormField label="BCC" htmlFor="email-bcc">
                {(control) => <Input {...control} value={bcc} onChange={(event) => setBcc(event.target.value)} />}
              </FormField>
              {template.availableVariables ? (
                <div className="space-y-2">
                  <p className="text-sm font-medium">Insert variable</p>
                  <div className="flex flex-wrap gap-2">
                    {template.availableVariables.map((variable) => (
                      <Button
                        key={variable}
                        type="button"
                        variant="outline"
                        size="sm"
                        onClick={() => insertAtCursor(editorRef.current, liquidVariable(variable))}
                      >
                        {variable}
                      </Button>
                    ))}
                  </div>
                </div>
              ) : null}
              <CodeEditor
                editorRef={editorRef}
                value={content}
                onChange={setContent}
                language="liquid"
                ariaLabel="Email template content"
              />
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

function liquidVariable(variable: string): string {
  if (variable.includes("{{") || variable.includes("{%")) {
    return variable;
  }
  return `{{ ${variable} }}`;
}

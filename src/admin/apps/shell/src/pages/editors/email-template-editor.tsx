import { adminApi, formatError } from "@raytha/api";
import type { EmailTemplateDetail, TemplateRevision } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  FormField,
  Input,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { ListBackLink } from "../../components/list-back-link";
import { useDocumentTitle } from "../../lib/document-title";
import { RevisionsPanel } from "./revisions-panel";
import { TemplateWorkbench } from "./template-workbench";

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
  const [subject, setSubject] = useState(template.subject);
  const [content, setContent] = useState(template.content);
  const [cc, setCc] = useState(template.cc);
  const [bcc, setBcc] = useState(template.bcc);

  const [synced, setSynced] = useState(template);
  if (synced !== template) {
    setSynced(template);
    setSubject(template.subject);
    setContent(template.content);
    setCc(template.cc);
    setBcc(template.bcc);
  }

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
        back={<ListBackLink to="/email-templates" listKey="email-templates" label="email templates" />}
        title={template.subject || template.developerName || "Email template"}
        meta={template.developerName ? <code>{template.developerName}</code> : undefined}
        actions={
          <Button type="submit" form="email-template-form" loading={save.isPending}>
            Save
          </Button>
        }
      />
      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <Card>
          <CardHeader>
            <CardTitle>Envelope</CardTitle>
          </CardHeader>
          <CardContent>
            <form id="email-template-form" className="grid gap-5 md:grid-cols-2" onSubmit={handleSubmit}>
              <div className="md:col-span-2">
                <FormField label="Subject" required htmlFor="email-subject">
                  {(control) => (
                    <Input {...control} value={subject} onChange={(event) => setSubject(event.target.value)} />
                  )}
                </FormField>
              </div>
              <FormField label="CC" htmlFor="email-cc">
                {(control) => <Input {...control} value={cc} onChange={(event) => setCc(event.target.value)} />}
              </FormField>
              <FormField label="BCC" htmlFor="email-bcc">
                {(control) => <Input {...control} value={bcc} onChange={(event) => setBcc(event.target.value)} />}
              </FormField>
            </form>
          </CardContent>
        </Card>
        <RevisionsPanel
          revisions={revisions}
          pendingId={revert.isPending ? (revert.variables ?? null) : null}
          onRevert={(revisionId) => revert.mutate(revisionId)}
        />
      </div>
      <TemplateWorkbench
        title="Message body"
        value={content}
        onChange={setContent}
        ariaLabel="Email template content"
        variables={template.availableVariables}
        onSave={() => save.mutate()}
      />
    </div>
  );
}

import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Checkbox,
  DangerZone,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { ArrowRight, Copy, KeyRound, TriangleAlert } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { ListBackLink } from "../components/list-back-link";
import { CrudListPage } from "./crud-list";
import { copyText } from "./editors/clipboard";
import { entityFields, readBoolean, readString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export { AuditLogPage } from "./audit-log";
export { EmailLogPage } from "./email-log";
export { BackgroundTasksPage } from "./background-tasks";

export function WebhooksPage() {
  return (
    <CrudListPage
      title="Webhooks"
      queryKey={["webhooks"]}
      listKey="webhooks"
      noun="webhook"
      list={adminApi.webhooks.list}
      createPermission={platformPermissions.systemSettings}
      createLabel="New webhook"
      createTo="/webhooks/new"
      columns={[
        {
          header: "Name",
          cell: (entity) => (
            <Link to="/webhooks/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "name") || entity.id}
            </Link>
          ),
        },
        { header: "URL", cell: (entity) => readString(entityFields(entity), "url") },
        {
          header: "Status",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isActive") ? (
              <Badge variant="success">Active</Badge>
            ) : (
              <Badge variant="secondary">Inactive</Badge>
            ),
        },
      ]}
    />
  );
}

export function NewWebhookPage() {
  useDocumentTitle(["New webhook"]);
  const navigate = useNavigate();
  const [form, setForm] = useState(emptyWebhookForm);

  const openWebhook = (id: string) => void navigate({ to: "/webhooks/$id", params: { id } });

  const mutation = useMutation({
    mutationFn: () => adminApi.webhooks.create(webhookPayload(form)),
    onSuccess: (created) => {
      if (!readString(entityFields(created), "secret")) {
        toast.success("Webhook created");
        openWebhook(created.id);
      }
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const created = mutation.data;
  const secret = created ? readString(entityFields(created), "secret") : "";
  if (created && secret) {
    return (
      <div className="space-y-6">
        <PageHeader
          title="Webhook created"
          description={`${form.name || "This webhook"} will send signed events to ${form.url}.`}
        />
        <WebhookSecretReveal secret={secret} onContinue={() => openWebhook(created.id)} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/webhooks" listKey="webhooks" label="webhooks" />} title="New webhook" />
      <Card>
        <CardContent className="pt-6">
          <WebhookForm
            form={form}
            setForm={setForm}
            pending={mutation.isPending}
            submitLabel="Create"
            onSubmit={() => mutation.mutate()}
          />
        </CardContent>
      </Card>
    </div>
  );
}

function WebhookSecretReveal({ secret, onContinue }: { secret: string; onContinue: () => void }) {
  useEffect(() => {
    document.getElementById("webhook-secret")?.focus();
  }, []);

  return (
    <Card className="max-w-3xl">
      <CardHeader>
        <div className="flex items-center gap-2">
          <KeyRound className="size-4 text-muted-foreground" aria-hidden />
          <CardTitle>Signing secret</CardTitle>
        </div>
        <CardDescription>
          Each delivery carries an <code className="font-mono text-foreground">X-Raytha-Signature</code> header, an
          HMAC-SHA256 of the request body made with this secret. Your endpoint uses it to verify the request came from
          Raytha.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div
          role="note"
          className="flex gap-3 rounded-lg border border-warning-border bg-warning-soft px-4 py-3 text-sm text-warning"
        >
          <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
          <p>
            <span className="font-medium">Copy it now. It will not be shown again.</span> The webhook page never
            displays it. If you lose it, delete this webhook and create a new one.
          </p>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="webhook-secret">Secret</Label>
          <div className="flex gap-2">
            <Input
              id="webhook-secret"
              readOnly
              value={secret}
              spellCheck={false}
              autoComplete="off"
              className="font-mono"
              onFocus={(event) => event.currentTarget.select()}
            />
            <Button type="button" variant="outline" onClick={() => void copyText(secret, "Signing secret copied")}>
              <Copy aria-hidden />
              Copy
            </Button>
          </div>
        </div>
      </CardContent>
      <CardFooter className="justify-end border-t border-border pt-4">
        <Button type="button" onClick={onContinue}>
          Continue to webhook
          <ArrowRight aria-hidden />
        </Button>
      </CardFooter>
    </Card>
  );
}

export function EditWebhookPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Edit webhook"]);
  const query = useQuery({
    queryKey: ["webhooks", id],
    queryFn: () => adminApi.webhooks.get(id),
    enabled: id.length > 0,
  });

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit webhook" />
        <p className="text-sm text-muted-foreground">Missing webhook id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/webhooks" listKey="webhooks" label="webhooks" />} title="Edit webhook" />
      <QueryGate query={query}>{(webhook) => <WebhookEditForm webhook={webhook} />}</QueryGate>
    </div>
  );
}

function WebhookEditForm({ webhook }: { webhook: EntityRef }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [form, setForm] = useState(() => formFromWebhook(webhook));
  const name = readString(entityFields(webhook), "name") || "this webhook";

  const remove = useMutation({
    mutationFn: () => adminApi.webhooks.remove(webhook.id),
    onSuccess: () => {
      toast.success("Webhook deleted");
      queryClient.removeQueries({ queryKey: ["webhooks", webhook.id] });
      void queryClient.invalidateQueries({ queryKey: ["webhooks"] });
      void navigate({ to: "/webhooks" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const mutation = useMutation({
    mutationFn: () => adminApi.webhooks.update(webhook.id, webhookPayload(form)),
    onSuccess: () => {
      toast.success("Webhook saved");
      void queryClient.invalidateQueries({ queryKey: ["webhooks"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <>
      <Card>
        <CardContent className="pt-6">
          <WebhookForm
            form={form}
            setForm={setForm}
            pending={mutation.isPending}
            submitLabel="Save"
            onSubmit={() => mutation.mutate()}
          />
        </CardContent>
      </Card>
      <DangerZone
        description="Delete this webhook and its delivery history. Pending deliveries are not sent. To pause it instead, clear Active."
        actionLabel="Delete webhook"
        confirmTitle={`Delete ${name}?`}
        confirmBody="Its delivery history is deleted too. This cannot be undone."
        onConfirm={() => remove.mutate()}
        pending={remove.isPending}
      />
    </>
  );
}

type WebhookFormState = {
  name: string;
  url: string;
  description: string;
  isActive: boolean;
  subscribedEvents: string;
  maxAttempts: string;
  timeoutSeconds: string;
};

const emptyWebhookForm: WebhookFormState = {
  name: "",
  url: "",
  description: "",
  isActive: true,
  subscribedEvents: "*",
  maxAttempts: "5",
  timeoutSeconds: "30",
};

function formFromWebhook(webhook: EntityRef): WebhookFormState {
  const fields = entityFields(webhook);
  const events = fields.subscribedEvents;
  return {
    name: readString(fields, "name"),
    url: readString(fields, "url"),
    description: readString(fields, "description"),
    isActive: readBoolean(fields, "isActive"),
    subscribedEvents: Array.isArray(events)
      ? events.filter((item): item is string => typeof item === "string").join(",")
      : "*",
    maxAttempts: String(typeof fields.maxAttempts === "number" ? fields.maxAttempts : 5),
    timeoutSeconds: String(typeof fields.timeoutSeconds === "number" ? fields.timeoutSeconds : 30),
  };
}

function webhookPayload(form: WebhookFormState) {
  return {
    name: form.name,
    url: form.url,
    description: form.description,
    isActive: form.isActive,
    subscribedEvents: form.subscribedEvents
      .split(",")
      .map((item) => item.trim())
      .filter(Boolean),
    maxAttempts: Number(form.maxAttempts) || 5,
    timeoutSeconds: Number(form.timeoutSeconds) || 30,
  };
}

function WebhookForm({
  form,
  setForm,
  pending,
  submitLabel,
  onSubmit,
}: {
  form: WebhookFormState;
  setForm: (next: WebhookFormState) => void;
  pending: boolean;
  submitLabel: string;
  onSubmit: () => void;
}) {
  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    onSubmit();
  };
  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      <FormField label="Name" required htmlFor="webhook-name">
        {(control) => (
          <Input {...control} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
        )}
      </FormField>
      <FormField label="URL" required htmlFor="webhook-url">
        {(control) => (
          <Input
            {...control}
            type="url"
            value={form.url}
            onChange={(event) => setForm({ ...form, url: event.target.value })}
          />
        )}
      </FormField>
      <FormField label="Description" htmlFor="webhook-description">
        {(control) => (
          <Input
            {...control}
            value={form.description}
            onChange={(event) => setForm({ ...form, description: event.target.value })}
          />
        )}
      </FormField>
      <FormField
        label="Subscribed events"
        hint="Comma-separated event names, or * for all."
        htmlFor="webhook-events"
      >
        {(control) => (
          <Input
            {...control}
            value={form.subscribedEvents}
            onChange={(event) => setForm({ ...form, subscribedEvents: event.target.value })}
          />
        )}
      </FormField>
      <FormField label="Max attempts" htmlFor="webhook-attempts">
        {(control) => (
          <Input
            {...control}
            type="number"
            value={form.maxAttempts}
            onChange={(event) => setForm({ ...form, maxAttempts: event.target.value })}
          />
        )}
      </FormField>
      <FormField label="Timeout (seconds)" htmlFor="webhook-timeout">
        {(control) => (
          <Input
            {...control}
            type="number"
            value={form.timeoutSeconds}
            onChange={(event) => setForm({ ...form, timeoutSeconds: event.target.value })}
          />
        )}
      </FormField>
      <div className="flex items-center gap-2">
        <Checkbox
          id="webhook-active"
          checked={form.isActive}
          onCheckedChange={(checked) => setForm({ ...form, isActive: checked })}
        />
        <Label htmlFor="webhook-active">Active</Label>
      </div>
      <Button type="submit" loading={pending}>
        {submitLabel}
      </Button>
    </form>
  );
}

import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { ListBackLink } from "../components/list-back-link";
import { CrudListPage } from "./crud-list";
import { entityFields, formatWhen, humanizeAuditCategory, readBoolean, readString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function AuditLogPage() {
  return (
    <CrudListPage
      title="Audit log"
      queryKey={["audit-logs"]}
      listKey="audit-logs"
      noun="entry"
      list={adminApi.auditLogs.list}
      columns={[
        { header: "When", cell: (entity) => formatWhen(entityFields(entity).creationTime) || "—" },
        { header: "Category", cell: (entity) => humanizeAuditCategory(readString(entityFields(entity), "category") || "—") },
        { header: "User", cell: (entity) => readString(entityFields(entity), "userEmail") || "—" },
        { header: "IP", cell: (entity) => readString(entityFields(entity), "ipAddress") || "—" },
      ]}
    />
  );
}

export function WebhooksPage() {
  return (
    <CrudListPage
      title="Webhooks"
      queryKey={["webhooks"]}
      listKey="webhooks"
      noun="webhook"
      list={adminApi.webhooks.list}
      remove={adminApi.webhooks.remove}
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

  const mutation = useMutation({
    mutationFn: () => adminApi.webhooks.create(webhookPayload(form)),
    onSuccess: (created) => {
      const secret = readString(entityFields(created), "secret");
      toast.success(secret ? `Webhook created. Secret: ${secret}` : "Webhook created");
      void navigate({ to: "/webhooks/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="New webhook" />
      <ListBackLink to="/webhooks" listKey="webhooks" label="webhooks" />
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
      <PageHeader title="Edit webhook" />
      <ListBackLink to="/webhooks" listKey="webhooks" label="webhooks" />
      <QueryGate query={query}>{(webhook) => <WebhookEditForm webhook={webhook} />}</QueryGate>
    </div>
  );
}

function WebhookEditForm({ webhook }: { webhook: EntityRef }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState(() => formFromWebhook(webhook));

  const mutation = useMutation({
    mutationFn: () => adminApi.webhooks.update(webhook.id, webhookPayload(form)),
    onSuccess: () => {
      toast.success("Webhook saved");
      void queryClient.invalidateQueries({ queryKey: ["webhooks"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
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

export function EmailLogPage() {
  return (
    <CrudListPage
      title="Email log"
      queryKey={["email-log"]}
      listKey="email-log"
      noun="message"
      list={adminApi.emailLog.list}
      columns={[
        { header: "When", cell: (entity) => formatWhen(entityFields(entity).creationTime) || "—" },
        { header: "To", cell: (entity) => readString(entityFields(entity), "toAddress") || "—" },
        { header: "Subject", cell: (entity) => readString(entityFields(entity), "subject") || "—" },
        {
          header: "Status",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isSuccess") ? (
              <Badge variant="success">Sent</Badge>
            ) : (
              <Badge variant="destructive">Failed</Badge>
            ),
        },
      ]}
    />
  );
}

export function BackgroundTasksPage() {
  useDocumentTitle(["Background tasks"]);

  return (
    <CrudListPage
      title="Background tasks"
      queryKey={["background-tasks"]}
      listKey="background-tasks"
      noun="task"
      list={adminApi.backgroundTasks.list}
      emptyHint="No background tasks yet."
      columns={[
        { header: "Name", cell: (entity) => readString(entityFields(entity), "name") || entity.id },
        {
          header: "Status",
          cell: (entity) => {
            const fields = entityFields(entity);
            const status = fields.status;
            if (typeof status === "string") {
              return status;
            }
            if (status && typeof status === "object" && "label" in status && typeof status.label === "string") {
              return status.label;
            }
            return readString(fields, "status") || "—";
          },
        },
        {
          header: "Progress",
          cell: (entity) => {
            const fields = entityFields(entity);
            const percent = fields.percentComplete;
            return typeof percent === "number" ? `${percent}%` : "—";
          },
        },
        {
          header: "Updated",
          cell: (entity) => {
            const fields = entityFields(entity);
            return formatWhen(fields.lastModificationTime) || formatWhen(fields.creationTime) || "—";
          },
        },
      ]}
    />
  );
}

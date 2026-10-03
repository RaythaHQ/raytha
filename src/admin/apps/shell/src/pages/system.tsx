import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef, WebhookEventGroup } from "@raytha/api";
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
  cn,
  DangerZone,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  Skeleton,
  Switch,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { ArrowRight, Copy, KeyRound, Send, TriangleAlert } from "lucide-react";
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
      <PageHeader
        back={<ListBackLink to="/webhooks" listKey="webhooks" label="webhooks" />}
        title="New webhook"
        description="Raytha sends a signed HTTP POST to your URL every time one of the events you pick happens."
      />
      <WebhookIntro />
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

/** What a webhook is and how a receiver verifies one, in the space of a short card. */
function WebhookIntro() {
  return (
    <Card className="bg-muted/30">
      <CardContent className="grid gap-4 pt-6 text-sm md:grid-cols-3">
        <div className="space-y-1">
          <p className="font-medium">What is delivered</p>
          <p className="text-muted-foreground">
            A JSON body with <code className="font-mono text-foreground">result</code>,{" "}
            <code className="font-mono text-foreground">request</code>, and{" "}
            <code className="font-mono text-foreground">requestType</code> for the command that ran. Passwords,
            secrets, tokens, and API keys are stripped before sending.
          </p>
        </div>
        <div className="space-y-1">
          <p className="font-medium">How to verify it</p>
          <p className="text-muted-foreground">
            Compute HMAC-SHA256 of <code className="font-mono text-foreground">{"<X-Raytha-Timestamp>.<raw body>"}</code>{" "}
            with the signing secret shown once at creation, and compare it in constant time to{" "}
            <code className="font-mono text-foreground">X-Raytha-Signature</code>. Reject stale timestamps as replays
            and dedupe on <code className="font-mono text-foreground">X-Raytha-Delivery</code>.
          </p>
        </div>
        <div className="space-y-1">
          <p className="font-medium">Retries</p>
          <p className="text-muted-foreground">
            A non-2xx response or a timeout is retried with exponential backoff (2 s base, 60 s cap) until the attempt
            limit. Delivery is at-least-once. Every attempt is recorded under{" "}
            <Link to="/maintenance" className="underline-offset-4 hover:underline">
              Maintenance
            </Link>
            .
          </p>
        </div>
      </CardContent>
    </Card>
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
          HMAC-SHA256 made with this secret over the{" "}
          <code className="font-mono text-foreground">X-Raytha-Timestamp</code> header value, a period, and the raw
          request body. Your endpoint uses it to verify the request came from Raytha, and rejects a delivery whose
          timestamp is too old as a replay.
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
      <TestDeliveryCard webhookId={webhook.id} url={form.url} />
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

/** Fires `webhook.test` at the endpoint and follows the delivery until it settles. */
function TestDeliveryCard({ webhookId, url }: { webhookId: string; url: string }) {
  const [deliveryId, setDeliveryId] = useState<string | null>(null);
  const fire = useMutation({
    mutationFn: () => adminApi.webhooks.test(webhookId),
    onSuccess: (result) => setDeliveryId(result.id),
    onError: (error) => toast.error(formatError(error)),
  });
  const delivery = useQuery({
    queryKey: ["webhook-delivery", deliveryId],
    queryFn: () => adminApi.webhooks.delivery(deliveryId ?? ""),
    enabled: deliveryId !== null,
    refetchInterval: (query) => {
      const status = query.state.data ? readString(entityFields(query.state.data), "status") : "";
      return status === "pending" || status === "" ? 2000 : false;
    },
  });
  const fields = delivery.data ? entityFields(delivery.data) : null;
  const status = fields ? readString(fields, "status") : "";
  const responseCode = fields && typeof fields.responseCode === "number" ? fields.responseCode : null;
  const attempts = fields && typeof fields.attemptCount === "number" ? fields.attemptCount : 0;
  const errorMessage = fields ? readString(fields, "errorMessage") : "";

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Send className="size-4 text-muted-foreground" aria-hidden />
          <CardTitle>Send a test event</CardTitle>
        </div>
        <CardDescription>
          Posts a <code className="font-mono text-foreground">webhook.test</code> event to{" "}
          <span className="break-all font-mono text-foreground">{url || "this URL"}</span>, signed like a real
          delivery, regardless of the subscribed events. Use it to confirm your endpoint receives the request and
          verifies the signature. Save first if you changed the URL.
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-wrap items-center gap-3">
        <Button type="button" variant="outline" onClick={() => fire.mutate()} loading={fire.isPending}>
          <Send aria-hidden />
          Send test event
        </Button>
        {deliveryId ? (
          status === "succeeded" ? (
            <p className="text-sm text-success" role="status">
              Delivered{responseCode !== null ? ` (HTTP ${responseCode})` : ""} after {attempts}{" "}
              {attempts === 1 ? "attempt" : "attempts"}.
            </p>
          ) : status === "failed" ? (
            <p className="text-sm text-destructive" role="alert">
              Failed after {attempts} {attempts === 1 ? "attempt" : "attempts"}
              {responseCode !== null ? ` (HTTP ${responseCode})` : ""}
              {errorMessage ? `: ${errorMessage}` : "."}
            </p>
          ) : (
            <p className="text-sm text-muted-foreground" role="status" aria-live="polite">
              Delivering{attempts > 0 ? ` (attempt ${attempts})` : ""}…
            </p>
          )
        ) : null}
      </CardContent>
    </Card>
  );
}

type WebhookFormState = {
  name: string;
  url: string;
  description: string;
  isActive: boolean;
  /** `true` subscribes to every event (`*`); otherwise `events` lists the chosen names. */
  allEvents: boolean;
  events: string[];
  maxAttempts: string;
  timeoutSeconds: string;
};

const MAX_ATTEMPTS_RANGE = { min: 1, max: 10 } as const;
const TIMEOUT_RANGE = { min: 1, max: 120 } as const;

const emptyWebhookForm: WebhookFormState = {
  name: "",
  url: "",
  description: "",
  isActive: true,
  allEvents: true,
  events: [],
  maxAttempts: "5",
  timeoutSeconds: "30",
};

function formFromWebhook(webhook: EntityRef): WebhookFormState {
  const fields = entityFields(webhook);
  const stored = Array.isArray(fields.subscribedEvents)
    ? fields.subscribedEvents.filter((item): item is string => typeof item === "string")
    : ["*"];
  const allEvents = stored.includes("*");
  return {
    name: readString(fields, "name"),
    url: readString(fields, "url"),
    description: readString(fields, "description"),
    isActive: readBoolean(fields, "isActive"),
    allEvents,
    events: allEvents ? [] : stored,
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
    subscribedEvents: form.allEvents ? ["*"] : form.events,
    maxAttempts: Number(form.maxAttempts) || 5,
    timeoutSeconds: Number(form.timeoutSeconds) || 30,
  };
}

function inRange(value: string, range: { min: number; max: number }): boolean {
  const parsed = /^\d+$/.test(value) ? Number(value) : Number.NaN;
  return Number.isInteger(parsed) && parsed >= range.min && parsed <= range.max;
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
  const catalog = useQuery({ queryKey: ["webhook-events"], queryFn: () => adminApi.webhooks.events() });
  const attemptsValid = inRange(form.maxAttempts, MAX_ATTEMPTS_RANGE);
  const timeoutValid = inRange(form.timeoutSeconds, TIMEOUT_RANGE);
  const eventsValid = form.allEvents || form.events.length > 0;
  const canSubmit = attemptsValid && timeoutValid && eventsValid;

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (canSubmit) {
      onSubmit();
    }
  };
  return (
    <form className="space-y-6" onSubmit={handleSubmit}>
      <div className="space-y-4">
        <FormField label="Name" required htmlFor="webhook-name" hint="Shown in the delivery log; pick something you will recognise.">
          {(control) => (
            <Input {...control} value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
          )}
        </FormField>
        <FormField label="URL" required htmlFor="webhook-url" hint="An absolute http(s) URL that accepts a JSON POST and answers 2xx.">
          {(control) => (
            <Input
              {...control}
              type="url"
              placeholder="https://example.com/hooks/raytha"
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
      </div>

      <WebhookEventPicker
        groups={catalog.data}
        loading={catalog.isPending}
        error={catalog.isError ? formatError(catalog.error) : null}
        allEvents={form.allEvents}
        selected={form.events}
        onChange={(allEvents, events) => setForm({ ...form, allEvents, events })}
      />

      <div className="grid gap-4 sm:grid-cols-2">
        <FormField
          label="Max attempts"
          htmlFor="webhook-attempts"
          hint={`How many times a failed delivery is tried before it is marked failed (${MAX_ATTEMPTS_RANGE.min}–${MAX_ATTEMPTS_RANGE.max}). Retries back off exponentially from 2 s up to 60 s.`}
          error={attemptsValid ? undefined : `Use a whole number from ${MAX_ATTEMPTS_RANGE.min} to ${MAX_ATTEMPTS_RANGE.max}.`}
        >
          {(control) => (
            <Input
              {...control}
              type="number"
              inputMode="numeric"
              min={MAX_ATTEMPTS_RANGE.min}
              max={MAX_ATTEMPTS_RANGE.max}
              step={1}
              value={form.maxAttempts}
              onChange={(event) => setForm({ ...form, maxAttempts: event.target.value })}
            />
          )}
        </FormField>
        <FormField
          label="Timeout (seconds)"
          htmlFor="webhook-timeout"
          hint={`How long one attempt waits for your endpoint to respond (${TIMEOUT_RANGE.min}–${TIMEOUT_RANGE.max}). A slow endpoint counts as a failed attempt.`}
          error={timeoutValid ? undefined : `Use a whole number from ${TIMEOUT_RANGE.min} to ${TIMEOUT_RANGE.max}.`}
        >
          {(control) => (
            <Input
              {...control}
              type="number"
              inputMode="numeric"
              min={TIMEOUT_RANGE.min}
              max={TIMEOUT_RANGE.max}
              step={1}
              value={form.timeoutSeconds}
              onChange={(event) => setForm({ ...form, timeoutSeconds: event.target.value })}
            />
          )}
        </FormField>
      </div>

      <div className="flex items-start gap-2">
        <Checkbox
          id="webhook-active"
          checked={form.isActive}
          onCheckedChange={(checked) => setForm({ ...form, isActive: checked })}
        />
        <div className="space-y-0.5">
          <Label htmlFor="webhook-active">Active</Label>
          <p className="text-xs text-muted-foreground">An inactive webhook keeps its settings and history but receives nothing.</p>
        </div>
      </div>
      <Button type="submit" loading={pending} disabled={!canSubmit}>
        {submitLabel}
      </Button>
    </form>
  );
}

/** Grouped checklist of every event the server can emit, with an "All events" switch instead of typing `*`. */
function WebhookEventPicker({
  groups,
  loading,
  error,
  allEvents,
  selected,
  onChange,
}: {
  groups: WebhookEventGroup[] | undefined;
  loading: boolean;
  error: string | null;
  allEvents: boolean;
  selected: string[];
  onChange: (allEvents: boolean, events: string[]) => void;
}) {
  const known = new Set((groups ?? []).flatMap((group) => group.events.map((event) => event.eventName)));
  const unknown = selected.filter((name) => groups && !known.has(name));
  const toggle = (eventName: string, checked: boolean) =>
    onChange(false, checked ? [...selected.filter((name) => name !== eventName), eventName] : selected.filter((name) => name !== eventName));
  const toggleGroup = (group: WebhookEventGroup, checked: boolean) => {
    const names = group.events.map((event) => event.eventName);
    const rest = selected.filter((name) => !names.includes(name));
    onChange(false, checked ? [...rest, ...names] : rest);
  };

  return (
    <fieldset className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-0.5">
          <legend className="text-sm font-medium">Subscribed events</legend>
          <p className="text-xs text-muted-foreground">
            {allEvents
              ? "Every event, including ones added in future releases."
              : selected.length === 0
                ? "Pick at least one event."
                : `${selected.length} ${selected.length === 1 ? "event" : "events"} selected.`}
          </p>
        </div>
        <label className="flex items-center gap-2 text-sm">
          <Switch checked={allEvents} onCheckedChange={(checked) => onChange(checked, checked ? [] : selected)} />
          All events
        </label>
      </div>
      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : loading ? (
        <div className="space-y-2" aria-busy="true">
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
        </div>
      ) : (
        <div
          className={cn(
            "grid gap-3 md:grid-cols-2 xl:grid-cols-3",
            allEvents && "pointer-events-none opacity-50",
          )}
          aria-disabled={allEvents}
        >
          {(groups ?? []).map((group) => {
            const names = group.events.map((event) => event.eventName);
            const picked = names.filter((name) => selected.includes(name)).length;
            const groupId = `webhook-group-${group.group.replace(/\W+/g, "-").toLowerCase()}`;
            return (
              <div key={group.group} className="rounded-xl border border-border bg-card p-3">
                <div className="mb-2 flex items-center gap-2">
                  <Checkbox
                    id={groupId}
                    checked={picked === names.length}
                    disabled={allEvents}
                    onCheckedChange={(checked) => toggleGroup(group, checked)}
                  />
                  <Label htmlFor={groupId} className="font-medium">
                    {group.group}
                  </Label>
                  <span className="ml-auto text-xs tabular-nums text-muted-foreground">
                    {picked}/{names.length}
                  </span>
                </div>
                <ul className="space-y-1.5">
                  {group.events.map((event) => {
                    const id = `webhook-event-${event.eventName.replace(/\W+/g, "-")}`;
                    return (
                      <li key={event.eventName} className="flex items-start gap-2">
                        <Checkbox
                          id={id}
                          checked={allEvents || selected.includes(event.eventName)}
                          disabled={allEvents}
                          onCheckedChange={(checked) => toggle(event.eventName, checked)}
                        />
                        <Label htmlFor={id} className="flex flex-col gap-0 font-normal leading-tight">
                          <span>{event.displayName}</span>
                          <code className="font-mono text-[11px] text-muted-foreground">{event.eventName}</code>
                        </Label>
                      </li>
                    );
                  })}
                </ul>
              </div>
            );
          })}
        </div>
      )}
      {unknown.length > 0 ? (
        <div className="flex flex-wrap items-center gap-2 text-sm text-warning" role="alert">
          <p>
            Subscribed to {unknown.length === 1 ? "an event" : "events"} this server does not emit:{" "}
            <code className="font-mono">{unknown.join(", ")}</code>. The server rejects unknown event names.
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => onChange(false, selected.filter((name) => known.has(name)))}
          >
            Remove {unknown.length === 1 ? "it" : "them"}
          </Button>
        </div>
      ) : null}
    </fieldset>
  );
}

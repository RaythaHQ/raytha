import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef, PagedResult } from "@raytha/api";
import { Badge, EmptyState, PageHeader, QueryGate, Switch, toast } from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Inbox } from "lucide-react";
import { CrudListPage } from "./crud-list";
import { entityFields, formatWhen, readBoolean, readString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function AuditLogPage() {
  return (
    <CrudListPage
      title="Audit log"
      queryKey={["audit-logs"]}
      noun="entry"
      list={adminApi.auditLogs.list}
      columns={[
        { header: "When", cell: (entity) => formatWhen(entityFields(entity).creationTime) || "—" },
        { header: "Category", cell: (entity) => readString(entityFields(entity), "category") || "—" },
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
      noun="webhook"
      list={adminApi.webhooks.list}
      create={async (input) => {
        const created = await adminApi.webhooks.create(input);
        const secret = readString(entityFields(created), "secret");
        if (secret) {
          toast.success(`Webhook created. Secret: ${secret}`);
        }
        return created;
      }}
      remove={adminApi.webhooks.remove}
      createPermission={platformPermissions.systemSettings}
      createLabel="New webhook"
      createFields={[
        { key: "name", label: "Name", required: true },
        { key: "url", label: "URL", type: "url", required: true },
      ]}
      buildCreatePayload={(form) => ({
        name: form.name,
        url: form.url,
        subscribedEvents: ["*"],
        isActive: true,
      })}
      columns={[
        { header: "Name", cell: (entity) => readString(entityFields(entity), "name") || entity.id },
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

export function EmailLogPage() {
  return (
    <CrudListPage
      title="Email log"
      queryKey={["email-log"]}
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

export function FeatureFlagsPage() {
  useDocumentTitle(["Feature flags"]);
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["feature-flags"],
    queryFn: () => adminApi.featureFlags.list(),
  });

  const mutation = useMutation({
    mutationFn: ({ key, enabled }: { key: string; enabled: boolean }) => adminApi.featureFlags.set(key, enabled),
    onSuccess: () => {
      toast.success("Flag updated");
      void queryClient.invalidateQueries({ queryKey: ["feature-flags"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Feature flags" description="Toggle platform features." />
      <QueryGate query={query}>
        {(flags) =>
          flags.length === 0 ? (
            <EmptyState icon={Inbox} title="No feature flags" hint="The server did not return any flags." />
          ) : (
            <ul className="divide-y divide-border overflow-hidden rounded-xl border border-border bg-card shadow-card">
              {flags.map((flag) => {
                const fields = entityFields(flag);
                const key = readString(fields, "key", "id") || flag.id;
                const label = readString(fields, "label") || key;
                const description = readString(fields, "description");
                const enabled = readBoolean(fields, "isEnabled");
                return (
                  <li key={key} className="flex items-start justify-between gap-4 px-4 py-3">
                    <div>
                      <p className="font-medium">{label}</p>
                      {description ? <p className="text-sm text-muted-foreground">{description}</p> : null}
                      <p className="text-xs text-muted-foreground">{key}</p>
                    </div>
                    <Switch
                      checked={enabled}
                      aria-label={label}
                      disabled={mutation.isPending}
                      onCheckedChange={(checked) => mutation.mutate({ key, enabled: checked })}
                    />
                  </li>
                );
              })}
            </ul>
          )
        }
      </QueryGate>
    </div>
  );
}

export function BackgroundTasksPage() {
  useDocumentTitle(["Background tasks"]);

  const list = async (params?: Record<string, string | number | boolean | undefined>): Promise<PagedResult<EntityRef>> => {
    try {
      return await adminApi.backgroundTasks.list(params);
    } catch {
      return { items: [], totalCount: 0, pageNumber: 1, pageSize: 50 };
    }
  };

  return (
    <CrudListPage
      title="Background tasks"
      queryKey={["background-tasks"]}
      noun="task"
      list={list}
      emptyHint="No running tasks, or this API only supports lookup by id."
      columns={[
        { header: "Id", cell: (entity) => entity.id },
        { header: "Status", cell: (entity) => formatCellStatus(entity) },
        { header: "Updated", cell: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—" },
      ]}
    />
  );
}

function formatCellStatus(entity: EntityRef): string {
  const fields = entityFields(entity);
  return readString(fields, "status", "name") || "—";
}

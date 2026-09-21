import { adminApi, formatError, platformPermissions } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  FormField,
  Input,
  PageHeader,
  Select,
  toast,
} from "@raytha/ui";
import { useMutation } from "@tanstack/react-query";
import { Link, useNavigate } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { CrudListPage } from "./crud-list";
import { entityFields, formatCell, formatWhen, readBoolean, readString, toDeveloperName } from "./entity";

export function EmailTemplatesPage() {
  return (
    <CrudListPage
      title="Email templates"
      queryKey={["email-templates"]}
      listKey="email-templates"
      noun="email template"
      list={adminApi.emailTemplates.list}
      columns={[
        {
          header: "Subject",
          cell: (entity) => (
            <Link to="/email-templates/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "subject") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        { header: "Updated", cell: (entity) => formatWhen(entityFields(entity).lastModificationTime) || "—" },
      ]}
    />
  );
}

export function MenusPage() {
  return (
    <CrudListPage
      title="Menus"
      queryKey={["menus"]}
      listKey="menus"
      noun="menu"
      list={adminApi.menus.list}
      remove={adminApi.menus.remove}
      createPermission={platformPermissions.contentTypes}
      createLabel="New menu"
      createTo="/menus/new"
      columns={[
        {
          header: "Label",
          cell: (entity) => (
            <Link to="/menus/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "label") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        {
          header: "Main",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isMainMenu") ? <Badge variant="info">Main</Badge> : "—",
        },
      ]}
    />
  );
}

const FUNCTION_TRIGGERS = [
  { value: "http_request", label: "HTTP request" },
  { value: "liquid_template", label: "Liquid template" },
  { value: "content_item_created", label: "Content item created" },
  { value: "content_item_updated", label: "Content item updated" },
  { value: "content_item_deleted", label: "Content item deleted" },
] as const;

export function FunctionsPage() {
  return (
    <CrudListPage
      title="Functions"
      queryKey={["functions"]}
      listKey="functions"
      noun="function"
      list={adminApi.functions.list}
      remove={adminApi.functions.remove}
      createPermission={platformPermissions.systemSettings}
      createLabel="New function"
      createTo="/functions/new"
      columns={[
        {
          header: "Name",
          cell: (entity) => (
            <Link to="/functions/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "name") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        { header: "Trigger", cell: (entity) => formatCell(entityFields(entity).triggerType) || "—" },
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

export function NewMenuPage() {
  useDocumentTitle(["New menu"]);
  const navigate = useNavigate();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);

  const mutation = useMutation({
    mutationFn: () => adminApi.menus.create({ label, developerName }),
    onSuccess: (created) => {
      toast.success("Menu created");
      void navigate({ to: "/menus/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="New menu" />
      <ListBackLink to="/menus" listKey="menus" label="menus" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Label" required htmlFor="menu-label">
              {(control) => (
                <Input
                  {...control}
                  value={label}
                  onChange={(event) => {
                    const next = event.target.value;
                    setLabel(next);
                    if (!developerTouched) {
                      setDeveloperName(toDeveloperName(next));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField label="Developer name" required htmlFor="menu-developer">
              {(control) => (
                <Input
                  {...control}
                  value={developerName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setDeveloperName(event.target.value);
                  }}
                />
              )}
            </FormField>
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function NewFunctionPage() {
  useDocumentTitle(["New function"]);
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [triggerType, setTriggerType] = useState<string>(FUNCTION_TRIGGERS[0].value);
  const [code, setCode] = useState("");
  const [isActive, setIsActive] = useState(true);

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.functions.create({ name, developerName, triggerType, code, isActive }),
    onSuccess: (created) => {
      toast.success("Function created");
      void navigate({ to: "/functions/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="New function" />
      <ListBackLink to="/functions" listKey="functions" label="functions" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event: FormEvent) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Name" required htmlFor="fn-name">
              {(control) => (
                <Input
                  {...control}
                  value={name}
                  onChange={(event) => {
                    const next = event.target.value;
                    setName(next);
                    if (!developerTouched) {
                      setDeveloperName(toDeveloperName(next));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField label="Developer name" required htmlFor="fn-developer">
              {(control) => (
                <Input
                  {...control}
                  value={developerName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setDeveloperName(event.target.value);
                  }}
                />
              )}
            </FormField>
            <FormField label="Trigger" required htmlFor="fn-trigger">
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
            <FormField label="Code" required htmlFor="fn-code">
              {(control) => (
                <textarea
                  {...control}
                  className="min-h-40 w-full rounded-lg border border-input bg-background px-3 py-2 font-mono text-sm"
                  value={code}
                  onChange={(event) => setCode(event.target.value)}
                />
              )}
            </FormField>
            <div className="flex items-center gap-2">
              <Checkbox id="fn-active" checked={isActive} onCheckedChange={setIsActive} />
              <label htmlFor="fn-active" className="text-sm">
                Active
              </label>
            </div>
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

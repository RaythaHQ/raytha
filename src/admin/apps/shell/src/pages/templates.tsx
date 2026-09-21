import { adminApi, platformPermissions } from "@raytha/api";
import { Badge, FormField, Select } from "@raytha/ui";
import { Link } from "@tanstack/react-router";
import { CrudListPage, type FormValue } from "./crud-list";
import { entityFields, formatCell, formatWhen, readBoolean, readString } from "./entity";

export function EmailTemplatesPage() {
  return (
    <CrudListPage
      title="Email templates"
      queryKey={["email-templates"]}
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
      noun="menu"
      list={adminApi.menus.list}
      create={adminApi.menus.create}
      remove={adminApi.menus.remove}
      createPermission={platformPermissions.contentTypes}
      createLabel="New menu"
      createFields={[
        { key: "label", label: "Label", required: true, autoDeveloperNameFrom: "developerName" },
        { key: "developerName", label: "Developer name", required: true },
      ]}
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
      noun="function"
      list={adminApi.functions.list}
      create={adminApi.functions.create}
      remove={adminApi.functions.remove}
      createPermission={platformPermissions.systemSettings}
      createLabel="New function"
      createFields={[
        { key: "name", label: "Name", required: true, autoDeveloperNameFrom: "developerName" },
        { key: "developerName", label: "Developer name", required: true },
        { key: "code", label: "Code", type: "textarea", required: true },
        { key: "isActive", label: "Active", type: "checkbox" },
      ]}
      extraCreateFields={(form, setForm) => (
        <FormField label="Trigger" required htmlFor="function-trigger">
          {(control) => (
            <Select
              {...control}
              value={triggerValue(form)}
              onChange={(event) => setForm({ ...form, triggerType: event.target.value })}
            >
              {FUNCTION_TRIGGERS.map((trigger) => (
                <option key={trigger.value} value={trigger.value}>
                  {trigger.label}
                </option>
              ))}
            </Select>
          )}
        </FormField>
      )}
      buildCreatePayload={(form) => ({
        name: form.name,
        developerName: form.developerName,
        triggerType: triggerValue(form),
        code: form.code,
        isActive: form.isActive === true,
      })}
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

function triggerValue(form: Record<string, FormValue>): string {
  return typeof form.triggerType === "string" && form.triggerType.length > 0
    ? form.triggerType
    : FUNCTION_TRIGGERS[0].value;
}

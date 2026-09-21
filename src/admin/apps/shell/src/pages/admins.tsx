import { adminApi, platformPermissions } from "@raytha/api";
import type { EntityRef, JsonObject } from "@raytha/api";
import { Badge, Checkbox, Label } from "@raytha/ui";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { CrudListPage, type FormValue } from "./crud-list";
import { displayName, entityFields, formatWhen, readBoolean, readString } from "./entity";

export function AdminsPage() {
  const rolesQuery = useQuery({
    queryKey: ["roles", "picker"],
    queryFn: () => adminApi.roles.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });

  const roles = rolesQuery.data?.items ?? [];

  return (
    <CrudListPage
      title="Admins"
      description="Administrator accounts."
      queryKey={["admins"]}
      noun="admin"
      list={adminApi.admins.list}
      create={adminApi.admins.create}
      remove={adminApi.admins.remove}
      createPermission={platformPermissions.admins}
      createLabel="New admin"
      createFields={[
        { key: "firstName", label: "First name", required: true },
        { key: "lastName", label: "Last name", required: true },
        { key: "emailAddress", label: "Email", type: "email", required: true },
        { key: "sendEmail", label: "Send welcome email", type: "checkbox" },
      ]}
      extraCreateFields={(form, setForm) => (
        <RolePicker form={form} setForm={setForm} roles={roles} />
      )}
      buildCreatePayload={(form) => {
        const payload: JsonObject = {
          firstName: form.firstName,
          lastName: form.lastName,
          emailAddress: form.emailAddress,
          sendEmail: form.sendEmail === true,
          roles: selectedRoleIds(form),
        };
        return payload;
      }}
      columns={[
        { header: "Name", cell: (entity) => displayName(entity) },
        { header: "Email", cell: (entity) => readString(entityFields(entity), "emailAddress") },
        {
          header: "Status",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isActive") ? (
              <Badge variant="success">Active</Badge>
            ) : (
              <Badge variant="secondary">Inactive</Badge>
            ),
        },
        { header: "Last login", cell: (entity) => formatWhen(entityFields(entity).lastLoggedInTime) || "—" },
      ]}
    />
  );
}

export function RolesPage() {
  return (
    <CrudListPage
      title="Roles"
      queryKey={["roles"]}
      noun="role"
      list={adminApi.roles.list}
      create={adminApi.roles.create}
      remove={adminApi.roles.remove}
      createPermission={platformPermissions.admins}
      createLabel="New role"
      createFields={[
        { key: "label", label: "Label", required: true, autoDeveloperNameFrom: "developerName" },
        { key: "developerName", label: "Developer name", required: true },
      ]}
      buildCreatePayload={(form) => ({
        label: form.label,
        developerName: form.developerName,
        systemPermissions: [],
        contentTypePermissions: {},
      })}
      columns={[
        { header: "Label", cell: (entity) => readString(entityFields(entity), "label") || entity.id },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
      ]}
    />
  );
}

function RolePicker({
  form,
  setForm,
  roles,
}: {
  form: Record<string, FormValue>;
  setForm: (next: Record<string, FormValue>) => void;
  roles: EntityRef[];
}) {
  const selected = useMemo(() => new Set(selectedRoleIds(form)), [form]);

  const toggle = (id: string, checked: boolean) => {
    const next = new Set(selected);
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    setForm({ ...form, roles: [...next].join(",") });
  };

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">Roles</legend>
      {roles.length === 0 ? (
        <p className="text-sm text-muted-foreground">No roles available. Create a role first.</p>
      ) : (
        roles.map((role) => {
          const label = readString(entityFields(role), "label") || role.id;
          return (
            <div key={role.id} className="flex items-center gap-2">
              <Checkbox
                id={`role-${role.id}`}
                checked={selected.has(role.id)}
                onCheckedChange={(checked) => toggle(role.id, checked)}
              />
              <Label htmlFor={`role-${role.id}`}>{label}</Label>
            </div>
          );
        })
      )}
    </fieldset>
  );
}

function selectedRoleIds(form: Record<string, FormValue>): string[] {
  const raw = form.roles;
  if (typeof raw !== "string" || raw.length === 0) {
    return [];
  }
  return raw.split(",").filter(Boolean);
}

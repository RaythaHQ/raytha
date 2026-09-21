import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef, PermissionOption } from "@raytha/api";
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
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { useMemo, useState, type FormEvent } from "react";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { CrudListPage } from "./crud-list";
import { displayName, entityFields, formatWhen, isRecord, readBoolean, readString } from "./entity";

export function AdminsPage() {
  return (
    <CrudListPage
      title="Admins"
      description="Administrator accounts."
      queryKey={["admins"]}
      listKey="admins"
      noun="admin"
      list={adminApi.admins.list}
      remove={adminApi.admins.remove}
      createPermission={platformPermissions.admins}
      createLabel="New admin"
      createTo="/settings/admins/new"
      columns={[
        {
          header: "Name",
          cell: (entity) => (
            <Link to="/settings/admins/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {displayName(entity)}
            </Link>
          ),
        },
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
      listKey="roles"
      noun="role"
      list={adminApi.roles.list}
      remove={adminApi.roles.remove}
      createPermission={platformPermissions.admins}
      createLabel="New role"
      createTo="/settings/roles/new"
      columns={[
        {
          header: "Label",
          cell: (entity) => (
            <Link to="/settings/roles/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "label") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
      ]}
    />
  );
}

export function NewAdminPage() {
  useDocumentTitle(["New admin"]);
  const navigate = useNavigate();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [emailAddress, setEmailAddress] = useState("");
  const [sendEmail, setSendEmail] = useState(false);
  const [roleIds, setRoleIds] = useState<string[]>([]);

  const rolesQuery = useQuery({
    queryKey: ["roles", "picker"],
    queryFn: () => adminApi.roles.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.admins.create({ firstName, lastName, emailAddress, sendEmail, roles: roleIds }),
    onSuccess: (created) => {
      toast.success("Admin created");
      void navigate({ to: "/settings/admins/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="New admin" />
      <ListBackLink to="/settings/admins" listKey="admins" label="admins" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <NameEmailFields
              firstName={firstName}
              lastName={lastName}
              emailAddress={emailAddress}
              onFirstName={setFirstName}
              onLastName={setLastName}
              onEmail={setEmailAddress}
            />
            <div className="flex items-center gap-2">
              <Checkbox id="admin-send-email" checked={sendEmail} onCheckedChange={setSendEmail} />
              <Label htmlFor="admin-send-email">Send welcome email</Label>
            </div>
            <RolePicker roles={rolesQuery.data?.items ?? []} selected={roleIds} onChange={setRoleIds} />
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function EditAdminPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Edit admin"]);
  const query = useQuery({
    queryKey: ["admins", id],
    queryFn: () => adminApi.admins.get(id),
    enabled: id.length > 0,
  });

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit admin" />
        <p className="text-sm text-muted-foreground">Missing admin id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Edit admin" />
      <ListBackLink to="/settings/admins" listKey="admins" label="admins" />
      <QueryGate query={query}>{(admin) => <AdminEditForm admin={admin} />}</QueryGate>
    </div>
  );
}

function AdminEditForm({ admin }: { admin: EntityRef }) {
  const queryClient = useQueryClient();
  const fields = entityFields(admin);
  const [firstName, setFirstName] = useState(readString(fields, "firstName"));
  const [lastName, setLastName] = useState(readString(fields, "lastName"));
  const [emailAddress, setEmailAddress] = useState(readString(fields, "emailAddress"));
  const [roleIds, setRoleIds] = useState<string[]>(readIds(fields.roles));

  const rolesQuery = useQuery({
    queryKey: ["roles", "picker"],
    queryFn: () => adminApi.roles.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });

  const mutation = useMutation({
    mutationFn: () => adminApi.admins.update(admin.id, { firstName, lastName, emailAddress, roles: roleIds }),
    onSuccess: () => {
      toast.success("Admin updated");
      void queryClient.invalidateQueries({ queryKey: ["admins"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <Card>
      <CardContent className="pt-6">
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            mutation.mutate();
          }}
        >
          <NameEmailFields
            firstName={firstName}
            lastName={lastName}
            emailAddress={emailAddress}
            onFirstName={setFirstName}
            onLastName={setLastName}
            onEmail={setEmailAddress}
          />
          <RolePicker roles={rolesQuery.data?.items ?? []} selected={roleIds} onChange={setRoleIds} />
          <Button type="submit" loading={mutation.isPending}>
            Save
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

export function NewRolePage() {
  useDocumentTitle(["New role"]);
  const navigate = useNavigate();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [systemPermissions, setSystemPermissions] = useState<string[]>([]);
  const [contentTypePermissions, setContentTypePermissions] = useState<Record<string, string[]>>({});

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.roles.create({ label, developerName, systemPermissions, contentTypePermissions }),
    onSuccess: (created) => {
      toast.success("Role created");
      void navigate({ to: "/settings/roles/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="New role" />
      <ListBackLink to="/settings/roles" listKey="roles" label="roles" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Label" required htmlFor="role-label">
              {(control) => (
                <Input
                  {...control}
                  value={label}
                  onChange={(event) => {
                    const next = event.target.value;
                    setLabel(next);
                    if (!developerTouched) {
                      setDeveloperName(toAutoName(next));
                    }
                  }}
                />
              )}
            </FormField>
            <FormField label="Developer name" required htmlFor="role-developer">
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
            <RolePermissionFields
              systemPermissions={systemPermissions}
              contentTypePermissions={contentTypePermissions}
              onSystem={setSystemPermissions}
              onContent={setContentTypePermissions}
            />
            <Button type="submit" loading={mutation.isPending}>
              Create
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function EditRolePage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Edit role"]);
  const query = useQuery({
    queryKey: ["roles", id],
    queryFn: () => adminApi.roles.get(id),
    enabled: id.length > 0,
  });

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit role" />
        <p className="text-sm text-muted-foreground">Missing role id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Edit role" />
      <ListBackLink to="/settings/roles" listKey="roles" label="roles" />
      <QueryGate query={query}>{(role) => <RoleEditForm role={role} />}</QueryGate>
    </div>
  );
}

function RoleEditForm({ role }: { role: EntityRef }) {
  const queryClient = useQueryClient();
  const fields = entityFields(role);
  const [label, setLabel] = useState(readString(fields, "label"));
  const [systemPermissions, setSystemPermissions] = useState<string[]>(readStringList(fields.systemPermissions));
  const [contentTypePermissions, setContentTypePermissions] = useState<Record<string, string[]>>(
    readPermissionMap(fields.contentTypePermissions),
  );

  const mutation = useMutation({
    mutationFn: () => adminApi.roles.update(role.id, { label, systemPermissions, contentTypePermissions }),
    onSuccess: () => {
      toast.success("Role updated");
      void queryClient.invalidateQueries({ queryKey: ["roles"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
    <Card>
      <CardContent className="pt-6">
        <form className="space-y-4" onSubmit={handleSubmit}>
          <FormField label="Label" required htmlFor="edit-role-label">
            {(control) => <Input {...control} value={label} onChange={(event) => setLabel(event.target.value)} />}
          </FormField>
          <p className="text-sm text-muted-foreground">
            Developer name {readString(fields, "developerName") || "—"}
          </p>
          <RolePermissionFields
            systemPermissions={systemPermissions}
            contentTypePermissions={contentTypePermissions}
            onSystem={setSystemPermissions}
            onContent={setContentTypePermissions}
          />
          <Button type="submit" loading={mutation.isPending}>
            Save
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function RolePermissionFields({
  systemPermissions,
  contentTypePermissions,
  onSystem,
  onContent,
}: {
  systemPermissions: string[];
  contentTypePermissions: Record<string, string[]>;
  onSystem: (next: string[]) => void;
  onContent: (next: Record<string, string[]>) => void;
}) {
  const catalogQuery = useQuery({
    queryKey: ["role-permissions"],
    queryFn: () => adminApi.roles.permissions(),
  });
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
  });
  const system = catalogQuery.data?.systemPermissions ?? [];
  const contentPerms = catalogQuery.data?.contentTypePermissions ?? [];
  const types = typesQuery.data?.items ?? [];

  return (
    <div className="space-y-4">
      <PermissionSet
        legend="System permissions"
        options={system}
        selected={systemPermissions}
        onChange={onSystem}
      />
      {types.map((type) => {
        const typeLabel = readString(entityFields(type), "labelPlural", "labelSingular", "developerName") || type.id;
        return (
          <PermissionSet
            key={type.id}
            legend={typeLabel}
            options={contentPerms}
            selected={contentTypePermissions[type.id] ?? []}
            onChange={(next) => {
              const updated = { ...contentTypePermissions };
              if (next.length === 0) {
                delete updated[type.id];
              } else {
                updated[type.id] = next;
              }
              onContent(updated);
            }}
          />
        );
      })}
    </div>
  );
}

function PermissionSet({
  legend,
  options,
  selected,
  onChange,
}: {
  legend: string;
  options: PermissionOption[];
  selected: string[];
  onChange: (next: string[]) => void;
}) {
  const selectedSet = useMemo(() => new Set(selected), [selected]);
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">{legend}</legend>
      {options.length === 0 ? (
        <p className="text-sm text-muted-foreground">No permissions listed.</p>
      ) : (
        options.map((option) => (
          <div key={option.developerName} className="flex items-center gap-2">
            <Checkbox
              id={`${legend}-${option.developerName}`}
              checked={selectedSet.has(option.developerName)}
              onCheckedChange={(checked) => {
                const next = new Set(selectedSet);
                if (checked) {
                  next.add(option.developerName);
                } else {
                  next.delete(option.developerName);
                }
                onChange([...next]);
              }}
            />
            <Label htmlFor={`${legend}-${option.developerName}`}>{option.label}</Label>
          </div>
        ))
      )}
    </fieldset>
  );
}

function RolePicker({
  roles,
  selected,
  onChange,
}: {
  roles: EntityRef[];
  selected: string[];
  onChange: (ids: string[]) => void;
}) {
  const selectedSet = useMemo(() => new Set(selected), [selected]);
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
                checked={selectedSet.has(role.id)}
                onCheckedChange={(checked) => {
                  const next = new Set(selectedSet);
                  if (checked) {
                    next.add(role.id);
                  } else {
                    next.delete(role.id);
                  }
                  onChange([...next]);
                }}
              />
              <Label htmlFor={`role-${role.id}`}>{label}</Label>
            </div>
          );
        })
      )}
    </fieldset>
  );
}

function NameEmailFields({
  firstName,
  lastName,
  emailAddress,
  onFirstName,
  onLastName,
  onEmail,
}: {
  firstName: string;
  lastName: string;
  emailAddress: string;
  onFirstName: (value: string) => void;
  onLastName: (value: string) => void;
  onEmail: (value: string) => void;
}) {
  return (
    <>
      <FormField label="First name" required htmlFor="admin-first">
        {(control) => <Input {...control} value={firstName} onChange={(event) => onFirstName(event.target.value)} />}
      </FormField>
      <FormField label="Last name" required htmlFor="admin-last">
        {(control) => <Input {...control} value={lastName} onChange={(event) => onLastName(event.target.value)} />}
      </FormField>
      <FormField label="Email" required htmlFor="admin-email">
        {(control) => (
          <Input {...control} type="email" value={emailAddress} onChange={(event) => onEmail(event.target.value)} />
        )}
      </FormField>
    </>
  );
}

function readIds(value: unknown): string[] {
  if (!Array.isArray(value)) {
    return [];
  }
  const ids: string[] = [];
  for (const item of value) {
    if (typeof item === "string") {
      ids.push(item);
    } else if (isRecord(item) && typeof item.id === "string") {
      ids.push(item.id);
    }
  }
  return ids;
}

function readStringList(value: unknown): string[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value.filter((item): item is string => typeof item === "string");
}

function readPermissionMap(value: unknown): Record<string, string[]> {
  if (!isRecord(value)) {
    return {};
  }
  const map: Record<string, string[]> = {};
  for (const [key, perms] of Object.entries(value)) {
    if (Array.isArray(perms)) {
      map[key] = perms.filter((item): item is string => typeof item === "string");
    }
  }
  return map;
}

function toAutoName(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

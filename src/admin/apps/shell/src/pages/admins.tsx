import { adminApi, currentSession, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef, PermissionOption } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  cn,
  ConfirmDialog,
  DangerZone,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  toast,
} from "@raytha/ui";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { Lock, ShieldCheck } from "lucide-react";
import { useId, useMemo, useState, type FormEvent, type ReactNode } from "react";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import {
  accountManageBlock,
  callerCeiling,
  canGrantContent,
  canGrantSystem,
  CONTENT_ACCESS,
  CONTENT_TYPES_PERMISSION,
  impliedBy,
  isBuiltInRole,
  isSuperAdminRole,
  PAIRED_SYSTEM_PERMISSIONS,
  readRoleSummaries,
  readRoleSummary,
  roleAssignmentBlock,
  roleEditBlock,
  SUPER_ADMIN_ROLE,
  SYSTEM_PERMISSION_DESCRIPTIONS,
  withImpliedRead,
  type Ceiling,
  type ContentAccess,
  type RoleSummary,
} from "../lib/role-permissions";
import { CrudListPage } from "./crud-list";
import { displayName, entityFields, formatWhen, isRecord, readBoolean, readString, toDeveloperName } from "./entity";

export function AdminsPage() {
  return (
    <CrudListPage
      title="Admins"
      description="People who can sign in to this admin. What each one can do comes from their roles."
      queryKey={["admins"]}
      listKey="admins"
      noun="admin"
      list={adminApi.admins.list}
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
          header: "Roles",
          cell: (entity) => <RoleBadges roles={readRoleSummaries(entityFields(entity).roles)} />,
        },
        {
          header: "Status",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isActive") ? (
              <Badge variant="success">Active</Badge>
            ) : (
              <Badge variant="secondary">Suspended</Badge>
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
      description="A role is a named set of permissions. Admins get their access from the roles they hold."
      queryKey={["roles"]}
      listKey="roles"
      noun="role"
      list={adminApi.roles.list}
      createPermission={platformPermissions.admins}
      createLabel="New role"
      createTo="/settings/roles/new"
      columns={[
        {
          header: "Role",
          cell: (entity) => {
            const role = readRoleSummary(entity);
            return (
              <span className="inline-flex flex-wrap items-center gap-2">
                <Link to="/settings/roles/$id" params={{ id: entity.id }} className="font-medium text-primary hover:underline">
                  {role.label || entity.id}
                </Link>
                {isSuperAdminRole(role) && (
                  <Badge variant="warning">Locked</Badge>
                )}
                {isBuiltInRole(role.developerName) && <Badge variant="outline">Built-in</Badge>}
              </span>
            );
          },
        },
        {
          header: "Developer name",
          cell: (entity) => <code className="font-mono text-xs">{readString(entityFields(entity), "developerName")}</code>,
        },
        { header: "Access", cell: (entity) => <span className="text-muted-foreground">{describeAccess(readRoleSummary(entity))}</span> },
      ]}
    />
  );
}

export function NewAdminPage() {
  useDocumentTitle(["New admin"]);
  const navigate = useNavigate();
  const ceiling = useMemo(() => callerCeiling(currentSession()), []);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [emailAddress, setEmailAddress] = useState("");
  const [sendEmail, setSendEmail] = useState(true);
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const roles = useRoleSummaries();

  const mutation = useMutation({
    mutationFn: () => adminApi.admins.create({ firstName, lastName, emailAddress, sendEmail, roles: roleIds }),
    onSuccess: (created) => {
      toast.success("Admin created");
      void navigate({ to: "/settings/admins/$id", params: { id: created.id } });
    },
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/settings/admins" listKey="admins" label="admins" />}
        title="New admin"
        description="They get an admin login. Roles decide what they can see and change."
      />
      <form
        className="space-y-6"
        onSubmit={(event) => {
          event.preventDefault();
          mutation.mutate();
        }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
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
              <Label htmlFor="admin-send-email">Email them a welcome message with a temporary password</Label>
            </div>
          </CardContent>
        </Card>
        <RolePicker roles={roles} selected={roleIds} onChange={setRoleIds} ceiling={ceiling} />
        <FormActions error={mutation.error}>
          <Button type="submit" loading={mutation.isPending}>
            Create admin
          </Button>
        </FormActions>
      </form>
    </div>
  );
}

export function EditAdminPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["admins", id],
    queryFn: () => adminApi.admins.get(id),
    enabled: id.length > 0,
  });
  useDocumentTitle([query.data ? displayName(query.data) : "Admin", "Admins"]);

  return (
    <QueryGate query={query}>{(admin) => <AdminEditForm key={admin.id} admin={admin} />}</QueryGate>
  );
}

function AdminEditForm({ admin }: { admin: EntityRef }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const fields = entityFields(admin);
  const session = currentSession();
  const ceiling = useMemo(() => callerCeiling(session), [session]);
  const isActive = readBoolean(fields, "isActive");
  const isSelf = session?.id === admin.id;
  const targetRoles = useMemo(() => readRoleSummaries(fields.roles), [fields.roles]);
  const initialRoleIds = useMemo(() => targetRoles.map((role) => role.id), [targetRoles]);
  const locked = isSelf ? null : accountManageBlock(ceiling, targetRoles);
  const [firstName, setFirstName] = useState(readString(fields, "firstName"));
  const [lastName, setLastName] = useState(readString(fields, "lastName"));
  const [emailAddress, setEmailAddress] = useState(readString(fields, "emailAddress"));
  const [roleIds, setRoleIds] = useState<string[]>(initialRoleIds);
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [sendEmail, setSendEmail] = useState(true);
  const [removeAccessOpen, setRemoveAccessOpen] = useState(false);
  const roles = useRoleSummaries();

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["admins"] });
  };

  const save = useMutation({
    mutationFn: () => adminApi.admins.update(admin.id, { firstName, lastName, emailAddress, roles: roleIds }),
    onSuccess: () => {
      toast.success("Admin updated");
      invalidate();
    },
  });

  const setActive = useMutation({
    mutationFn: () => (isActive ? adminApi.admins.suspend(admin.id) : adminApi.admins.restore(admin.id)),
    onSuccess: () => {
      toast.success(isActive ? "Admin suspended" : "Admin restored");
      invalidate();
    },
  });

  const removeAccess = useMutation({
    mutationFn: () => adminApi.admins.removeAccess(admin.id),
    onSuccess: () => {
      toast.success("Admin access removed");
      setRemoveAccessOpen(false);
      invalidate();
      void navigate({ to: "/settings/admins" });
    },
    onError: () => setRemoveAccessOpen(false),
  });

  const resetPassword = useMutation({
    mutationFn: () => adminApi.admins.resetPassword(admin.id, { newPassword, confirmNewPassword, sendEmail }),
    onSuccess: () => {
      toast.success(sendEmail ? "Password reset and emailed" : "Password reset");
      setNewPassword("");
      setConfirmNewPassword("");
    },
  });

  const remove = useMutation({
    mutationFn: () => adminApi.admins.remove(admin.id),
    onSuccess: () => {
      toast.success("Admin deleted");
      invalidate();
      void navigate({ to: "/settings/admins" });
    },
  });

  const accountError = setActive.error ?? removeAccess.error;
  const name = displayName(admin);

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/settings/admins" listKey="admins" label="admins" />}
        title={name}
        meta={
          <>
            {isActive ? <Badge variant="success">Active</Badge> : <Badge variant="secondary">Suspended</Badge>}
            {isSelf && <Badge variant="info">You</Badge>}
            <RoleBadges roles={targetRoles} />
            <span>Last login {formatWhen(fields.lastLoggedInTime) || "never"}</span>
          </>
        }
      />
      {locked && (
        <LockedNotice title="You can view this admin but not change them">{locked}</LockedNotice>
      )}
      <form
        className="space-y-6"
        onSubmit={(event) => {
          event.preventDefault();
          save.mutate();
        }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <NameEmailFields
              firstName={firstName}
              lastName={lastName}
              emailAddress={emailAddress}
              onFirstName={setFirstName}
              onLastName={setLastName}
              onEmail={setEmailAddress}
              disabled={locked !== null}
            />
          </CardContent>
        </Card>
        <RolePicker
          roles={roles}
          selected={roleIds}
          onChange={setRoleIds}
          ceiling={ceiling}
          readOnly={locked !== null || isSelf}
          note={isSelf ? "You cannot change your own roles. Another administrator has to change them for you." : undefined}
        />
        {!locked && (
          <FormActions error={save.error}>
            <Button type="submit" loading={save.isPending}>
              Save changes
            </Button>
          </FormActions>
        )}
      </form>
      {!locked && (
        <>
          <Card>
            <CardHeader>
              <CardTitle>Account access</CardTitle>
              <CardDescription>
                {isSelf
                  ? "You cannot suspend yourself or remove your own admin access."
                  : "Suspending blocks sign-in and keeps the account. Removing access turns them into a website user without admin roles."}
              </CardDescription>
            </CardHeader>
            {!isSelf && (
              <CardContent className="space-y-3">
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" loading={setActive.isPending} onClick={() => setActive.mutate()}>
                    {isActive ? "Suspend" : "Restore"}
                  </Button>
                  <Button type="button" variant="outline" onClick={() => setRemoveAccessOpen(true)}>
                    Remove admin access
                  </Button>
                </div>
                <InlineError error={accountError} />
              </CardContent>
            )}
          </Card>
          <ConfirmDialog
            open={removeAccessOpen}
            onOpenChange={setRemoveAccessOpen}
            title={`Remove admin access for ${name}?`}
            body="They lose every admin role and become a website user. Their content and history stay."
            confirmLabel="Remove access"
            onConfirm={() => removeAccess.mutate()}
            pending={removeAccess.isPending}
          />
          <Card>
            <CardHeader>
              <CardTitle>Reset password</CardTitle>
              {!isActive && <CardDescription>Restore this account before resetting the password.</CardDescription>}
            </CardHeader>
            {isActive && (
              <CardContent>
                <form
                  className="space-y-4"
                  onSubmit={(event) => {
                    event.preventDefault();
                    if (newPassword.length < 8) {
                      setPasswordError("Password must be at least 8 characters.");
                      return;
                    }
                    if (newPassword !== confirmNewPassword) {
                      setPasswordError("The passwords do not match.");
                      return;
                    }
                    setPasswordError(null);
                    resetPassword.mutate();
                  }}
                >
                  <FormField label="New password" required htmlFor="admin-new-password" hint="At least 8 characters.">
                    {(control) => (
                      <Input
                        {...control}
                        type="password"
                        autoComplete="new-password"
                        value={newPassword}
                        onChange={(event) => setNewPassword(event.target.value)}
                      />
                    )}
                  </FormField>
                  <FormField label="Confirm password" required htmlFor="admin-confirm-password">
                    {(control) => (
                      <Input
                        {...control}
                        type="password"
                        autoComplete="new-password"
                        value={confirmNewPassword}
                        onChange={(event) => setConfirmNewPassword(event.target.value)}
                      />
                    )}
                  </FormField>
                  <div className="flex items-center gap-2">
                    <Checkbox id="admin-reset-send-email" checked={sendEmail} onCheckedChange={setSendEmail} />
                    <Label htmlFor="admin-reset-send-email">Email the new password</Label>
                  </div>
                  <FormActions error={passwordError ?? resetPassword.error}>
                    <Button type="submit" variant="outline" loading={resetPassword.isPending}>
                      Reset password
                    </Button>
                  </FormActions>
                </form>
              </CardContent>
            )}
          </Card>
          {!isSelf && (
            <div className="space-y-2">
              <DangerZone
                description="Deletes the account and its API keys. Audit history keeps their name. This cannot be undone."
                actionLabel="Delete admin"
                confirmTitle={`Delete ${name}?`}
                confirmBody="The account and its API keys are deleted. This cannot be undone."
                onConfirm={() => remove.mutate()}
                pending={remove.isPending}
              />
              <InlineError error={remove.error} />
            </div>
          )}
        </>
      )}
    </div>
  );
}

export function NewRolePage() {
  useDocumentTitle(["New role", "Roles"]);
  const navigate = useNavigate();
  const ceiling = useMemo(() => callerCeiling(currentSession()), []);
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);
  const [systemPermissions, setSystemPermissions] = useState<string[]>([]);
  const [contentTypePermissions, setContentTypePermissions] = useState<Record<string, string[]>>({});

  const mutation = useMutation({
    mutationFn: () => adminApi.roles.create({ label, developerName, systemPermissions, contentTypePermissions }),
    onSuccess: (created) => {
      toast.success("Role created");
      void navigate({ to: "/settings/roles/$id", params: { id: created.id } });
    },
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/settings/roles" listKey="roles" label="roles" />}
        title="New role"
        description={
          ceiling.unlimited
            ? "Pick what admins with this role can do."
            : "Pick what admins with this role can do. You can only grant permissions you hold yourself."
        }
      />
      <form
        className="space-y-6"
        onSubmit={(event) => {
          event.preventDefault();
          mutation.mutate();
        }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Details</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <FormField label="Label" required htmlFor="role-label">
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
            <FormField label="Developer name" required htmlFor="role-developer" hint="Used in code and templates. It cannot change later.">
              {(control) => (
                <Input
                  {...control}
                  className="font-mono"
                  value={developerName}
                  onChange={(event) => {
                    setDeveloperTouched(true);
                    setDeveloperName(event.target.value);
                  }}
                />
              )}
            </FormField>
          </CardContent>
        </Card>
        <RolePermissionMatrix
          ceiling={ceiling}
          systemPermissions={systemPermissions}
          contentTypePermissions={contentTypePermissions}
          onSystem={setSystemPermissions}
          onContent={setContentTypePermissions}
        />
        <FormActions error={mutation.error}>
          <Button type="submit" loading={mutation.isPending}>
            Create role
          </Button>
        </FormActions>
      </form>
    </div>
  );
}

export function EditRolePage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["roles", id],
    queryFn: () => adminApi.roles.get(id),
    enabled: id.length > 0,
  });
  useDocumentTitle([query.data ? readString(entityFields(query.data), "label") || "Role" : "Role", "Roles"]);

  return <QueryGate query={query}>{(role) => <RoleEditForm key={role.id} role={role} />}</QueryGate>;
}

function RoleEditForm({ role }: { role: EntityRef }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const fields = entityFields(role);
  const summary = useMemo(() => readRoleSummary(role), [role]);
  const ceiling = useMemo(() => callerCeiling(currentSession()), []);
  const superAdmin = isSuperAdminRole(summary);
  const builtIn = isBuiltInRole(summary.developerName);
  const locked = roleEditBlock(ceiling, summary);
  const [label, setLabel] = useState(summary.label);
  const [systemPermissions, setSystemPermissions] = useState<string[]>(summary.systemPermissions);
  const [contentTypePermissions, setContentTypePermissions] = useState<Record<string, string[]>>(() =>
    normalizePermissionMap(readPermissionMap(fields.contentTypePermissions)),
  );

  const save = useMutation({
    mutationFn: () => adminApi.roles.update(role.id, { label, systemPermissions, contentTypePermissions }),
    onSuccess: () => {
      toast.success("Role updated");
      void queryClient.invalidateQueries({ queryKey: ["roles"] });
    },
  });

  const remove = useMutation({
    mutationFn: () => adminApi.roles.remove(role.id),
    onSuccess: () => {
      toast.success("Role deleted");
      void queryClient.invalidateQueries({ queryKey: ["roles"] });
      void navigate({ to: "/settings/roles" });
    },
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/settings/roles" listKey="roles" label="roles" />}
        title={summary.label}
        meta={
          <>
            <code>{summary.developerName}</code>
            {superAdmin && (
              <Badge variant="warning">Locked</Badge>
            )}
            {builtIn && <Badge variant="outline">Built-in</Badge>}
            <span>{describeAccess(summary)}</span>
          </>
        }
      />
      {locked && (
        <LockedNotice title={superAdmin ? "Super Admin is locked" : "You can view this role but not change it"}>
          {superAdmin
            ? `${locked} This guarantees there is always an account that can recover full control of the site.`
            : locked}
        </LockedNotice>
      )}
      <form
        className="space-y-6"
        onSubmit={(event: FormEvent) => {
          event.preventDefault();
          save.mutate();
        }}
      >
        <Card>
          <CardHeader>
            <CardTitle>Details</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <FormField label="Label" required htmlFor="edit-role-label">
              {(control) => (
                <Input {...control} value={label} disabled={locked !== null} onChange={(event) => setLabel(event.target.value)} />
              )}
            </FormField>
            <FormField label="Developer name" htmlFor="edit-role-developer" hint="Set when the role was created.">
              {(control) => <Input {...control} className="font-mono" value={summary.developerName} disabled readOnly />}
            </FormField>
          </CardContent>
        </Card>
        <RolePermissionMatrix
          ceiling={ceiling}
          systemPermissions={systemPermissions}
          contentTypePermissions={contentTypePermissions}
          onSystem={setSystemPermissions}
          onContent={setContentTypePermissions}
          readOnly={locked !== null}
          fullAccess={superAdmin}
        />
        {!locked && (
          <FormActions error={save.error}>
            <Button type="submit" loading={save.isPending}>
              Save changes
            </Button>
          </FormActions>
        )}
      </form>
      {!locked && (
        <div className="space-y-2">
          <DangerZone
            description={
              builtIn
                ? "This is a built-in role. Once deleted, Raytha does not recreate it. Remove it from every admin first."
                : "Remove this role from every admin first. This cannot be undone."
            }
            actionLabel="Delete role"
            confirmTitle={`Delete the ${summary.label} role?`}
            confirmBody="This cannot be undone."
            onConfirm={() => remove.mutate()}
            pending={remove.isPending}
          />
          <InlineError error={remove.error} />
        </div>
      )}
    </div>
  );
}

function RolePermissionMatrix({
  ceiling,
  systemPermissions,
  contentTypePermissions,
  onSystem,
  onContent,
  readOnly = false,
  fullAccess = false,
}: {
  ceiling: Ceiling;
  systemPermissions: string[];
  contentTypePermissions: Record<string, string[]>;
  onSystem: (next: string[]) => void;
  onContent: (next: Record<string, string[]>) => void;
  readOnly?: boolean;
  fullAccess?: boolean;
}) {
  const catalogQuery = useQuery({ queryKey: ["role-permissions"], queryFn: () => adminApi.roles.permissions() });
  const typesQuery = useQuery({
    queryKey: ["content-types", "picker"],
    queryFn: () => adminApi.contentTypes.list({ pageSize: 200 }),
  });
  const catalog = useMemo(() => catalogQuery.data?.systemPermissions ?? [], [catalogQuery.data]);
  const labels = useMemo(() => new Map(catalog.map((option) => [option.developerName, option.label])), [catalog]);
  const selected = useMemo(() => new Set(systemPermissions), [systemPermissions]);
  const allContentTypes = fullAccess || selected.has(CONTENT_TYPES_PERMISSION);

  const toggleSystem = (permission: string, checked: boolean) => {
    const next = new Set(selected);
    const partner = PAIRED_SYSTEM_PERMISSIONS[permission];
    for (const value of partner ? [permission, partner] : [permission]) {
      if (checked) {
        next.add(value);
      } else {
        next.delete(value);
      }
    }
    onSystem(catalog.map((option) => option.developerName).filter((value) => next.has(value)));
  };

  const toggleContent = (typeId: string, access: ContentAccess, checked: boolean) => {
    const current = new Set(contentTypePermissions[typeId] ?? []);
    if (checked) {
      current.add(access);
    } else {
      current.delete(access);
    }
    const nextAccess = withImpliedRead([...current]);
    const updated = { ...contentTypePermissions };
    if (nextAccess.length === 0) {
      delete updated[typeId];
    } else {
      updated[typeId] = nextAccess;
    }
    onContent(updated);
  };

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>System permissions</CardTitle>
          <CardDescription>Areas of the admin this role can manage.</CardDescription>
        </CardHeader>
        <CardContent>
          {catalogQuery.isPending ? (
            <p className="text-sm text-muted-foreground">Loading permissions…</p>
          ) : (
            <ul className="grid gap-2 md:grid-cols-2">
              {catalog.map((option) => (
                <li key={option.developerName}>
                  <SystemPermissionTile
                    option={option}
                    checked={fullAccess || selected.has(option.developerName)}
                    reason={systemLockReason(option.developerName, { ceiling, readOnly, fullAccess, labels })}
                    pairedWith={labelFor(labels, PAIRED_SYSTEM_PERMISSIONS[option.developerName])}
                    disabled={readOnly || fullAccess || !canToggleSystem(ceiling, option.developerName)}
                    onChange={(checked) => toggleSystem(option.developerName, checked)}
                  />
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Content types</CardTitle>
          <CardDescription>
            {allContentTypes
              ? `${fullAccess ? "Super Admin" : "Manage Content Types"} grants read, edit, and configure on every content type, including ones created later.`
              : "Access per content type. Edit and Configure include Read."}
          </CardDescription>
        </CardHeader>
        <CardContent className="px-0 pb-2">
          {typesQuery.isPending ? (
            <p className="px-6 pb-4 text-sm text-muted-foreground">Loading content types…</p>
          ) : (typesQuery.data?.items.length ?? 0) === 0 ? (
            <p className="px-6 pb-4 text-sm text-muted-foreground">No content types yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <caption className="sr-only">Content type permissions</caption>
                <thead>
                  <tr className="border-y border-border bg-muted/40 text-left">
                    <th scope="col" className="px-6 py-2 font-medium">
                      Content type
                    </th>
                    {CONTENT_ACCESS.map((access) => (
                      <th key={access.value} scope="col" className="w-40 px-3 py-2 text-center font-medium">
                        <span className="block">{access.label}</span>
                        <span className="block text-xs font-normal text-muted-foreground">{access.description}</span>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {(typesQuery.data?.items ?? []).map((type) => {
                    const typeFields = entityFields(type);
                    const typeName = readString(typeFields, "labelPlural", "labelSingular", "developerName") || type.id;
                    const developerName = readString(typeFields, "developerName");
                    const access = contentTypePermissions[type.id] ?? [];
                    const limited =
                      !readOnly &&
                      !allContentTypes &&
                      CONTENT_ACCESS.some((option) => !canGrantContent(ceiling, developerName, option.value));
                    return (
                      <tr key={type.id} className="border-b border-border last:border-b-0">
                        <th scope="row" className="px-6 py-2.5 text-left font-medium">
                          {typeName}
                          <code className="ml-2 font-mono text-xs font-normal text-muted-foreground">{developerName}</code>
                          {limited && (
                            <span className="mt-0.5 flex items-center gap-1 text-xs font-normal text-muted-foreground">
                              <Lock aria-hidden className="size-3 shrink-0" />
                              You can only grant the access you hold on {typeName}.
                            </span>
                          )}
                        </th>
                        {CONTENT_ACCESS.map((option) => {
                          const implied = impliedBy(access, option.value);
                          const granted = allContentTypes || access.includes(option.value);
                          const grantable = canGrantContent(ceiling, developerName, option.value);
                          const impliedReason =
                            !allContentTypes && implied ? `Included with ${implied === "edit" ? "Edit" : "Configure"}` : null;
                          const deniedReason =
                            !allContentTypes && !implied && !grantable && !readOnly
                              ? `You do not have ${option.label} on ${typeName}`
                              : null;
                          return (
                            <td key={option.value} className="px-3 py-2.5 text-center">
                              <MatrixCell
                                id={`ct-${type.id}-${option.value}`}
                                label={`${option.label} ${typeName}`}
                                checked={granted}
                                disabled={readOnly || allContentTypes || implied !== null || !grantable}
                                reason={impliedReason ?? deniedReason}
                                reasonVisible={impliedReason !== null}
                                onChange={(checked) => toggleContent(type.id, option.value, checked)}
                              />
                            </td>
                          );
                        })}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </>
  );
}

function SystemPermissionTile({
  option,
  checked,
  disabled,
  reason,
  pairedWith,
  onChange,
}: {
  option: PermissionOption;
  checked: boolean;
  disabled: boolean;
  reason: string | null;
  pairedWith: string | null;
  onChange: (checked: boolean) => void;
}) {
  const id = `perm-${option.developerName}`;
  const noteId = useId();
  const description = SYSTEM_PERMISSION_DESCRIPTIONS[option.developerName];
  return (
    <div
      className={cn(
        "flex h-full gap-3 rounded-lg border px-3.5 py-3 transition-colors",
        checked ? "border-primary/40 bg-primary/5" : "border-border",
        disabled && !checked && "opacity-70",
      )}
    >
      <Checkbox
        id={id}
        className="mt-0.5"
        checked={checked}
        disabled={disabled}
        aria-describedby={noteId}
        onCheckedChange={onChange}
      />
      <div className="min-w-0 space-y-0.5">
        <Label htmlFor={id} className={cn("font-medium", !disabled && "cursor-pointer")}>
          {option.label}
        </Label>
        <div id={noteId} className="space-y-0.5 text-[13px] leading-5 text-muted-foreground">
          {description && <p>{description}</p>}
          {pairedWith && <p>Always granted together with {pairedWith}.</p>}
          {reason && (
            <p className="flex items-center gap-1 text-xs font-medium text-foreground/70">
              <Lock aria-hidden className="size-3 shrink-0" />
              {reason}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

function MatrixCell({
  id,
  label,
  checked,
  disabled,
  reason,
  reasonVisible,
  onChange,
}: {
  id: string;
  label: string;
  checked: boolean;
  disabled: boolean;
  reason: string | null;
  reasonVisible: boolean;
  onChange: (checked: boolean) => void;
}) {
  const reasonId = `${id}-reason`;
  return (
    <span className="inline-flex flex-col items-center gap-0.5" title={reason ?? undefined}>
      <Checkbox
        id={id}
        checked={checked}
        disabled={disabled}
        aria-label={label}
        aria-describedby={reason ? reasonId : undefined}
        onCheckedChange={onChange}
      />
      {reason && (
        <span id={reasonId} className={reasonVisible ? "text-[11px] leading-4 text-muted-foreground" : "sr-only"}>
          {reason}
        </span>
      )}
    </span>
  );
}

function RolePicker({
  roles,
  selected,
  onChange,
  ceiling,
  readOnly = false,
  note,
}: {
  roles: RoleSummary[] | undefined;
  selected: string[];
  onChange: (ids: string[]) => void;
  ceiling: Ceiling;
  readOnly?: boolean;
  note?: string;
}) {
  const selectedSet = useMemo(() => new Set(selected), [selected]);
  return (
    <Card>
      <CardHeader>
        <CardTitle>Roles</CardTitle>
        <CardDescription>An admin can do anything any of their roles allows.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {note && (
          <p className="flex items-start gap-2 rounded-lg border border-border bg-muted/50 px-3 py-2 text-[13px] text-muted-foreground">
            <Lock aria-hidden className="mt-0.5 size-3.5 shrink-0" />
            {note}
          </p>
        )}
        {roles === undefined ? (
          <p className="text-sm text-muted-foreground">Loading roles…</p>
        ) : roles.length === 0 ? (
          <p className="text-sm text-muted-foreground">No roles yet. Create a role first.</p>
        ) : (
          <fieldset>
            <legend className="sr-only">Roles</legend>
            <ul className="grid gap-2 md:grid-cols-2">
              {roles.map((role) => {
                const checked = selectedSet.has(role.id);
                const block = readOnly ? null : roleAssignmentBlock(ceiling, role);
                const disabled = readOnly || block !== null;
                const id = `role-${role.id}`;
                return (
                  <li key={role.id}>
                    <div
                      className={cn(
                        "flex h-full gap-3 rounded-lg border px-3.5 py-3 transition-colors",
                        checked ? "border-primary/40 bg-primary/5" : "border-border",
                        disabled && !checked && "opacity-70",
                      )}
                    >
                      <Checkbox
                        id={id}
                        className="mt-0.5"
                        checked={checked}
                        disabled={disabled}
                        aria-describedby={`${id}-note`}
                        onCheckedChange={(next) => {
                          const ids = new Set(selectedSet);
                          if (next) {
                            ids.add(role.id);
                          } else {
                            ids.delete(role.id);
                          }
                          onChange(roles.map((item) => item.id).filter((value) => ids.has(value)));
                        }}
                      />
                      <div className="min-w-0 space-y-0.5">
                        <div className="flex flex-wrap items-center gap-1.5">
                          <Label htmlFor={id} className={cn("font-medium", !disabled && "cursor-pointer")}>
                            {role.label}
                          </Label>
                          {isSuperAdminRole(role) && (
                            <ShieldCheck aria-hidden className="size-3.5 text-warning" />
                          )}
                          {isBuiltInRole(role.developerName) && <Badge variant="outline">Built-in</Badge>}
                        </div>
                        <div id={`${id}-note`} className="space-y-0.5 text-[13px] leading-5 text-muted-foreground">
                          <p>{describeAccess(role)}</p>
                          {block && (
                            <p className="flex items-center gap-1 text-xs font-medium text-foreground/70">
                              <Lock aria-hidden className="size-3 shrink-0" />
                              {block}
                            </p>
                          )}
                        </div>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ul>
          </fieldset>
        )}
      </CardContent>
    </Card>
  );
}

function RoleBadges({ roles }: { roles: RoleSummary[] }) {
  if (roles.length === 0) {
    return <span className="text-muted-foreground">No roles</span>;
  }
  return (
    <span className="inline-flex flex-wrap gap-1">
      {roles.map((role) => (
        <Badge key={role.id} variant={isSuperAdminRole(role) ? "warning" : "secondary"}>
          {role.label}
        </Badge>
      ))}
    </span>
  );
}

function LockedNotice({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div
      role="note"
      className="flex gap-3 rounded-xl border border-warning-border bg-warning-soft px-4 py-3"
    >
      <Lock aria-hidden className="mt-0.5 size-4 shrink-0 text-warning" />
      <div className="space-y-0.5">
        <p className="text-sm font-semibold text-foreground">{title}</p>
        <p className="text-[13px] leading-5 text-muted-foreground">{children}</p>
      </div>
    </div>
  );
}

function FormActions({ error, children }: { error: unknown; children: ReactNode }) {
  return (
    <div className="space-y-3">
      <InlineError error={error} />
      <div className="flex items-center gap-2">{children}</div>
    </div>
  );
}

function InlineError({ error }: { error: unknown }) {
  if (!error) {
    return null;
  }
  return (
    <p
      role="alert"
      className="rounded-lg border border-destructive-border bg-destructive-soft px-3 py-2 text-sm text-destructive-soft-foreground"
    >
      {typeof error === "string" ? error : formatError(error)}
    </p>
  );
}

function NameEmailFields({
  firstName,
  lastName,
  emailAddress,
  onFirstName,
  onLastName,
  onEmail,
  disabled = false,
}: {
  firstName: string;
  lastName: string;
  emailAddress: string;
  onFirstName: (value: string) => void;
  onLastName: (value: string) => void;
  onEmail: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <FormField label="First name" required htmlFor="admin-first">
        {(control) => (
          <Input {...control} value={firstName} disabled={disabled} onChange={(event) => onFirstName(event.target.value)} />
        )}
      </FormField>
      <FormField label="Last name" required htmlFor="admin-last">
        {(control) => (
          <Input {...control} value={lastName} disabled={disabled} onChange={(event) => onLastName(event.target.value)} />
        )}
      </FormField>
      <div className="sm:col-span-2">
        <FormField label="Email" required htmlFor="admin-email">
          {(control) => (
            <Input
              {...control}
              type="email"
              value={emailAddress}
              disabled={disabled}
              onChange={(event) => onEmail(event.target.value)}
            />
          )}
        </FormField>
      </div>
    </div>
  );
}

function useRoleSummaries(): RoleSummary[] | undefined {
  const query = useQuery({
    queryKey: ["roles", "picker"],
    queryFn: () => adminApi.roles.list({ pageSize: 100 }),
    placeholderData: keepPreviousData,
  });
  return useMemo(() => {
    if (!query.data) {
      return undefined;
    }
    const summaries = query.data.items.map(readRoleSummary);
    return summaries.sort((a, b) => rolePriority(a) - rolePriority(b) || a.label.localeCompare(b.label));
  }, [query.data]);
}

function rolePriority(role: RoleSummary): number {
  if (role.developerName === SUPER_ADMIN_ROLE) {
    return 0;
  }
  return isBuiltInRole(role.developerName) ? 1 : 2;
}

function describeAccess(role: RoleSummary): string {
  if (isSuperAdminRole(role)) {
    return "Every permission, always";
  }
  const system = role.systemPermissions.length;
  const types = role.systemPermissions.includes(CONTENT_TYPES_PERMISSION) ? null : Object.keys(role.contentTypes).length;
  const parts: string[] = [];
  parts.push(system === 1 ? "1 system permission" : `${system} system permissions`);
  parts.push(types === null ? "all content types" : types === 1 ? "1 content type" : `${types} content types`);
  return parts.join(" · ");
}

function canToggleSystem(ceiling: Ceiling, permission: string): boolean {
  const partner = PAIRED_SYSTEM_PERMISSIONS[permission];
  return canGrantSystem(ceiling, permission) && (partner === undefined || canGrantSystem(ceiling, partner));
}

function systemLockReason(
  permission: string,
  context: { ceiling: Ceiling; readOnly: boolean; fullAccess: boolean; labels: Map<string, string> },
): string | null {
  if (context.readOnly || context.fullAccess) {
    return null;
  }
  if (!canGrantSystem(context.ceiling, permission)) {
    return "You do not have this permission, so you cannot grant it.";
  }
  const partner = PAIRED_SYSTEM_PERMISSIONS[permission];
  if (partner && !canGrantSystem(context.ceiling, partner)) {
    return `Needs ${labelFor(context.labels, partner) ?? partner}, which you do not have.`;
  }
  return null;
}

function labelFor(labels: Map<string, string>, permission: string | undefined): string | null {
  return permission ? (labels.get(permission) ?? permission) : null;
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

function normalizePermissionMap(map: Record<string, string[]>): Record<string, string[]> {
  const normalized: Record<string, string[]> = {};
  for (const [key, access] of Object.entries(map)) {
    const next = withImpliedRead(access);
    if (next.length > 0) {
      normalized[key] = next;
    }
  }
  return normalized;
}

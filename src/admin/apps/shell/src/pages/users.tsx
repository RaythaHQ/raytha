import { adminApi, currentSession, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
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
import { useState, type FormEvent } from "react";
import { ImpersonateCard } from "../components/impersonate-card";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { CrudListPage } from "./crud-list";
import { displayName, entityFields, formatWhen, isRecord, readBoolean, readString } from "./entity";
import { UsersSectionTabs } from "./users-section-tabs";

export function UsersPage() {
  return (
    <CrudListPage
      title="Users"
      description="Website member accounts."
      queryKey={["users"]}
      listKey="users"
      noun="user"
      list={adminApi.users.list}
      createPermission={platformPermissions.users}
      createLabel="New user"
      createTo="/users/new"
      tabs={<UsersSectionTabs active="users" />}
      columns={[
        {
          header: "Name",
          cell: (entity) => (
            <Link to="/users/$id" params={{ id: entity.id }} className="text-primary hover:underline">
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

export function UserGroupsPage() {
  return (
    <CrudListPage
      title="User groups"
      description="Groups for assigning membership on users."
      queryKey={["user-groups"]}
      listKey="user-groups"
      noun="user group"
      list={adminApi.userGroups.list}
      createPermission={platformPermissions.users}
      createLabel="New group"
      createTo="/users/groups/new"
      tabs={<UsersSectionTabs active="groups" />}
      columns={[
        {
          header: "Label",
          cell: (entity) => (
            <Link to="/users/groups/$id" params={{ id: entity.id }} className="text-primary hover:underline">
              {readString(entityFields(entity), "label") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
      ]}
    />
  );
}

export function NewUserPage() {
  useDocumentTitle(["New user"]);
  const navigate = useNavigate();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [emailAddress, setEmailAddress] = useState("");
  const [sendEmail, setSendEmail] = useState(false);
  const [groupIds, setGroupIds] = useState<string[]>([]);

  const groupsQuery = useQuery({
    queryKey: ["user-groups", "picker"],
    queryFn: () => adminApi.userGroups.list({ pageSize: 100 }),
  });

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.users.create({ firstName, lastName, emailAddress, sendEmail, userGroups: groupIds }),
    onSuccess: (created) => {
      toast.success("User created");
      void navigate({ to: "/users/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/users" listKey="users" label="users" />} title="New user" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <UserFields
              firstName={firstName}
              lastName={lastName}
              emailAddress={emailAddress}
              onFirstName={setFirstName}
              onLastName={setLastName}
              onEmail={setEmailAddress}
            />
            <div className="flex items-center gap-2">
              <Checkbox id="user-send-email" checked={sendEmail} onCheckedChange={setSendEmail} />
              <Label htmlFor="user-send-email">Send welcome email</Label>
            </div>
            <GroupPicker
              groups={groupsQuery.data?.items ?? []}
              selected={groupIds}
              onChange={setGroupIds}
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

export function EditUserPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  const query = useQuery({
    queryKey: ["users", id],
    queryFn: () => adminApi.users.get(id),
    enabled: id.length > 0,
  });
  useDocumentTitle([query.data ? displayName(query.data) : "User", "Users"]);

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit user" />
        <p className="text-sm text-muted-foreground">Missing user id.</p>
      </div>
    );
  }

  return <QueryGate query={query}>{(user) => <UserEditForm key={user.id} user={user} />}</QueryGate>;
}

function UserEditForm({ user }: { user: EntityRef }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const fields = entityFields(user);
  const isActive = readBoolean(fields, "isActive");
  const isSelf = currentSession()?.id === user.id;
  const [firstName, setFirstName] = useState(readString(fields, "firstName"));
  const [lastName, setLastName] = useState(readString(fields, "lastName"));
  const [emailAddress, setEmailAddress] = useState(readString(fields, "emailAddress"));
  const [groupIds, setGroupIds] = useState<string[]>(readIds(fields.userGroups));
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [sendEmail, setSendEmail] = useState(true);

  const groupsQuery = useQuery({
    queryKey: ["user-groups", "picker"],
    queryFn: () => adminApi.userGroups.list({ pageSize: 100 }),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["users"] });
    void queryClient.invalidateQueries({ queryKey: ["users", user.id] });
  };

  const mutation = useMutation({
    mutationFn: () => adminApi.users.update(user.id, { firstName, lastName, emailAddress, userGroups: groupIds }),
    onSuccess: () => {
      toast.success("User updated");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const setActive = useMutation({
    mutationFn: () => (isActive ? adminApi.users.suspend(user.id) : adminApi.users.restore(user.id)),
    onSuccess: () => {
      toast.success(isActive ? "User suspended" : "User restored");
      invalidate();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const resetPassword = useMutation({
    mutationFn: () =>
      adminApi.users.resetPassword(user.id, { newPassword, confirmNewPassword, sendEmail }),
    onSuccess: () => {
      toast.success(sendEmail ? "Password reset and email sent" : "Password reset");
      setNewPassword("");
      setConfirmNewPassword("");
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const remove = useMutation({
    mutationFn: () => adminApi.users.remove(user.id),
    onSuccess: () => {
      toast.success("User deleted");
      void queryClient.invalidateQueries({ queryKey: ["users"] });
      void navigate({ to: "/users" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/users" listKey="users" label="users" />}
        title={displayName(user)}
        meta={
          <>
            {isActive ? <Badge variant="success">Active</Badge> : <Badge variant="secondary">Suspended</Badge>}
            {isSelf && <Badge variant="info">You</Badge>}
            <span>Last login {formatWhen(fields.lastLoggedInTime) || "never"}</span>
          </>
        }
      />
      <Card>
        <CardHeader>
          <CardTitle>Details</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <UserFields
              firstName={firstName}
              lastName={lastName}
              emailAddress={emailAddress}
              onFirstName={setFirstName}
              onLastName={setLastName}
              onEmail={setEmailAddress}
            />
            <GroupPicker
              groups={groupsQuery.data?.items ?? []}
              selected={groupIds}
              onChange={setGroupIds}
            />
            <Button type="submit" loading={mutation.isPending}>
              Save changes
            </Button>
          </form>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Account access</CardTitle>
          <CardDescription>
            {isSelf
              ? "You cannot suspend your own account."
              : isActive
                ? "Suspending blocks sign-in and keeps the account, its groups, and its history."
                : "This account is suspended and cannot sign in. Restoring lets them sign in again."}
          </CardDescription>
        </CardHeader>
        {!isSelf && (
          <CardContent>
            <Button type="button" variant="outline" loading={setActive.isPending} onClick={() => setActive.mutate()}>
              {isActive ? "Suspend" : "Restore"}
            </Button>
          </CardContent>
        )}
      </Card>
      <ImpersonateCard
        id={user.id}
        name={displayName(user)}
        kind={readBoolean(fields, "isAdmin") ? "admin" : "websiteUser"}
        isActive={isActive}
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
                  toast.error("Password must be at least 8 characters.");
                  return;
                }
                if (newPassword !== confirmNewPassword) {
                  toast.error("Confirm password did not match.");
                  return;
                }
                resetPassword.mutate();
              }}
            >
              <FormField label="New password" required htmlFor="user-new-password" hint="At least 8 characters.">
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
              <FormField label="Confirm password" required htmlFor="user-confirm-password">
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
                <Checkbox id="user-reset-send-email" checked={sendEmail} onCheckedChange={setSendEmail} />
                <Label htmlFor="user-reset-send-email">Email the new password</Label>
              </div>
              <Button type="submit" variant="outline" loading={resetPassword.isPending}>
                Reset password
              </Button>
            </form>
          </CardContent>
        )}
      </Card>
      {!isSelf && (
        <DangerZone
          description="Deletes the account. Audit history keeps their name. This cannot be undone."
          actionLabel="Delete user"
          confirmTitle={`Delete ${displayName(user)}?`}
          confirmBody="This cannot be undone."
          onConfirm={() => remove.mutate()}
          pending={remove.isPending}
        />
      )}
    </div>
  );
}

export function NewUserGroupPage() {
  useDocumentTitle(["New user group"]);
  const navigate = useNavigate();
  const [label, setLabel] = useState("");
  const [developerName, setDeveloperName] = useState("");
  const [developerTouched, setDeveloperTouched] = useState(false);

  const mutation = useMutation({
    mutationFn: () => adminApi.userGroups.create({ label, developerName }),
    onSuccess: (created) => {
      toast.success("User group created");
      void navigate({ to: "/users/groups/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/users/groups" listKey="user-groups" label="user groups" />} title="New user group" />
      <Card>
        <CardContent className="pt-6">
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              mutation.mutate();
            }}
          >
            <FormField label="Label" required htmlFor="group-label">
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
            <FormField label="Developer name" required htmlFor="group-developer">
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

export function EditUserGroupPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Edit user group"]);
  const query = useQuery({
    queryKey: ["user-groups", id],
    queryFn: () => adminApi.userGroups.get(id),
    enabled: id.length > 0,
  });

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit user group" />
        <p className="text-sm text-muted-foreground">Missing group id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader back={<ListBackLink to="/users/groups" listKey="user-groups" label="user groups" />} title="Edit user group" />
      <QueryGate query={query}>{(group) => <UserGroupEditForm group={group} />}</QueryGate>
    </div>
  );
}

function UserGroupEditForm({ group }: { group: EntityRef }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const fields = entityFields(group);
  const [label, setLabel] = useState(readString(fields, "label"));

  const mutation = useMutation({
    mutationFn: () => adminApi.userGroups.update(group.id, { label }),
    onSuccess: () => {
      toast.success("User group updated");
      void queryClient.invalidateQueries({ queryKey: ["user-groups"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const remove = useMutation({
    mutationFn: () => adminApi.userGroups.remove(group.id),
    onSuccess: () => {
      toast.success("User group deleted");
      void queryClient.invalidateQueries({ queryKey: ["user-groups"] });
      void navigate({ to: "/users/groups" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
    <div className="space-y-6">
      <Card>
        <CardContent className="pt-6">
          <form className="space-y-4" onSubmit={handleSubmit}>
            <FormField label="Label" required htmlFor="edit-group-label">
              {(control) => <Input {...control} value={label} onChange={(event) => setLabel(event.target.value)} />}
            </FormField>
            <p className="text-sm text-muted-foreground">
              Developer name {readString(fields, "developerName") || "—"}
            </p>
            <Button type="submit" loading={mutation.isPending}>
              Save
            </Button>
          </form>
        </CardContent>
      </Card>
      <DangerZone
        description="Delete this group. Unassign every member first."
        actionLabel="Delete group"
        confirmTitle="Delete user group?"
        confirmBody="This cannot be undone."
        onConfirm={() => remove.mutate()}
        pending={remove.isPending}
      />
    </div>
  );
}

function UserFields({
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
    <div className="grid gap-4 sm:grid-cols-2">
      <FormField label="First name" required htmlFor="user-first">
        {(control) => <Input {...control} value={firstName} onChange={(event) => onFirstName(event.target.value)} />}
      </FormField>
      <FormField label="Last name" required htmlFor="user-last">
        {(control) => <Input {...control} value={lastName} onChange={(event) => onLastName(event.target.value)} />}
      </FormField>
      <div className="sm:col-span-2">
        <FormField label="Email" required htmlFor="user-email">
          {(control) => (
            <Input
              {...control}
              type="email"
              value={emailAddress}
              onChange={(event) => onEmail(event.target.value)}
            />
          )}
        </FormField>
      </div>
    </div>
  );
}

function GroupPicker({
  groups,
  selected,
  onChange,
}: {
  groups: EntityRef[];
  selected: string[];
  onChange: (ids: string[]) => void;
}) {
  const selectedSet = new Set(selected);
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-medium">User groups</legend>
      {groups.length === 0 ? (
        <p className="text-sm text-muted-foreground">No groups yet.</p>
      ) : (
        groups.map((group) => {
          const label = readString(entityFields(group), "label") || group.id;
          return (
            <div key={group.id} className="flex items-center gap-2">
              <Checkbox
                id={`user-group-${group.id}`}
                checked={selectedSet.has(group.id)}
                onCheckedChange={(checked) => {
                  const next = new Set(selectedSet);
                  if (checked) {
                    next.add(group.id);
                  } else {
                    next.delete(group.id);
                  }
                  onChange([...next]);
                }}
              />
              <Label htmlFor={`user-group-${group.id}`}>{label}</Label>
            </div>
          );
        })
      )}
    </fieldset>
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

function toAutoName(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

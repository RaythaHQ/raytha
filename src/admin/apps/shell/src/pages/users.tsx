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
import { useDocumentTitle } from "../lib/document-title";
import { CrudListPage } from "./crud-list";
import { displayName, entityFields, formatWhen, isRecord, readBoolean, readString } from "./entity";

export function UsersPage() {
  return (
    <CrudListPage
      title="Users"
      description="Website member accounts."
      queryKey={["users"]}
      listKey="users"
      noun="user"
      list={adminApi.users.list}
      remove={adminApi.users.remove}
      createPermission={platformPermissions.users}
      createLabel="New user"
      createTo="/users/new"
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
      queryKey={["user-groups"]}
      listKey="user-groups"
      noun="user group"
      list={adminApi.userGroups.list}
      remove={adminApi.userGroups.remove}
      createPermission={platformPermissions.users}
      createLabel="New group"
      createTo="/users/groups/new"
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
      <PageHeader title="New user" />
      <ListBackLink to="/users" listKey="users" label="users" />
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
  useDocumentTitle(["Edit user"]);
  const query = useQuery({
    queryKey: ["users", id],
    queryFn: () => adminApi.users.get(id),
    enabled: id.length > 0,
  });

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit user" />
        <p className="text-sm text-muted-foreground">Missing user id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title="Edit user" />
      <ListBackLink to="/users" listKey="users" label="users" />
      <QueryGate query={query}>{(user) => <UserEditForm user={user} />}</QueryGate>
    </div>
  );
}

function UserEditForm({ user }: { user: EntityRef }) {
  const queryClient = useQueryClient();
  const fields = entityFields(user);
  const [firstName, setFirstName] = useState(readString(fields, "firstName"));
  const [lastName, setLastName] = useState(readString(fields, "lastName"));
  const [emailAddress, setEmailAddress] = useState(readString(fields, "emailAddress"));
  const [groupIds, setGroupIds] = useState<string[]>(readIds(fields.userGroups));

  const groupsQuery = useQuery({
    queryKey: ["user-groups", "picker"],
    queryFn: () => adminApi.userGroups.list({ pageSize: 100 }),
  });

  const mutation = useMutation({
    mutationFn: () => adminApi.users.update(user.id, { firstName, lastName, emailAddress, userGroups: groupIds }),
    onSuccess: () => {
      toast.success("User updated");
      void queryClient.invalidateQueries({ queryKey: ["users"] });
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
            Save
          </Button>
        </form>
      </CardContent>
    </Card>
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
      <PageHeader title="New user group" />
      <ListBackLink to="/users/groups" listKey="user-groups" label="user groups" />
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
      <PageHeader title="Edit user group" />
      <ListBackLink to="/users/groups" listKey="user-groups" label="user groups" />
      <QueryGate query={query}>{(group) => <UserGroupEditForm group={group} />}</QueryGate>
    </div>
  );
}

function UserGroupEditForm({ group }: { group: EntityRef }) {
  const queryClient = useQueryClient();
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

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
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
    <>
      <FormField label="First name" required htmlFor="user-first">
        {(control) => <Input {...control} value={firstName} onChange={(event) => onFirstName(event.target.value)} />}
      </FormField>
      <FormField label="Last name" required htmlFor="user-last">
        {(control) => <Input {...control} value={lastName} onChange={(event) => onLastName(event.target.value)} />}
      </FormField>
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
    </>
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

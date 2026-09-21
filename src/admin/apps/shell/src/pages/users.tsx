import { adminApi, formatError, platformPermissions } from "@raytha/api";
import type { EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  QueryGate,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { CrudListPage } from "./crud-list";
import { displayName, entityFields, formatWhen, readBoolean, readString } from "./entity";

export function UsersPage() {
  const [editId, setEditId] = useState<string | null>(null);

  return (
    <>
      <CrudListPage
        title="Users"
        description="Public user accounts."
        queryKey={["users"]}
        noun="user"
        list={adminApi.users.list}
        create={adminApi.users.create}
        remove={adminApi.users.remove}
        createPermission={platformPermissions.users}
        createLabel="New user"
        createFields={[
          { key: "firstName", label: "First name", required: true },
          { key: "lastName", label: "Last name", required: true },
          { key: "emailAddress", label: "Email", type: "email", required: true },
          { key: "sendEmail", label: "Send welcome email", type: "checkbox" },
        ]}
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
        rowActions={(entity) => (
          <Button type="button" variant="outline" size="sm" onClick={() => setEditId(entity.id)}>
            Edit
          </Button>
        )}
      />
      {editId && <UserEditDialog id={editId} onClose={() => setEditId(null)} />}
    </>
  );
}

export function UserGroupsPage() {
  return (
    <CrudListPage
      title="User groups"
      queryKey={["user-groups"]}
      noun="user group"
      list={adminApi.userGroups.list}
      create={adminApi.userGroups.create}
      remove={adminApi.userGroups.remove}
      createPermission={platformPermissions.users}
      createLabel="New group"
      createFields={[
        { key: "label", label: "Label", required: true, autoDeveloperNameFrom: "developerName" },
        { key: "developerName", label: "Developer name", required: true },
      ]}
      columns={[
        { header: "Label", cell: (entity) => readString(entityFields(entity), "label") || entity.id },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
      ]}
    />
  );
}

function UserEditDialog({ id, onClose }: { id: string; onClose: () => void }) {
  const query = useQuery({
    queryKey: ["users", id],
    queryFn: () => adminApi.users.get(id),
  });

  return (
    <Dialog open onOpenChange={(open) => { if (!open) onClose(); }}>
      <DialogHeader>
        <DialogTitle>Edit user</DialogTitle>
      </DialogHeader>
      <QueryGate query={query}>{(user) => <UserEditForm user={user} onClose={onClose} />}</QueryGate>
    </Dialog>
  );
}

function UserEditForm({ user, onClose }: { user: EntityRef; onClose: () => void }) {
  const queryClient = useQueryClient();
  const fields = entityFields(user);
  const [firstName, setFirstName] = useState(readString(fields, "firstName"));
  const [lastName, setLastName] = useState(readString(fields, "lastName"));
  const [emailAddress, setEmailAddress] = useState(readString(fields, "emailAddress"));

  const mutation = useMutation({
    mutationFn: () => adminApi.users.update(user.id, { firstName, lastName, emailAddress }),
    onSuccess: () => {
      toast.success("User updated");
      void queryClient.invalidateQueries({ queryKey: ["users"] });
      onClose();
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate();
  };

  return (
    <form onSubmit={handleSubmit}>
      <DialogContent className="space-y-4">
        <FormField label="First name" required htmlFor="edit-first-name">
          {(control) => <Input {...control} value={firstName} onChange={(event) => setFirstName(event.target.value)} />}
        </FormField>
        <FormField label="Last name" required htmlFor="edit-last-name">
          {(control) => <Input {...control} value={lastName} onChange={(event) => setLastName(event.target.value)} />}
        </FormField>
        <FormField label="Email" required htmlFor="edit-email">
          {(control) => (
            <Input
              {...control}
              type="email"
              value={emailAddress}
              onChange={(event) => setEmailAddress(event.target.value)}
            />
          )}
        </FormField>
      </DialogContent>
      <DialogFooter>
        <Button type="button" variant="outline" onClick={onClose}>
          Cancel
        </Button>
        <Button type="submit" loading={mutation.isPending}>
          Save
        </Button>
      </DialogFooter>
    </form>
  );
}

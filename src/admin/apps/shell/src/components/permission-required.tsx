import { EmptyState, PageHeader } from "@raytha/ui";
import { LockKeyhole } from "lucide-react";

/** Page-level denied state that names the permission, so the admin knows what to ask for. */
export function PermissionRequired({ title, permissionLabel }: { title: string; permissionLabel: string }) {
  return (
    <div className="space-y-6">
      <PageHeader title={title} />
      <EmptyState
        icon={LockKeyhole}
        title="No access"
        hint={`This page requires the ${permissionLabel} permission. Ask an administrator if you need it.`}
      />
    </div>
  );
}

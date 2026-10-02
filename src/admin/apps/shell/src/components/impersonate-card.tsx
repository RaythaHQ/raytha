import { adminApi, currentSession, formatError, hasPermission, platformPermissions } from "@raytha/api";
import { AlertDialog, Button, Card, CardContent, CardDescription, CardHeader, CardTitle, toast } from "@raytha/ui";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { SUPER_ADMIN_ROLE } from "../lib/role-permissions";

export type ImpersonationTarget = "websiteUser" | "admin";

/** Presentation only; the server's policies and ImpersonationRules decide. */
function canImpersonate(kind: ImpersonationTarget, id: string, isActive: boolean): boolean {
  const session = currentSession();
  if (!session || session.impersonation || session.id === id || !isActive) {
    return false;
  }
  return kind === "admin" ? session.roles.includes(SUPER_ADMIN_ROLE) : hasPermission(platformPermissions.users);
}

export function ImpersonateCard({
  id,
  name,
  kind,
  isActive,
}: {
  id: string;
  name: string;
  kind: ImpersonationTarget;
  isActive: boolean;
}) {
  const [confirming, setConfirming] = useState(false);
  const start = useMutation({
    mutationFn: () => (kind === "admin" ? adminApi.impersonation.startAdmin(id) : adminApi.impersonation.startUser(id)),
    onSuccess: ({ redirectUrl }) => window.location.assign(redirectUrl),
    onError: (error) => {
      setConfirming(false);
      toast.error(formatError(error));
    },
  });

  if (!canImpersonate(kind, id, isActive)) {
    return null;
  }

  const where = kind === "admin" ? "this admin with their roles" : "the public site";
  return (
    <Card>
      <CardHeader>
        <CardTitle>Impersonate</CardTitle>
        <CardDescription>
          Sign in as {name} to see {where} the way they do. The session ends on its own after a fixed time limit, and
          everything you do is audited under both your names.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Button type="button" variant="outline" onClick={() => setConfirming(true)}>
          Impersonate
        </Button>
        <AlertDialog
          open={confirming}
          onOpenChange={setConfirming}
          title={`Sign in as ${name}?`}
          body={
            kind === "admin"
              ? "You will use the admin as this administrator until you stop impersonating from the banner at the top."
              : "You will leave the admin and browse the public site as this user. End impersonation from the banner at the bottom of the page."
          }
          confirmLabel="Impersonate"
          onConfirm={() => start.mutate()}
          pending={start.isPending || start.isSuccess}
        />
      </CardContent>
    </Card>
  );
}

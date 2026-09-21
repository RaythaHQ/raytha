import type { TemplateRevision } from "@raytha/api";
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState } from "@raytha/ui";
import { History } from "lucide-react";
import { formatWhen } from "../entity";

export function RevisionsPanel({
  revisions,
  pendingId,
  onRevert,
}: {
  revisions: TemplateRevision[];
  pendingId: string | null;
  onRevert: (revisionId: string) => void;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Revisions</CardTitle>
      </CardHeader>
      <CardContent>
        {revisions.length === 0 ? (
          <EmptyState icon={History} title="No revisions" hint="Save the template to start a history." />
        ) : (
          <ul className="space-y-3">
            {revisions.map((revision) => (
              <li key={revision.id} className="rounded-lg border border-border px-3 py-2 text-sm">
                <div className="font-medium">{formatWhen(revision.creationTime) || "Unknown date"}</div>
                {revision.creatorName ? (
                  <div className="text-muted-foreground">{revision.creatorName}</div>
                ) : null}
                {revision.subject || revision.label ? (
                  <div className="truncate text-muted-foreground">{revision.subject || revision.label}</div>
                ) : null}
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="mt-2"
                  loading={pendingId === revision.id}
                  onClick={() => onRevert(revision.id)}
                >
                  Revert
                </Button>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

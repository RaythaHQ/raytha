import { FormField, Input } from "@raytha/ui";

export const DEFAULT_ROUTE_TEMPLATE = "{ContentTypeDeveloperName}/{PrimaryField}";

const ROUTE_TOKENS = [
  { token: "{ContentTypeDeveloperName}", meaning: "developer name" },
  { token: "{PrimaryField}", meaning: "primary field, slugified" },
  { token: "{Id}", meaning: "item id" },
  { token: "{CurrentYear}", meaning: "year created" },
  { token: "{CurrentMonth}", meaning: "month created" },
] as const;

function exampleRoute(template: string, developerName: string): string {
  const now = new Date();
  return template
    .replaceAll("{ContentTypeDeveloperName}", developerName || "content_type")
    .replaceAll("{PrimaryField}", "my-first-item")
    .replaceAll("{Id}", "a1b2c3d4")
    .replaceAll("{CurrentYear}", String(now.getFullYear()))
    .replaceAll("{CurrentMonth}", String(now.getMonth() + 1).padStart(2, "0"))
    .replace(/^\/+/, "");
}

/** Default route template input with token help and a live example path. */
export function RouteTemplateField({
  id,
  value,
  onChange,
  developerName,
  error,
}: {
  id: string;
  value: string;
  onChange: (next: string) => void;
  developerName: string;
  error?: string;
}) {
  const example = value.trim() ? exampleRoute(value.trim(), developerName) : "";
  return (
    <FormField
      label="Default route template"
      required
      htmlFor={id}
      error={error}
      hint={
        <span className="block space-y-1.5">
          <span className="block">New items get this path. Click a token to add it:</span>
          <span className="flex flex-wrap gap-1.5">
            {ROUTE_TOKENS.map(({ token, meaning }) => (
              <button
                key={token}
                type="button"
                title={meaning}
                onClick={() => onChange(value.endsWith("/") || !value ? `${value}${token}` : `${value}/${token}`)}
                className="rounded-md border border-border bg-muted/50 px-1.5 py-0.5 font-mono text-[11px] text-foreground hover:border-primary/50 hover:bg-primary/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                {token}
              </button>
            ))}
          </span>
          {example && (
            <span className="block">
              Example: <code className="font-mono text-foreground">/{example}</code>
            </span>
          )}
        </span>
      }
    >
      {(control) => (
        <Input {...control} className="font-mono" value={value} onChange={(event) => onChange(event.target.value)} />
      )}
    </FormField>
  );
}

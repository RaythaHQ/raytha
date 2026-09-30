import { FormField, Input } from "@raytha/ui";
import { ExternalLink, TriangleAlert } from "lucide-react";
import { publicPageUrl } from "../site-pages/page-meta";

const HTTP_TRIGGER = "http_request";

export function normalizePublicPath(value: string): string {
  return value.trim().replace(/^\/+|\/+$/g, "");
}

/** The value to save: only HTTP request functions keep a public path. */
export function publicPathFor(trigger: string, value: string): string {
  return trigger === HTTP_TRIGGER ? normalizePublicPath(value) : "";
}

export function FunctionPublicPath({
  id,
  trigger,
  value,
  savedPath = "",
  onChange,
}: {
  id: string;
  trigger: string;
  value: string;
  /** The path the server has now; the live link shows only once the field matches it. */
  savedPath?: string;
  onChange: (next: string) => void;
}) {
  if (trigger !== HTTP_TRIGGER) {
    return savedPath ? (
      <p
        role="status"
        className="flex items-start gap-2 rounded-lg border border-warning-border bg-warning-soft px-3 py-2 text-sm"
      >
        <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
        <span>
          Only HTTP request functions answer at a public path. Saving removes{" "}
          <span className="font-mono">/{savedPath}</span>.
        </span>
      </p>
    ) : null;
  }

  const path = normalizePublicPath(value);
  const url = path ? `${window.location.origin}${publicPageUrl(path)}` : "";
  const live = path.length > 0 && path === savedPath;

  return (
    <FormField
      label="Public path"
      htmlFor={id}
      hint={
        path ? (
          <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <span>{live ? "Live at" : "Will answer at"}</span>
            {live ? (
              <a
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 font-mono text-foreground hover:text-primary hover:underline"
              >
                {url}
                <ExternalLink className="size-3" aria-hidden />
              </a>
            ) : (
              <span className="font-mono text-foreground">{url}</span>
            )}
            {live ? null : <span>once you save.</span>}
          </span>
        ) : (
          "Optional. Serve this function at a URL on your site, such as llms.txt or feeds/posts.xml. GET runs get(query), POST runs post(payload, query)."
        )
      }
    >
      {(control) => (
        <div className="relative">
          <span
            aria-hidden
            className="pointer-events-none absolute inset-y-0 left-3 flex items-center font-mono text-sm text-muted-foreground"
          >
            /
          </span>
          <Input
            {...control}
            value={value}
            placeholder="llms.txt"
            spellCheck={false}
            autoComplete="off"
            className="pl-6 font-mono"
            onChange={(event) => onChange(event.target.value.replace(/^\s*\/+/, ""))}
          />
        </div>
      )}
    </FormField>
  );
}

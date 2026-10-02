import { adminApi } from "@raytha/api";
import { Badge, cn, Tabs, TabsContent, TabsList, TabsTrigger } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { BookOpen, ChevronRight, Copy, TriangleAlert } from "lucide-react";
import { useState, type ReactNode } from "react";
import { copyText } from "./editors/clipboard";
import { jsonString } from "./entity";
import {
  ACCOUNT_RULES,
  AUDIENCE_LABEL,
  guideEndpoints,
  guideValues,
  JWT_CLAIMS,
  JWT_EXAMPLE_PAYLOAD,
  JWT_FLOW,
  JWT_FLOW_NOTE,
  JWT_HIGH_SECURITY,
  JWT_SIGNING_NOTES,
  JWT_SNIPPETS,
  JWT_TROUBLESHOOTING,
  JWT_URL_NOTES,
  SAML_ATTRIBUTE_NOTE,
  SAML_ATTRIBUTES,
  SAML_BRING_BACK,
  SAML_CERTIFICATE_NOTES,
  SAML_FLOW,
  SAML_FLOW_NOTE,
  SAML_IDP_RECIPES,
  SAML_IDP_VALUES,
  SAML_TROUBLESHOOTING,
  SAML_VERIFICATION,
  ssoContext,
  type Audience,
  type GuideProblem,
  type GuideRow,
  type GuideStep,
  type GuideValues,
  type SsoContext,
  type SsoSchemeType,
} from "./authentication-guide";

export function useSsoContext(developerName: string): { context: SsoContext; websiteUrlKnown: boolean } {
  const configuration = useQuery({ queryKey: ["configuration"], queryFn: () => adminApi.configuration.get() });
  const websiteUrl = configuration.data ? jsonString(configuration.data, "websiteUrl") : "";
  return {
    context: ssoContext(websiteUrl, window.location, developerName),
    websiteUrlKnown: !configuration.isPending,
  };
}

export function SsoGuidePanel({
  type,
  developerName,
  spEntityId,
  enabled,
}: {
  type: SsoSchemeType;
  developerName: string;
  spEntityId: string;
  enabled: Record<Audience, boolean>;
}) {
  const { context, websiteUrlKnown } = useSsoContext(developerName);
  const values = guideValues(context, type, spEntityId);

  return (
    <aside
      aria-label={`${type === "jwt" ? "JWT" : "SAML"} setup guide`}
      className="flex min-h-0 flex-col overflow-hidden rounded-xl border border-border bg-card shadow-card xl:sticky xl:top-4 xl:max-h-[calc(100vh-2rem)]"
    >
      <div className="space-y-1.5 border-b border-border p-4">
        <div className="flex items-center gap-2">
          <BookOpen className="size-4 text-primary" aria-hidden />
          <h2 className="font-display text-sm font-semibold">{type === "jwt" ? "JWT" : "SAML"} setup guide</h2>
        </div>
        <p className="text-[13px] leading-5 text-muted-foreground">
          {type === "jwt"
            ? "Your app signs people in and hands Raytha a signed token. Everything below matches what Raytha checks."
            : "Your identity provider signs people in and posts a signed assertion to Raytha. Everything below matches what Raytha checks."}
        </p>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto">
        <div className="border-b border-border p-4">
          <YourUrls type={type} context={context} enabled={enabled} websiteUrlKnown={websiteUrlKnown} />
        </div>
        {type === "jwt" ? <JwtGuide context={context} values={values} /> : <SamlGuide values={values} />}
      </div>
    </aside>
  );
}

function YourUrls({
  type,
  context,
  enabled,
  websiteUrlKnown,
}: {
  type: SsoSchemeType;
  context: SsoContext;
  enabled: Record<Audience, boolean>;
  websiteUrlKnown: boolean;
}) {
  const endpoints = guideEndpoints(context, type);
  return (
    <section aria-label="Your URLs" className="space-y-3">
      {websiteUrlKnown && !context.websiteUrlConfigured ? (
        <Callout>
          Website URL is not set, so these use this browser's address. Set it in{" "}
          <Link to="/settings/configuration" className="font-medium text-foreground underline underline-offset-2">
            Configuration
          </Link>
          . Admin callbacks are built from it.
        </Callout>
      ) : null}
      {context.developerName ? null : <Callout>Type a developer name to fill in the URLs.</Callout>}
      {(["users", "admins"] as const).map((audience) => (
        <div key={audience} className="space-y-2 rounded-lg bg-muted/60 p-3">
          <div className="flex items-center justify-between gap-2">
            <p className="text-xs font-medium uppercase tracking-wider text-muted-foreground">{AUDIENCE_LABEL[audience]}</p>
            <Badge variant={enabled[audience] ? "success" : "secondary"}>{enabled[audience] ? "Enabled" : "Off"}</Badge>
          </div>
          {endpoints
            .filter((endpoint) => endpoint.audience === audience)
            .map((endpoint) => (
              <div key={endpoint.role} className="space-y-1">
                <CopyValue label={`${AUDIENCE_LABEL[audience]} ${endpoint.label}`} caption={endpoint.label} value={endpoint.url} />
                <p className="text-xs leading-4 text-muted-foreground">{endpoint.detail}</p>
              </div>
            ))}
        </div>
      ))}
    </section>
  );
}

function JwtGuide({ context, values }: { context: SsoContext; values: GuideValues }) {
  const [language, setLanguage] = useState(JWT_SNIPPETS[0]?.id ?? "node");
  return (
    <>
      <GuideSection title="How sign-in works" defaultOpen>
        <Steps steps={JWT_FLOW} values={values} />
        <Note>{JWT_FLOW_NOTE}</Note>
      </GuideSection>
      <GuideSection title="Claims">
        <Rows rows={JWT_CLAIMS} />
      </GuideSection>
      <GuideSection title="Example payload">
        <CodeBlock code={JWT_EXAMPLE_PAYLOAD} label="example payload" />
      </GuideSection>
      <GuideSection title="Sign a token">
        <Bullets items={JWT_SIGNING_NOTES} />
        <Tabs value={language} onValueChange={setLanguage} className="space-y-2 px-4 pt-1">
          <TabsList aria-label="Snippet language">
            {JWT_SNIPPETS.map((snippet) => (
              <TabsTrigger key={snippet.id} value={snippet.id}>
                {snippet.label}
              </TabsTrigger>
            ))}
          </TabsList>
          {JWT_SNIPPETS.map((snippet) => (
            <TabsContent key={snippet.id} value={snippet.id} className="-mx-4">
              <CodeBlock code={snippet.code(context.origin)} label={`${snippet.label} snippet`} />
            </TabsContent>
          ))}
        </Tabs>
      </GuideSection>
      <GuideSection title="High security (jti)">
        <Bullets items={JWT_HIGH_SECURITY} />
      </GuideSection>
      <GuideSection title="Sign-in URL, sign-out URL, button text">
        <Bullets items={JWT_URL_NOTES} />
      </GuideSection>
      <GuideSection title="Accounts and groups">
        <Bullets items={ACCOUNT_RULES("sub")} />
      </GuideSection>
      <GuideSection title="Troubleshooting">
        <Problems problems={JWT_TROUBLESHOOTING} />
      </GuideSection>
    </>
  );
}

function SamlGuide({ values }: { values: GuideValues }) {
  const [provider, setProvider] = useState(SAML_IDP_RECIPES[0]?.id ?? "okta");
  return (
    <>
      <GuideSection title="How sign-in works" defaultOpen>
        <Steps steps={SAML_FLOW} values={values} />
        <Note>{SAML_FLOW_NOTE}</Note>
      </GuideSection>
      <GuideSection title="Give your IdP these values" defaultOpen>
        <Steps steps={SAML_IDP_VALUES} values={values} />
        <p className="px-4 pt-1 text-xs font-medium uppercase tracking-wider text-muted-foreground">Bring back</p>
        <Bullets items={SAML_BRING_BACK} />
      </GuideSection>
      <GuideSection title="Attributes">
        <Rows rows={SAML_ATTRIBUTES} />
        <Note>{SAML_ATTRIBUTE_NOTE}</Note>
      </GuideSection>
      <GuideSection title="Signing and certificate">
        <p className="px-4 pt-1 text-xs font-medium uppercase tracking-wider text-muted-foreground">Raytha checks</p>
        <Bullets items={SAML_VERIFICATION} />
        <p className="px-4 pt-1 text-xs font-medium uppercase tracking-wider text-muted-foreground">Certificate</p>
        <Bullets items={SAML_CERTIFICATE_NOTES} />
      </GuideSection>
      <GuideSection title="Provider recipes">
        <Tabs value={provider} onValueChange={setProvider} className="space-y-2 px-4 pt-1">
          <TabsList aria-label="Identity provider">
            {SAML_IDP_RECIPES.map((recipe) => (
              <TabsTrigger key={recipe.id} value={recipe.id}>
                {recipe.name}
              </TabsTrigger>
            ))}
          </TabsList>
          {SAML_IDP_RECIPES.map((recipe) => (
            <TabsContent key={recipe.id} value={recipe.id} className="-mx-4">
              <Steps steps={recipe.steps} values={values} />
              {recipe.note ? <Note>{recipe.note}</Note> : null}
            </TabsContent>
          ))}
        </Tabs>
      </GuideSection>
      <GuideSection title="Accounts and groups">
        <Bullets items={ACCOUNT_RULES("NameID")} />
      </GuideSection>
      <GuideSection title="Troubleshooting">
        <Problems problems={SAML_TROUBLESHOOTING} />
      </GuideSection>
    </>
  );
}

function GuideSection({ title, defaultOpen, children }: { title: string; defaultOpen?: boolean; children: ReactNode }) {
  return (
    <details open={defaultOpen} className="group border-b border-border last:border-b-0">
      <summary className="sticky top-0 z-10 flex cursor-pointer list-none items-center gap-2 bg-card/95 px-4 py-2.5 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground backdrop-blur hover:text-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring [&::-webkit-details-marker]:hidden">
        <ChevronRight className="size-3.5 transition-transform group-open:rotate-90" aria-hidden />
        {title}
      </summary>
      <div className="space-y-2 pb-3">{children}</div>
    </details>
  );
}

function Steps({ steps, values }: { steps: readonly GuideStep[]; values: GuideValues }) {
  return (
    <ol className="space-y-3 px-4 pt-1">
      {steps.map((step, index) => (
        <li key={step.text} className="flex gap-2.5 text-[13px] leading-5">
          <span
            aria-hidden
            className="mt-px flex size-5 shrink-0 items-center justify-center rounded-full bg-primary/10 text-[11px] font-semibold tabular-nums text-primary"
          >
            {index + 1}
          </span>
          <div className="min-w-0 flex-1 space-y-1.5">
            <p className="text-foreground">{step.text}</p>
            {step.values?.map((item) => (
              <CopyValue key={item.label} label={item.label} caption={item.label} value={item.value(values)} />
            ))}
          </div>
        </li>
      ))}
    </ol>
  );
}

function Rows({ rows }: { rows: readonly GuideRow[] }) {
  return (
    <dl className="divide-y divide-border px-4">
      {rows.map((row) => (
        <div key={row.name} className="space-y-1 py-2">
          <dt className="flex flex-wrap items-center gap-2">
            <code className="font-mono text-[12.5px] font-semibold text-foreground">{row.name}</code>
            <Badge variant={requirementVariant(row.requirement)}>{row.requirement}</Badge>
          </dt>
          <dd className="text-xs leading-5 text-muted-foreground">{row.detail}</dd>
        </div>
      ))}
    </dl>
  );
}

function requirementVariant(requirement: string): "info" | "neutral" | "warning" | "outline" {
  switch (requirement) {
    case "Required":
      return "warning";
    case "Recommended":
    case "High security":
      return "info";
    case "Ignored":
      return "outline";
    default:
      return "neutral";
  }
}

function Problems({ problems }: { problems: readonly GuideProblem[] }) {
  return (
    <dl className="divide-y divide-border px-4">
      {problems.map((problem) => (
        <div key={problem.symptom} className="space-y-1 py-2">
          <dt className="text-[13px] font-medium leading-5 text-foreground">{problem.symptom}</dt>
          <dd className="text-xs leading-5 text-muted-foreground">{problem.fix}</dd>
        </div>
      ))}
    </dl>
  );
}

function Bullets({ items }: { items: readonly string[] }) {
  return (
    <ul className="list-disc space-y-1.5 pl-8 pr-4 text-[13px] leading-5 text-foreground marker:text-muted-foreground">
      {items.map((item) => (
        <li key={item}>{item}</li>
      ))}
    </ul>
  );
}

function Note({ children }: { children: ReactNode }) {
  return <p className="mx-4 rounded-md bg-muted/60 px-3 py-2 text-xs leading-5 text-muted-foreground">{children}</p>;
}

function Callout({ children }: { children: ReactNode }) {
  return (
    <p className="flex gap-2 rounded-md border border-warning-border bg-warning-soft px-3 py-2 text-xs leading-5 text-foreground">
      <TriangleAlert className="mt-0.5 size-3.5 shrink-0 text-warning" aria-hidden />
      <span>{children}</span>
    </p>
  );
}

function CopyValue({ label, caption, value }: { label: string; caption: string; value: string }) {
  return (
    <div className="flex items-start gap-2 rounded-md border border-border bg-card px-2 py-1.5">
      <div className="min-w-0 flex-1">
        <p className="text-[10.5px] font-medium uppercase tracking-wider text-muted-foreground">{caption}</p>
        {value ? (
          <code className="block break-all font-mono text-[12px] leading-5 text-foreground">{value}</code>
        ) : (
          <p className="text-xs italic leading-5 text-muted-foreground">Fill in the Service provider entity ID field.</p>
        )}
      </div>
      {value ? <CopyButton label={label} text={value} /> : null}
    </div>
  );
}

function CodeBlock({ code, label }: { code: string; label: string }) {
  return (
    <div className="relative mx-4">
      <pre className="max-h-96 overflow-auto rounded-md border border-border bg-muted/50 p-2.5 pr-10 font-mono text-[11.5px] leading-[1.5] text-foreground">
        {code}
      </pre>
      <CopyButton label={label} text={code} className="absolute right-1.5 top-1.5" />
    </div>
  );
}

function CopyButton({ label, text, className }: { label: string; text: string; className?: string }) {
  return (
    <button
      type="button"
      title={`Copy ${label}`}
      aria-label={`Copy ${label}`}
      onClick={() => void copyText(text, `Copied ${label}`)}
      className={cn(
        "flex size-7 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring [&_svg]:size-3.5",
        className,
      )}
    >
      <Copy aria-hidden />
    </button>
  );
}

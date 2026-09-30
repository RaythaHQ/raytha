import { adminApi, AUTH_SCHEME_TYPES, formatError, platformPermissions } from "@raytha/api";
import type { AuthenticationSchemeRequest, AuthSchemeType, EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  DangerZone,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  Textarea,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "@tanstack/react-router";
import { Copy, KeyRound } from "lucide-react";
import { useState, type FormEvent, type ReactNode } from "react";
import { ListBackLink } from "../components/list-back-link";
import { useDocumentTitle } from "../lib/document-title";
import { schemeDeveloperName, suggestedSpEntityId, type SsoSchemeType } from "./authentication-guide";
import { SsoGuidePanel, useSsoContext } from "./authentication-guide-panel";
import { CrudListPage } from "./crud-list";
import { copyText } from "./editors/clipboard";
import { entityFields, formatCell, isRecord, readBoolean, readString } from "./entity";

const BUILT_IN: ReadonlySet<AuthSchemeType> = new Set(["email_and_password", "magic_link"]);

export function AuthenticationPage() {
  return (
    <CrudListPage
      title="Authentication"
      description="Sign-in schemes for admins and users."
      queryKey={["auth-schemes"]}
      listKey="auth-schemes"
      noun="scheme"
      list={adminApi.authSchemes.list}
      createPermission={platformPermissions.systemSettings}
      columns={[
        {
          header: "Label",
          cell: (entity) => (
            <Link
              to="/settings/authentication/$id"
              params={{ id: entity.id }}
              className="text-primary hover:underline"
            >
              {readString(entityFields(entity), "label") || entity.id}
            </Link>
          ),
        },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        {
          header: "Type",
          cell: (entity) => schemeTypeLabel(readSchemeType(entityFields(entity).authenticationSchemeType)),
        },
        {
          header: "Admins",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isEnabledForAdmins") ? (
              <Badge variant="success">On</Badge>
            ) : (
              <Badge variant="secondary">Off</Badge>
            ),
        },
        {
          header: "Users",
          cell: (entity) =>
            readBoolean(entityFields(entity), "isEnabledForUsers") ? (
              <Badge variant="success">On</Badge>
            ) : (
              <Badge variant="secondary">Off</Badge>
            ),
        },
      ]}
      actions={<CreateSchemeMenu />}
    />
  );
}

export function NewAuthenticationPage() {
  const params = useParams({ strict: false });
  const schemeType = parseSchemeTypeParam(params.schemeType);
  useDocumentTitle(["New authentication scheme"]);
  const navigate = useNavigate();
  const [form, setForm] = useState<SchemeForm>(() => emptySchemeForm(schemeType ?? "jwt"));

  const mutation = useMutation({
    mutationFn: () => {
      if (!schemeType) {
        throw new Error("Choose jwt or saml.");
      }
      return adminApi.authSchemes.createScheme(toRequest(form, schemeType, true));
    },
    onSuccess: (created) => {
      toast.success("Authentication scheme created");
      void navigate({ to: "/settings/authentication/$id", params: { id: created.id } });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  if (!schemeType || schemeType === "email_and_password" || schemeType === "magic_link") {
    return (
      <div className="space-y-6">
        <PageHeader
          back={<ListBackLink to="/settings/authentication" listKey="auth-schemes" label="authentication" />}
          title="New authentication scheme"
        />
        <p className="text-sm text-muted-foreground">Create a jwt or saml scheme from the authentication list.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/settings/authentication" listKey="auth-schemes" label="authentication" />}
        title={`New ${schemeTypeLabel(schemeType)} scheme`}
      />
      <SchemeLayout form={form} schemeType={schemeType}>
        <Card>
          <CardContent className="pt-6">
            <SchemeFormFields
              form={form}
              setForm={setForm}
              schemeType={schemeType}
              isCreate
              onSubmit={() => mutation.mutate()}
              pending={mutation.isPending}
              submitLabel="Create"
            />
          </CardContent>
        </Card>
      </SchemeLayout>
    </div>
  );
}

function SchemeLayout({
  form,
  schemeType,
  children,
}: {
  form: SchemeForm;
  schemeType: AuthSchemeType;
  children: ReactNode;
}) {
  const sso = ssoType(schemeType);
  if (!sso) {
    return <div className="space-y-6">{children}</div>;
  }
  return (
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_27rem] xl:items-start">
      <div className="min-w-0 space-y-6">{children}</div>
      <SsoGuidePanel
        type={sso}
        developerName={form.developerName}
        spEntityId={form.samlIdpEntityId}
        enabled={{ users: form.isEnabledForUsers, admins: form.isEnabledForAdmins }}
      />
    </div>
  );
}

function ssoType(type: AuthSchemeType): SsoSchemeType | undefined {
  return type === "jwt" || type === "saml" ? type : undefined;
}

export function EditAuthenticationPage() {
  const params = useParams({ strict: false });
  const id = typeof params.id === "string" ? params.id : "";
  useDocumentTitle(["Edit authentication scheme"]);
  const query = useQuery({
    queryKey: ["auth-schemes", id],
    queryFn: () => adminApi.authSchemes.get(id),
    enabled: id.length > 0,
  });

  if (!id) {
    return (
      <div className="space-y-6">
        <PageHeader title="Edit authentication scheme" />
        <p className="text-sm text-muted-foreground">Missing scheme id.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        back={<ListBackLink to="/settings/authentication" listKey="auth-schemes" label="authentication" />}
        title="Edit authentication scheme"
      />
      <QueryGate query={query}>{(scheme) => <SchemeEditForm scheme={scheme} />}</QueryGate>
    </div>
  );
}

function SchemeEditForm({ scheme }: { scheme: EntityRef }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const fields = entityFields(scheme);
  const schemeType = readSchemeType(fields.authenticationSchemeType) ?? "email_and_password";
  const [form, setForm] = useState<SchemeForm>(() => formFromEntity(fields, schemeType));
  const canDelete = !BUILT_IN.has(schemeType);

  const mutation = useMutation({
    mutationFn: () => adminApi.authSchemes.updateScheme(scheme.id, toRequest(form, schemeType, false)),
    onSuccess: () => {
      toast.success("Authentication scheme saved");
      void queryClient.invalidateQueries({ queryKey: ["auth-schemes"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const remove = useMutation({
    mutationFn: () => adminApi.authSchemes.remove(scheme.id),
    onSuccess: () => {
      toast.success("Authentication scheme deleted");
      void queryClient.invalidateQueries({ queryKey: ["auth-schemes"] });
      void navigate({ to: "/settings/authentication" });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <SchemeLayout form={form} schemeType={schemeType}>
      <Card>
        <CardContent className="pt-6">
          <SchemeFormFields
            form={form}
            setForm={setForm}
            schemeType={schemeType}
            isCreate={false}
            onSubmit={() => mutation.mutate()}
            pending={mutation.isPending}
            submitLabel="Save"
          />
        </CardContent>
      </Card>
      {canDelete ? (
        <DangerZone
          description="Delete this scheme. This cannot be undone."
          actionLabel="Delete scheme"
          confirmTitle="Delete authentication scheme?"
          confirmBody="This cannot be undone."
          onConfirm={() => remove.mutate()}
          pending={remove.isPending}
        />
      ) : null}
    </SchemeLayout>
  );
}

type SchemeForm = {
  label: string;
  developerName: string;
  loginButtonText: string;
  signInUrl: string;
  signOutUrl: string;
  isEnabledForUsers: boolean;
  isEnabledForAdmins: boolean;
  jwtSecretKey: string;
  jwtUseHighSecurity: boolean;
  samlCertificate: string;
  samlIdpEntityId: string;
  magicLinkExpiresInSeconds: string;
  bruteForceProtectionMaxFailedAttempts: string;
  bruteForceProtectionWindowInSeconds: string;
};

function emptySchemeForm(schemeType: AuthSchemeType): SchemeForm {
  return {
    label: schemeTypeLabel(schemeType),
    developerName: schemeType,
    loginButtonText: "",
    signInUrl: "",
    signOutUrl: "",
    isEnabledForUsers: false,
    isEnabledForAdmins: false,
    jwtSecretKey: "",
    jwtUseHighSecurity: false,
    samlCertificate: "",
    samlIdpEntityId: "",
    magicLinkExpiresInSeconds: "900",
    bruteForceProtectionMaxFailedAttempts: "5",
    bruteForceProtectionWindowInSeconds: "900",
  };
}

function formFromEntity(fields: Record<string, unknown>, schemeType: AuthSchemeType): SchemeForm {
  const defaults = emptySchemeForm(schemeType);
  return {
    label: readString(fields, "label") || defaults.label,
    developerName: readString(fields, "developerName") || defaults.developerName,
    loginButtonText: readString(fields, "loginButtonText"),
    signInUrl: readString(fields, "signInUrl"),
    signOutUrl: readString(fields, "signOutUrl"),
    isEnabledForUsers: readBoolean(fields, "isEnabledForUsers"),
    isEnabledForAdmins: readBoolean(fields, "isEnabledForAdmins"),
    jwtSecretKey: readString(fields, "jwtSecretKey"),
    jwtUseHighSecurity: readBoolean(fields, "jwtUseHighSecurity"),
    samlCertificate: readString(fields, "samlCertificate"),
    samlIdpEntityId: readString(fields, "samlIdpEntityId"),
    magicLinkExpiresInSeconds: String(readNumber(fields, "magicLinkExpiresInSeconds") ?? 900),
    bruteForceProtectionMaxFailedAttempts: String(
      readNumber(fields, "bruteForceProtectionMaxFailedAttempts") ?? 5,
    ),
    bruteForceProtectionWindowInSeconds: String(
      readNumber(fields, "bruteForceProtectionWindowInSeconds") ?? 900,
    ),
  };
}

function toRequest(form: SchemeForm, schemeType: AuthSchemeType, isCreate: boolean): AuthenticationSchemeRequest {
  return {
    label: form.label,
    developerName: isCreate ? form.developerName : undefined,
    authenticationSchemeType: schemeType,
    loginButtonText: form.loginButtonText,
    signInUrl: form.signInUrl,
    signOutUrl: form.signOutUrl,
    isEnabledForUsers: form.isEnabledForUsers,
    isEnabledForAdmins: form.isEnabledForAdmins,
    jwtSecretKey: form.jwtSecretKey,
    jwtUseHighSecurity: form.jwtUseHighSecurity,
    samlCertificate: form.samlCertificate.trim(),
    samlIdpEntityId: form.samlIdpEntityId,
    magicLinkExpiresInSeconds: Number(form.magicLinkExpiresInSeconds) || 0,
    bruteForceProtectionMaxFailedAttempts: Number(form.bruteForceProtectionMaxFailedAttempts) || 0,
    bruteForceProtectionWindowInSeconds: Number(form.bruteForceProtectionWindowInSeconds) || 0,
  };
}

function SchemeFormFields({
  form,
  setForm,
  schemeType,
  isCreate,
  onSubmit,
  pending,
  submitLabel,
}: {
  form: SchemeForm;
  setForm: (next: SchemeForm) => void;
  schemeType: AuthSchemeType;
  isCreate: boolean;
  onSubmit: () => void;
  pending: boolean;
  submitLabel: string;
}) {
  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    onSubmit();
  };
  const sso = ssoType(schemeType);
  const { context } = useSsoContext(form.developerName);
  const storedDeveloperName = schemeDeveloperName(form.developerName);
  const suggestedEntityId = suggestedSpEntityId(context);

  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      <FormField
        label="Label"
        required
        htmlFor="scheme-label"
        hint={sso ? "Names the scheme in this list and on the admin sign-in page, as Continue with …" : undefined}
      >
        {(control) => (
          <Input
            {...control}
            value={form.label}
            onChange={(event) => setForm({ ...form, label: event.target.value })}
          />
        )}
      </FormField>
      {isCreate ? (
        <FormField
          label="Developer name"
          required
          htmlFor="scheme-developer"
          hint={
            <>
              Part of every sign-in URL, and it can't be changed later. Lowercase letters, numbers, and underscores.
              {storedDeveloperName && storedDeveloperName !== form.developerName ? (
                <>
                  {" "}
                  Saved as <code className="font-mono text-foreground">{storedDeveloperName}</code>.
                </>
              ) : null}
            </>
          }
        >
          {(control) => (
            <Input
              {...control}
              value={form.developerName}
              onChange={(event) => setForm({ ...form, developerName: event.target.value })}
            />
          )}
        </FormField>
      ) : (
        <div className="flex flex-col gap-1">
          <p className="text-sm font-medium">Developer name</p>
          <code className="font-mono text-sm text-muted-foreground">{form.developerName}</code>
        </div>
      )}
      <FormField
        label="Login button text"
        required
        htmlFor="scheme-button"
        hint={sso ? "The label of this scheme's button on the public login page." : undefined}
      >
        {(control) => (
          <Input
            {...control}
            value={form.loginButtonText}
            onChange={(event) => setForm({ ...form, loginButtonText: event.target.value })}
          />
        )}
      </FormField>
      {sso ? (
        <>
          <FormField label="Sign in URL" required htmlFor="scheme-signin" hint={SIGN_IN_HINT[sso]}>
            {(control) => (
              <Input
                {...control}
                type="url"
                placeholder={sso === "jwt" ? "https://app.example.com/raytha-sign-in" : "https://idp.example.com/sso/saml"}
                value={form.signInUrl}
                onChange={(event) => setForm({ ...form, signInUrl: event.target.value })}
              />
            )}
          </FormField>
          <FormField
            label="Sign out URL"
            htmlFor="scheme-signout"
            hint="Optional. Raytha never calls it: signing out of Raytha ends only the Raytha session. Site templates can read it from CurrentOrganization.AuthenticationSchemes to link to your provider's sign-out page."
          >
            {(control) => (
              <Input
                {...control}
                type="url"
                value={form.signOutUrl}
                onChange={(event) => setForm({ ...form, signOutUrl: event.target.value })}
              />
            )}
          </FormField>
        </>
      ) : null}
      <CheckboxField
        id="scheme-users"
        label="Enabled for users"
        hint={
          sso
            ? "Public users may sign in with this scheme, and its button shows on the public login page."
            : undefined
        }
        checked={form.isEnabledForUsers}
        onCheckedChange={(checked) => setForm({ ...form, isEnabledForUsers: checked })}
      />
      <CheckboxField
        id="scheme-admins"
        label="Enabled for admins"
        hint={
          sso
            ? "Admin accounts may sign in with this scheme, and the admin sign-in page offers it. It never grants admin access: the admin must already exist in Raytha."
            : undefined
        }
        checked={form.isEnabledForAdmins}
        onCheckedChange={(checked) => setForm({ ...form, isEnabledForAdmins: checked })}
      />
      {schemeType === "jwt" ? (
        <>
          <FormField
            label="JWT secret"
            required
            htmlFor="scheme-jwt-secret"
            hint="The shared HMAC secret your app signs tokens with (HS256). Use at least 32 random ASCII characters: Raytha pads shorter secrets with NUL bytes to 32."
          >
            {(control) => (
              <div className="flex gap-2">
                <Input
                  {...control}
                  spellCheck={false}
                  autoComplete="off"
                  className="font-mono"
                  value={form.jwtSecretKey}
                  onChange={(event) => setForm({ ...form, jwtSecretKey: event.target.value })}
                />
                <Button
                  type="button"
                  variant="outline"
                  className="shrink-0"
                  onClick={() => setForm({ ...form, jwtSecretKey: randomSecret() })}
                >
                  <KeyRound aria-hidden />
                  Generate
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="shrink-0"
                  aria-label="Copy JWT secret"
                  title="Copy JWT secret"
                  disabled={!form.jwtSecretKey}
                  onClick={() => void copyText(form.jwtSecretKey, "Copied JWT secret")}
                >
                  <Copy aria-hidden />
                </Button>
              </div>
            )}
          </FormField>
          <CheckboxField
            id="scheme-jwt-high"
            label="JWT high security"
            hint="Require a jti claim in every token and accept each jti once, so a leaked sign-in link can't be replayed. Recommended."
            checked={form.jwtUseHighSecurity}
            onCheckedChange={(checked) => setForm({ ...form, jwtUseHighSecurity: checked })}
          />
        </>
      ) : null}
      {schemeType === "saml" ? (
        <>
          <FormField
            label="SAML certificate"
            required
            htmlFor="scheme-saml-cert"
            hint="The identity provider's X.509 signing certificate as PEM, including the BEGIN and END lines. Raytha verifies every response against it."
          >
            {(control) => (
              <Textarea
                {...control}
                rows={20}
                spellCheck={false}
                autoComplete="off"
                className="font-mono text-xs leading-5"
                placeholder={"-----BEGIN CERTIFICATE-----\n…\n-----END CERTIFICATE-----"}
                value={form.samlCertificate}
                onChange={(event) => setForm({ ...form, samlCertificate: event.target.value })}
              />
            )}
          </FormField>
          <FormField
            label="Service provider entity ID (sent as Issuer)"
            required={isCreate}
            htmlFor="scheme-saml-idp"
            hint={
              <>
                Raytha's own identifier, not your IdP's. Raytha sends it as the Issuer of every SAML request, so enter
                the exact value you gave the IdP as the app's Entity ID, Audience URI, or Identifier. Any unique URI
                works.
                {form.samlIdpEntityId.trim() === "" && context.developerName ? (
                  <>
                    {" "}
                    <button
                      type="button"
                      className="font-medium text-primary hover:underline focus-visible:outline-2 focus-visible:outline-ring"
                      onClick={() => setForm({ ...form, samlIdpEntityId: suggestedEntityId })}
                    >
                      Use {suggestedEntityId}
                    </button>
                  </>
                ) : null}
              </>
            }
          >
            {(control) => (
              <Input
                {...control}
                spellCheck={false}
                value={form.samlIdpEntityId}
                onChange={(event) => setForm({ ...form, samlIdpEntityId: event.target.value })}
              />
            )}
          </FormField>
        </>
      ) : null}
      {schemeType === "magic_link" ? (
        <FormField label="Magic link expiry (seconds)" htmlFor="scheme-magic-expiry">
          {(control) => (
            <Input
              {...control}
              type="number"
              value={form.magicLinkExpiresInSeconds}
              onChange={(event) => setForm({ ...form, magicLinkExpiresInSeconds: event.target.value })}
            />
          )}
        </FormField>
      ) : null}
      {schemeType === "email_and_password" || schemeType === "magic_link" ? (
        <>
          <FormField label="Brute force max attempts" htmlFor="scheme-bf-max">
            {(control) => (
              <Input
                {...control}
                type="number"
                value={form.bruteForceProtectionMaxFailedAttempts}
                onChange={(event) =>
                  setForm({ ...form, bruteForceProtectionMaxFailedAttempts: event.target.value })
                }
              />
            )}
          </FormField>
          <FormField label="Brute force window (seconds)" htmlFor="scheme-bf-window">
            {(control) => (
              <Input
                {...control}
                type="number"
                value={form.bruteForceProtectionWindowInSeconds}
                onChange={(event) =>
                  setForm({ ...form, bruteForceProtectionWindowInSeconds: event.target.value })
                }
              />
            )}
          </FormField>
        </>
      ) : null}
      <Button type="submit" loading={pending}>
        {submitLabel}
      </Button>
    </form>
  );
}

const SIGN_IN_HINT: Record<SsoSchemeType, string> = {
  jwt: "Your app's sign-in page. Raytha sends people here with a raytha_callback_url query parameter and expects them back at that URL with token=<jwt> added.",
  saml: "Your IdP's SAML single sign-on URL (HTTP-Redirect). Raytha adds SAMLRequest, and RelayState when there is a return path.",
};

const SECRET_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

function randomSecret(): string {
  return Array.from(crypto.getRandomValues(new Uint8Array(48)), (byte) => SECRET_ALPHABET[byte % 64]).join("");
}

function CheckboxField({
  id,
  label,
  hint,
  checked,
  onCheckedChange,
}: {
  id: string;
  label: string;
  hint?: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
}) {
  const hintId = hint ? `${id}-hint` : undefined;
  return (
    <div className="flex items-start gap-2.5">
      <Checkbox
        id={id}
        checked={checked}
        onCheckedChange={onCheckedChange}
        aria-describedby={hintId}
        className="mt-0.5"
      />
      <div className="space-y-0.5">
        <Label htmlFor={id}>{label}</Label>
        {hint ? (
          <p id={hintId} className="text-xs leading-5 text-muted-foreground">
            {hint}
          </p>
        ) : null}
      </div>
    </div>
  );
}

function CreateSchemeMenu() {
  const navigate = useNavigate();
  return (
    <DropdownMenu>
      <DropdownMenuTrigger>
        <Button type="button">New scheme</Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent>
        <DropdownMenuItem
          onSelect={() => void navigate({ to: "/settings/authentication/new/$schemeType", params: { schemeType: "jwt" } })}
        >
          JWT
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={() => void navigate({ to: "/settings/authentication/new/$schemeType", params: { schemeType: "saml" } })}
        >
          SAML
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function readSchemeType(value: unknown): AuthSchemeType | undefined {
  if (typeof value === "string") {
    return parseSchemeTypeParam(value);
  }
  if (isRecord(value)) {
    return parseSchemeTypeParam(readString(value, "developerName"));
  }
  return undefined;
}

function parseSchemeTypeParam(value: unknown): AuthSchemeType | undefined {
  if (typeof value !== "string") {
    return undefined;
  }
  for (const type of AUTH_SCHEME_TYPES) {
    if (type === value) {
      return type;
    }
  }
  return undefined;
}

function schemeTypeLabel(type: AuthSchemeType | undefined): string {
  switch (type) {
    case "email_and_password":
      return "Email and password";
    case "magic_link":
      return "Magic link";
    case "jwt":
      return "JWT";
    case "saml":
      return "SAML";
    default:
      return formatCell(type) || "—";
  }
}

function readNumber(record: Record<string, unknown>, key: string): number | undefined {
  const value = record[key];
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === "string" && value.trim() !== "") {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }
  return undefined;
}

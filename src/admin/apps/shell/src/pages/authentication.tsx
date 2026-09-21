import { adminApi, AUTH_SCHEME_TYPES, formatError, platformPermissions } from "@raytha/api";
import type { AuthenticationSchemeRequest, AuthSchemeType, EntityRef } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  Checkbox,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
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
      remove={adminApi.authSchemes.remove}
      canDelete={(entity) => {
        const type = readSchemeType(entityFields(entity).authenticationSchemeType);
        return type === undefined || !BUILT_IN.has(type);
      }}
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
        <PageHeader title="New authentication scheme" />
        <p className="text-sm text-muted-foreground">Create a jwt or saml scheme from the authentication list.</p>
        <ListBackLink to="/settings/authentication" listKey="auth-schemes" label="authentication" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader title={`New ${schemeTypeLabel(schemeType)} scheme`} />
      <ListBackLink to="/settings/authentication" listKey="auth-schemes" label="authentication" />
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
    </div>
  );
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
      <PageHeader title="Edit authentication scheme" />
      <ListBackLink to="/settings/authentication" listKey="auth-schemes" label="authentication" />
      <QueryGate query={query}>{(scheme) => <SchemeEditForm scheme={scheme} />}</QueryGate>
    </div>
  );
}

function SchemeEditForm({ scheme }: { scheme: EntityRef }) {
  const queryClient = useQueryClient();
  const fields = entityFields(scheme);
  const schemeType = readSchemeType(fields.authenticationSchemeType) ?? "email_and_password";
  const [form, setForm] = useState<SchemeForm>(() => formFromEntity(fields, schemeType));

  const mutation = useMutation({
    mutationFn: () => adminApi.authSchemes.updateScheme(scheme.id, toRequest(form, schemeType, false)),
    onSuccess: () => {
      toast.success("Authentication scheme saved");
      void queryClient.invalidateQueries({ queryKey: ["auth-schemes"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
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
    samlCertificate: form.samlCertificate,
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

  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      <FormField label="Label" required htmlFor="scheme-label">
        {(control) => (
          <Input
            {...control}
            value={form.label}
            onChange={(event) => setForm({ ...form, label: event.target.value })}
          />
        )}
      </FormField>
      {isCreate ? (
        <FormField label="Developer name" required htmlFor="scheme-developer">
          {(control) => (
            <Input
              {...control}
              value={form.developerName}
              onChange={(event) => setForm({ ...form, developerName: event.target.value })}
            />
          )}
        </FormField>
      ) : (
        <p className="text-sm text-muted-foreground">Developer name {form.developerName}</p>
      )}
      <FormField label="Login button text" htmlFor="scheme-button">
        {(control) => (
          <Input
            {...control}
            value={form.loginButtonText}
            onChange={(event) => setForm({ ...form, loginButtonText: event.target.value })}
          />
        )}
      </FormField>
      <FormField label="Sign in URL" htmlFor="scheme-signin">
        {(control) => (
          <Input
            {...control}
            value={form.signInUrl}
            onChange={(event) => setForm({ ...form, signInUrl: event.target.value })}
          />
        )}
      </FormField>
      <FormField label="Sign out URL" htmlFor="scheme-signout">
        {(control) => (
          <Input
            {...control}
            value={form.signOutUrl}
            onChange={(event) => setForm({ ...form, signOutUrl: event.target.value })}
          />
        )}
      </FormField>
      <div className="flex items-center gap-2">
        <Checkbox
          id="scheme-users"
          checked={form.isEnabledForUsers}
          onCheckedChange={(checked) => setForm({ ...form, isEnabledForUsers: checked })}
        />
        <Label htmlFor="scheme-users">Enabled for users</Label>
      </div>
      <div className="flex items-center gap-2">
        <Checkbox
          id="scheme-admins"
          checked={form.isEnabledForAdmins}
          onCheckedChange={(checked) => setForm({ ...form, isEnabledForAdmins: checked })}
        />
        <Label htmlFor="scheme-admins">Enabled for admins</Label>
      </div>
      {schemeType === "jwt" ? (
        <>
          <FormField label="JWT secret" htmlFor="scheme-jwt-secret">
            {(control) => (
              <Input
                {...control}
                value={form.jwtSecretKey}
                onChange={(event) => setForm({ ...form, jwtSecretKey: event.target.value })}
              />
            )}
          </FormField>
          <div className="flex items-center gap-2">
            <Checkbox
              id="scheme-jwt-high"
              checked={form.jwtUseHighSecurity}
              onCheckedChange={(checked) => setForm({ ...form, jwtUseHighSecurity: checked })}
            />
            <Label htmlFor="scheme-jwt-high">JWT high security</Label>
          </div>
        </>
      ) : null}
      {schemeType === "saml" ? (
        <>
          <FormField label="SAML certificate" htmlFor="scheme-saml-cert">
            {(control) => (
              <Input
                {...control}
                value={form.samlCertificate}
                onChange={(event) => setForm({ ...form, samlCertificate: event.target.value })}
              />
            )}
          </FormField>
          <FormField label="SAML IdP entity id" htmlFor="scheme-saml-idp">
            {(control) => (
              <Input
                {...control}
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

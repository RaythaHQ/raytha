import { adminApi, formatError, patchSession } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { RichTextEditor } from "../components/rich-text-editor";
import { CrudListPage } from "./crud-list";
import { entityFields, formatCell, humanizeKey, isRecord, jsonBoolean, jsonNumber, jsonString, readBoolean, readString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export function MaintenancePage() {
  useDocumentTitle(["Maintenance"]);
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["maintenance"],
    queryFn: () => adminApi.maintenance.snapshot(),
  });
  const [html, setHtml] = useState("<p>Try the editor.</p>");

  const clearCache = useMutation({
    mutationFn: () => adminApi.maintenance.clearCache(),
    onSuccess: () => {
      toast.success("Cache cleared");
      void queryClient.invalidateQueries({ queryKey: ["maintenance"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  return (
    <div className="space-y-6">
      <PageHeader
        title="Maintenance"
        description="Platform snapshot and editor playground."
        actions={
          <Button type="button" variant="outline" loading={clearCache.isPending} onClick={() => clearCache.mutate()}>
            Clear cache
          </Button>
        }
      />
      <QueryGate query={query}>{(data) => <SnapshotCards data={data} />}</QueryGate>
      <Card>
        <CardHeader>
          <CardTitle>Rich text editor</CardTitle>
          <CardDescription>Playground for the TipTap editor used on content forms.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <RichTextEditor content={html} onHtmlChange={setHtml} ariaLabel="Rich text playground" />
        </CardContent>
      </Card>
    </div>
  );
}

function SnapshotCards({ data }: { data: JsonObject }) {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      {Object.entries(data).map(([key, value]) => (
        <Card key={key}>
          <CardHeader>
            <CardTitle>{humanizeKey(key)}</CardTitle>
          </CardHeader>
          <CardContent>
            {isRecord(value) ? (
              <dl className="space-y-2 text-sm">
                {Object.entries(value).map(([childKey, childValue]) => (
                  <div key={childKey} className="flex justify-between gap-4">
                    <dt className="text-muted-foreground">{humanizeKey(childKey)}</dt>
                    <dd className="text-right font-medium">
                      {Array.isArray(childValue)
                        ? childValue.length
                        : isRecord(childValue)
                          ? formatCell(childValue)
                          : formatCell(childValue) || "—"}
                    </dd>
                  </div>
                ))}
              </dl>
            ) : Array.isArray(value) ? (
              <ul className="space-y-2 text-sm">
                {value.map((item, index) => (
                  <li key={index} className="rounded-lg border border-border px-3 py-2">
                    {isRecord(item)
                      ? Object.entries(item)
                          .map(([childKey, childValue]) => `${humanizeKey(childKey)}: ${formatCell(childValue)}`)
                          .join(" · ")
                      : formatCell(item)}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="font-display text-2xl font-semibold">{formatCell(value) || "—"}</p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

export function ConfigurationPage() {
  useDocumentTitle(["Configuration"]);
  const query = useQuery({
    queryKey: ["configuration"],
    queryFn: () => adminApi.configuration.get(),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Configuration" description="Organization settings." />
      <QueryGate query={query}>{(data) => <ConfigurationForm data={data} />}</QueryGate>
    </div>
  );
}

function ConfigurationForm({ data }: { data: JsonObject }) {
  const queryClient = useQueryClient();
  const [organizationName, setOrganizationName] = useState(jsonString(data, "organizationName"));
  const [websiteUrl, setWebsiteUrl] = useState(jsonString(data, "websiteUrl"));
  const [timeZone, setTimeZone] = useState(jsonString(data, "timeZone"));
  const [dateFormat, setDateFormat] = useState(jsonString(data, "dateFormat"));
  const [smtpDefaultFromAddress, setSmtpDefaultFromAddress] = useState(jsonString(data, "smtpDefaultFromAddress"));
  const [smtpDefaultFromName, setSmtpDefaultFromName] = useState(jsonString(data, "smtpDefaultFromName"));

  const mutation = useMutation({
    mutationFn: () =>
      adminApi.configuration.update({
        organizationName,
        websiteUrl,
        timeZone,
        dateFormat,
        smtpDefaultFromAddress,
        smtpDefaultFromName,
      }),
    onSuccess: () => {
      toast.success("Configuration saved");
      void queryClient.invalidateQueries({ queryKey: ["configuration"] });
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
          <FormField label="Organization name" required htmlFor="org-name">
            {(control) => (
              <Input {...control} value={organizationName} onChange={(event) => setOrganizationName(event.target.value)} />
            )}
          </FormField>
          <FormField label="Website URL" required htmlFor="website-url">
            {(control) => (
              <Input {...control} type="url" value={websiteUrl} onChange={(event) => setWebsiteUrl(event.target.value)} />
            )}
          </FormField>
          <FormField label="Time zone" required htmlFor="time-zone">
            {(control) => <Input {...control} value={timeZone} onChange={(event) => setTimeZone(event.target.value)} />}
          </FormField>
          <FormField label="Date format" required htmlFor="date-format">
            {(control) => <Input {...control} value={dateFormat} onChange={(event) => setDateFormat(event.target.value)} />}
          </FormField>
          <FormField label="Default from address" required htmlFor="from-address">
            {(control) => (
              <Input
                {...control}
                type="email"
                value={smtpDefaultFromAddress}
                onChange={(event) => setSmtpDefaultFromAddress(event.target.value)}
              />
            )}
          </FormField>
          <FormField label="Default from name" required htmlFor="from-name">
            {(control) => (
              <Input
                {...control}
                value={smtpDefaultFromName}
                onChange={(event) => setSmtpDefaultFromName(event.target.value)}
              />
            )}
          </FormField>
          <Button type="submit" loading={mutation.isPending}>
            Save
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

export function SmtpPage() {
  useDocumentTitle(["SMTP"]);
  const query = useQuery({
    queryKey: ["smtp"],
    queryFn: () => adminApi.smtp.get(),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="SMTP" description="Outbound email settings." />
      <QueryGate query={query}>{(data) => <SmtpForm data={data} />}</QueryGate>
    </div>
  );
}

function SmtpForm({ data }: { data: JsonObject }) {
  const queryClient = useQueryClient();
  const [smtpOverrideSystem, setSmtpOverrideSystem] = useState(jsonBoolean(data, "smtpOverrideSystem"));
  const [smtpHost, setSmtpHost] = useState(jsonString(data, "smtpHost"));
  const [smtpPort, setSmtpPort] = useState(String(jsonNumber(data, "smtpPort") ?? ""));
  const [smtpUsername, setSmtpUsername] = useState(jsonString(data, "smtpUsername"));
  const [smtpPassword, setSmtpPassword] = useState("");
  const hasPassword = jsonBoolean(data, "hasSmtpPassword");

  const save = useMutation({
    mutationFn: () =>
      adminApi.smtp.update({
        smtpOverrideSystem,
        smtpHost,
        smtpPort: smtpPort ? Number(smtpPort) : undefined,
        smtpUsername,
        smtpPassword,
      }),
    onSuccess: () => {
      toast.success("SMTP settings saved");
      setSmtpPassword("");
      void queryClient.invalidateQueries({ queryKey: ["smtp"] });
    },
    onError: (error) => toast.error(formatError(error)),
  });

  const sendTest = useMutation({
    mutationFn: () => adminApi.smtp.sendTest(),
    onSuccess: () => toast.success("Test email sent"),
    onError: (error) => toast.error(formatError(error)),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    save.mutate();
  };

  return (
    <Card>
      <CardContent className="pt-6">
        <form className="space-y-4" onSubmit={handleSubmit}>
          {jsonBoolean(data, "missingSmtpEnvironmentVariables") && (
            <p className="rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-sm">
              Server SMTP environment variables are missing. Override is required.
            </p>
          )}
          <div className="flex items-center gap-2">
            <Checkbox id="smtp-override" checked={smtpOverrideSystem} onCheckedChange={setSmtpOverrideSystem} />
            <Label htmlFor="smtp-override">Override system SMTP</Label>
          </div>
          <FormField label="Host" htmlFor="smtp-host">
            {(control) => <Input {...control} value={smtpHost} onChange={(event) => setSmtpHost(event.target.value)} />}
          </FormField>
          <FormField label="Port" htmlFor="smtp-port">
            {(control) => <Input {...control} type="number" value={smtpPort} onChange={(event) => setSmtpPort(event.target.value)} />}
          </FormField>
          <FormField label="Username" htmlFor="smtp-username">
            {(control) => (
              <Input {...control} value={smtpUsername} onChange={(event) => setSmtpUsername(event.target.value)} />
            )}
          </FormField>
          <FormField
            label="Password"
            htmlFor="smtp-password"
            hint={hasPassword ? "A password is already stored. Leave blank unless you are changing it." : undefined}
          >
            {(control) => (
              <Input
                {...control}
                type="password"
                autoComplete="new-password"
                value={smtpPassword}
                onChange={(event) => setSmtpPassword(event.target.value)}
              />
            )}
          </FormField>
          <div className="flex gap-2">
            <Button type="submit" loading={save.isPending}>
              Save
            </Button>
            <Button type="button" variant="outline" loading={sendTest.isPending} onClick={() => sendTest.mutate()}>
              Send test
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

export function AuthenticationPage() {
  return (
    <CrudListPage
      title="Authentication"
      description="Sign-in schemes for admins and users."
      queryKey={["auth-schemes"]}
      noun="scheme"
      list={adminApi.authSchemes.list}
      columns={[
        { header: "Label", cell: (entity) => readString(entityFields(entity), "label") || entity.id },
        { header: "Developer name", cell: (entity) => readString(entityFields(entity), "developerName") },
        { header: "Type", cell: (entity) => formatCell(entityFields(entity).authenticationSchemeType) || "—" },
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
    />
  );
}

export function ProfilePage() {
  useDocumentTitle(["My profile"]);
  const query = useQuery({
    queryKey: ["profile"],
    queryFn: () => adminApi.profile.get(),
  });

  return (
    <div className="space-y-6">
      <PageHeader title="My profile" />
      <QueryGate query={query}>
        {(me) => <ProfileTabs firstName={me.firstName} lastName={me.lastName} />}
      </QueryGate>
    </div>
  );
}

function ProfileTabs({ firstName, lastName }: { firstName: string; lastName: string }) {
  const [tab, setTab] = useState("name");
  return (
    <Tabs value={tab} onValueChange={setTab}>
      <TabsList>
        <TabsTrigger value="name">Name</TabsTrigger>
        <TabsTrigger value="password">Password</TabsTrigger>
      </TabsList>
      <TabsContent value="name" className="mt-4">
        <ProfileNameForm firstName={firstName} lastName={lastName} />
      </TabsContent>
      <TabsContent value="password" className="mt-4">
        <ChangePasswordForm />
      </TabsContent>
    </Tabs>
  );
}

function ProfileNameForm({ firstName: initialFirst, lastName: initialLast }: { firstName: string; lastName: string }) {
  const queryClient = useQueryClient();
  const [firstName, setFirstName] = useState(initialFirst);
  const [lastName, setLastName] = useState(initialLast);

  const mutation = useMutation({
    mutationFn: () => adminApi.profile.update({ firstName, lastName }),
    onSuccess: () => {
      patchSession({ firstName, lastName, fullName: `${firstName} ${lastName}`.trim() });
      toast.success("Profile updated");
      void queryClient.invalidateQueries({ queryKey: ["profile"] });
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
          <FormField label="First name" required htmlFor="profile-first">
            {(control) => <Input {...control} value={firstName} onChange={(event) => setFirstName(event.target.value)} />}
          </FormField>
          <FormField label="Last name" required htmlFor="profile-last">
            {(control) => <Input {...control} value={lastName} onChange={(event) => setLastName(event.target.value)} />}
          </FormField>
          <Button type="submit" loading={mutation.isPending}>
            Save
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function ChangePasswordForm() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");

  const mutation = useMutation({
    mutationFn: () => adminApi.profile.changePassword({ currentPassword, newPassword }),
    onSuccess: () => {
      toast.success("Password changed");
      setCurrentPassword("");
      setNewPassword("");
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
          <FormField label="Current password" required htmlFor="current-password">
            {(control) => (
              <Input
                {...control}
                type="password"
                autoComplete="current-password"
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
              />
            )}
          </FormField>
          <FormField label="New password" required htmlFor="new-password">
            {(control) => (
              <Input
                {...control}
                type="password"
                autoComplete="new-password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
              />
            )}
          </FormField>
          <Button type="submit" loading={mutation.isPending}>
            Change password
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

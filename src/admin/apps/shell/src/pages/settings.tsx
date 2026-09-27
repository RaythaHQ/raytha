import { adminApi, formatError, patchSession } from "@raytha/api";
import type { JsonObject } from "@raytha/api";
import {
  Button,
  Card,
  CardContent,
  Checkbox,
  FormField,
  Input,
  Label,
  PageHeader,
  QueryGate,
  Select,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
  toast,
} from "@raytha/ui";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Navigate } from "@tanstack/react-router";
import { useState, type FormEvent } from "react";
import { jsonBoolean, jsonNumber, jsonString } from "./entity";
import { useDocumentTitle } from "../lib/document-title";

export { MaintenancePage } from "./maintenance";

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
  const optionsQuery = useQuery({
    queryKey: ["configuration-options"],
    queryFn: () => adminApi.configuration.options(),
  });
  const smtpQuery = useQuery({
    queryKey: ["smtp"],
    queryFn: () => adminApi.smtp.get(),
  });
  const [organizationName, setOrganizationName] = useState(jsonString(data, "organizationName"));
  const [websiteUrl, setWebsiteUrl] = useState(jsonString(data, "websiteUrl"));
  const [timeZone, setTimeZone] = useState(jsonString(data, "timeZone"));
  const [dateFormat, setDateFormat] = useState(jsonString(data, "dateFormat"));
  const [smtpDefaultFromAddress, setSmtpDefaultFromAddress] = useState(jsonString(data, "smtpDefaultFromAddress"));
  const [smtpDefaultFromName, setSmtpDefaultFromName] = useState(jsonString(data, "smtpDefaultFromName"));

  const smtp = smtpQuery.data ?? {};
  const [smtpOverrideSystem, setSmtpOverrideSystem] = useState(jsonBoolean(smtp, "smtpOverrideSystem"));
  const [smtpHost, setSmtpHost] = useState(jsonString(smtp, "smtpHost"));
  const [smtpPort, setSmtpPort] = useState(String(jsonNumber(smtp, "smtpPort") ?? ""));
  const [smtpUsername, setSmtpUsername] = useState(jsonString(smtp, "smtpUsername"));
  const [smtpPassword, setSmtpPassword] = useState("");
  const smtpReady = smtpQuery.isSuccess;
  const [smtpHydrated, setSmtpHydrated] = useState(false);
  if (smtpReady && !smtpHydrated) {
    setSmtpOverrideSystem(jsonBoolean(smtp, "smtpOverrideSystem"));
    setSmtpHost(jsonString(smtp, "smtpHost"));
    setSmtpPort(String(jsonNumber(smtp, "smtpPort") ?? ""));
    setSmtpUsername(jsonString(smtp, "smtpUsername"));
    setSmtpHydrated(true);
  }
  const hasPassword = jsonBoolean(smtp, "hasSmtpPassword");
  const smtpEnvMissing = jsonBoolean(smtp, "missingSmtpEnvironmentVariables");
  const smtpOverrideOn = smtpEnvMissing || smtpOverrideSystem;

  const mutation = useMutation({
    mutationFn: async () => {
      await adminApi.configuration.update({
        organizationName,
        websiteUrl,
        timeZone,
        dateFormat,
        smtpDefaultFromAddress,
        smtpDefaultFromName,
      });
      await adminApi.smtp.update({
        smtpOverrideSystem: smtpOverrideOn,
        smtpHost,
        smtpPort: smtpPort ? Number(smtpPort) : undefined,
        smtpUsername,
        smtpPassword,
      });
    },
    onSuccess: () => {
      toast.success("Configuration saved");
      setSmtpPassword("");
      void queryClient.invalidateQueries({ queryKey: ["configuration"] });
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
    mutation.mutate();
  };

  const timeZones = optionsQuery.data?.timeZones ?? [];
  const dateFormats = optionsQuery.data?.dateFormats ?? [];

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
            {(control) => (
              <Select {...control} value={timeZone} onChange={(event) => setTimeZone(event.target.value)}>
                <option value="">Select a time zone</option>
                {timeZones.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            )}
          </FormField>
          <FormField label="Date format" required htmlFor="date-format">
            {(control) => (
              <Select {...control} value={dateFormat} onChange={(event) => setDateFormat(event.target.value)}>
                <option value="">Select a date format</option>
                {dateFormats.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </Select>
            )}
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
          {smtpEnvMissing && (
            <p className="rounded-lg border border-warning/40 bg-warning/10 px-3 py-2 text-sm">
              Server SMTP environment variables are missing. Override is required.
            </p>
          )}
          <div className="flex items-center gap-2">
            <Checkbox
              id="smtp-override"
              checked={smtpOverrideOn}
              disabled={smtpEnvMissing}
              onCheckedChange={setSmtpOverrideSystem}
            />
            <Label htmlFor="smtp-override">Override system SMTP</Label>
          </div>
          {smtpOverrideOn ? (
            <>
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
            </>
          ) : null}
          <div className="flex gap-2">
            <Button type="submit" loading={mutation.isPending}>
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

export function SmtpPage() {
  return <Navigate to="/settings/configuration" replace />;
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

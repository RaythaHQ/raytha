import { createFirstAdmin, formatError, getAdminSetupStatus, isAuthenticated } from "@raytha/api";
import { Button, Input, Label } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link } from "@tanstack/react-router";
import { useDocumentTitle } from "../lib/document-title";

export function SetupPage() {
  useDocumentTitle(["Setup"]);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const setupQuery = useQuery({
    queryKey: ["admin-setup"],
    queryFn: getAdminSetupStatus,
  });

  if (isAuthenticated()) {
    window.location.replace("/raytha");
  }

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      await createFirstAdmin({ email, password, firstName, lastName });
      window.location.assign("/raytha");
    } catch (err) {
      setError(formatError(err));
    } finally {
      setPending(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6 rounded-xl border border-border bg-card p-8 shadow-card">
        <div className="flex flex-col items-center gap-3">
          <img src="/raytha/color-no-background.svg" alt="Raytha" className="h-10" />
          <h1 className="font-display text-2xl font-semibold">Create administrator</h1>
          <p className="text-sm text-muted-foreground">
            {setupQuery.data?.required === false
              ? "An administrator already exists. You can still use this form if setup is re-enabled."
              : "Set up the first admin account for this site."}
          </p>
        </div>
        <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          {error && (
            <p role="alert" className="rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="firstName">First name</Label>
              <Input id="firstName" value={firstName} onChange={(e) => setFirstName(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="lastName">Last name</Label>
              <Input id="lastName" value={lastName} onChange={(e) => setLastName(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="email">Email</Label>
            <Input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="password">Password</Label>
            <Input id="password" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </div>
          <Button type="submit" className="w-full" loading={pending}>
            Create administrator
          </Button>
        </form>
        <p className="text-center text-sm">
          <Link to="/login" className="text-muted-foreground hover:underline">
            Back to sign in
          </Link>
        </p>
      </div>
    </div>
  );
}

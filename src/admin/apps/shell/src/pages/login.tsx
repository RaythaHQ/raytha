import { completeMagicLink, formatError, getAdminSetupStatus, getEnabledAdminSchemes, isAuthenticated, login, requestForgotPassword, requestMagicLink } from "@raytha/api";
import { Button, Input, Label } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link } from "@tanstack/react-router";
import { useDocumentTitle } from "../lib/document-title";

export function LoginPage() {
  useDocumentTitle(["Sign in"]);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [mode, setMode] = useState<"password" | "magic" | "magic-code" | "forgot">("password");
  const isMagicMode = mode === "magic" || mode === "magic-code";

  const schemesQuery = useQuery({
    queryKey: ["admin-login-schemes"],
    queryFn: getEnabledAdminSchemes,
  });
  const setupQuery = useQuery({
    queryKey: ["admin-setup-status"],
    queryFn: getAdminSetupStatus,
  });

  if (isAuthenticated()) {
    window.location.replace("/raytha");
  }

  const finishSignIn = () => {
    window.location.assign("/raytha");
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setMessage(null);
    setPending(true);
    try {
      if (mode === "magic") {
        await requestMagicLink(email);
        setMode("magic-code");
        setMessage("Check your email for a sign-in code.");
        return;
      }
      if (mode === "magic-code") {
        await completeMagicLink(email, code);
        finishSignIn();
        return;
      }
      if (mode === "forgot") {
        await requestForgotPassword(email);
        setMessage("If that account exists, a reset email is on the way.");
        return;
      }
      await login(email, password);
      finishSignIn();
    } catch (err) {
      setError(formatError(err));
    } finally {
      setPending(false);
    }
  };

  const resendCode = async () => {
    setError(null);
    setMessage(null);
    setPending(true);
    try {
      await requestMagicLink(email);
      setMessage("We sent a new code.");
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
          <h1 className="font-display text-2xl font-semibold">Sign in</h1>
          <p className="text-sm text-muted-foreground">Administrator console</p>
        </div>
        <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          {error && (
            <p role="alert" className="rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}
          {message && (
            <p role="status" className="rounded-lg border border-success/40 bg-success/10 px-3 py-2 text-sm text-success">
              {message}
            </p>
          )}
          {mode === "magic-code" ? (
            <p className="text-sm text-muted-foreground">
              Code sent to <span className="font-medium text-foreground">{email}</span>
            </p>
          ) : (
            <div className="space-y-1.5">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>
          )}
          {mode === "password" && (
            <div className="space-y-1.5">
              <Label htmlFor="password">Password</Label>
              <Input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </div>
          )}
          {mode === "magic-code" && (
            <div className="space-y-1.5">
              <Label htmlFor="code">One-time code</Label>
              <Input
                id="code"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                required
              />
            </div>
          )}
          <Button type="submit" className="w-full" loading={pending}>
            {mode === "password" ? "Sign in" : mode === "magic" ? "Email me a code" : mode === "magic-code" ? "Verify code" : "Send reset email"}
          </Button>
        </form>
        <div className="flex flex-col gap-2 text-center text-sm">
          {mode === "magic-code" && (
            <button type="button" className="text-primary hover:underline" onClick={() => void resendCode()}>
              Resend code
            </button>
          )}
          <button
            type="button"
            className="text-primary hover:underline"
            onClick={() => {
              setCode("");
              setMode(isMagicMode ? "password" : "magic");
            }}
          >
            {isMagicMode ? "Use password instead" : "Sign in with a one-time code"}
          </button>
          <button
            type="button"
            className="text-muted-foreground hover:underline"
            onClick={() => {
              setCode("");
              setMode(mode === "forgot" ? "password" : "forgot");
            }}
          >
            {mode === "forgot" ? "Back to sign in" : "Forgot password?"}
          </button>
          {setupQuery.data?.required ? (
            <Link to="/setup" className="text-muted-foreground hover:underline">
              First-time setup
            </Link>
          ) : null}
        </div>
        {(() => {
          const ssoSchemes = (schemesQuery.data ?? []).filter((scheme) => scheme.signInUrl);
          if (ssoSchemes.length === 0) {
            return null;
          }
          return (
            <ul className="space-y-2 border-t border-border pt-4">
              {ssoSchemes.map((scheme) => (
                <li key={scheme.developerName}>
                  <a
                    href={scheme.signInUrl ?? "#"}
                    className="flex h-10 items-center justify-center rounded-lg border border-input text-sm font-medium hover:bg-accent"
                  >
                    Continue with {scheme.label}
                  </a>
                </li>
              ))}
            </ul>
          );
        })()}
      </div>
    </div>
  );
}

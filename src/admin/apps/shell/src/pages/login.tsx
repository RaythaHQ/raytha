import {
  completeForgotPassword,
  completeMagicLink,
  formatError,
  getAdminSetupStatus,
  getEnabledAdminSchemes,
  isAuthenticated,
  login,
  requestForgotPassword,
  requestMagicLink,
  type LoginScheme,
} from "@raytha/api";
import { Button, Checkbox, Input, Label } from "@raytha/ui";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { Link, useParams, useSearch } from "@tanstack/react-router";
import { useDocumentTitle } from "../lib/document-title";
import { afterSignInUrl, safeReturnUrl } from "../lib/return-url";

type Mode = "password" | "magic" | "magic-code" | "forgot";

/** The mode to show given which built-in schemes are enabled, or null when only SSO is. */
function availableMode(requested: Mode, passwordEnabled: boolean, magicEnabled: boolean): Mode | null {
  const isMagic = requested === "magic" || requested === "magic-code";
  if (isMagic && !magicEnabled) {
    return passwordEnabled ? "password" : null;
  }
  if (!isMagic && !passwordEnabled) {
    return magicEnabled ? "magic" : null;
  }
  return requested;
}

function AuthCard({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4">
      <div className="w-full max-w-md space-y-6 rounded-xl border border-border bg-card p-8 shadow-card">
        <div className="flex flex-col items-center gap-3">
          <img src="/raytha/color-no-background.svg" alt="Raytha" className="h-10" />
          <h1 className="font-display text-2xl font-semibold">{title}</h1>
          <p className="text-sm text-muted-foreground">Administrator console</p>
        </div>
        {children}
      </div>
    </div>
  );
}

function Notices({ error, message }: { error: string | null; message: string | null }) {
  return (
    <>
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
    </>
  );
}

export function LoginPage({ initialMode = "password" }: { initialMode?: Mode }) {
  useDocumentTitle(["Sign in"]);
  const search = useSearch({ strict: false });
  const returnUrl = safeReturnUrl((search as { returnUrl?: unknown }).returnUrl);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [requestedMode, setRequestedMode] = useState<Mode>(initialMode);

  const schemesQuery = useQuery({
    queryKey: ["admin-login-schemes", returnUrl],
    queryFn: () => getEnabledAdminSchemes(returnUrl ?? undefined),
  });
  const setupQuery = useQuery({
    queryKey: ["admin-setup-status"],
    queryFn: getAdminSetupStatus,
  });

  const schemes: LoginScheme[] | undefined = schemesQuery.data;
  const ssoSchemes = (schemes ?? []).filter((scheme) => scheme.signInUrl);
  // Without the scheme list, offer what every install starts with.
  const passwordEnabled = !schemes || schemes.some((scheme) => scheme.schemeType === "email_and_password");
  const magicEnabled = !!schemes && schemes.some((scheme) => scheme.schemeType === "magic_link");
  const mode = availableMode(requestedMode, passwordEnabled, magicEnabled);
  const isMagicMode = mode === "magic" || mode === "magic-code";
  const onlySsoUrl = schemes && mode === null && ssoSchemes.length === 1 ? ssoSchemes[0]?.signInUrl : null;
  const alreadySignedIn = isAuthenticated();

  useEffect(() => {
    if (alreadySignedIn) {
      window.location.replace(afterSignInUrl(returnUrl));
    } else if (onlySsoUrl) {
      window.location.replace(onlySsoUrl);
    }
  }, [alreadySignedIn, onlySsoUrl, returnUrl]);

  const finishSignIn = () => {
    window.location.assign(afterSignInUrl(returnUrl));
  };

  const switchMode = (next: Mode) => {
    setCode("");
    setError(null);
    setMessage(null);
    setRequestedMode(next);
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setMessage(null);
    setPending(true);
    try {
      if (mode === "magic") {
        await requestMagicLink(email);
        setRequestedMode("magic-code");
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
      await login(email, password, rememberMe);
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

  if (schemesQuery.isPending || alreadySignedIn || onlySsoUrl) {
    return <AuthCard title="Sign in">{null}</AuthCard>;
  }

  return (
    <AuthCard title="Sign in">
      {mode !== null && (
        <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          <Notices error={error} message={message} />
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
            <>
              <div className="space-y-1.5">
                <Label htmlFor="password">Password</Label>
                <Input id="password" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} required />
              </div>
              <div className="flex items-center gap-2">
                <Checkbox id="remember-me" checked={rememberMe} onCheckedChange={setRememberMe} />
                <Label htmlFor="remember-me">Keep me signed in</Label>
              </div>
            </>
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
      )}
      {mode === null && <Notices error={error} message={message} />}
      <div className="flex flex-col gap-2 text-center text-sm">
        {mode === "magic-code" && (
          <button type="button" className="text-primary hover:underline" onClick={() => void resendCode()}>
            Resend code
          </button>
        )}
        {passwordEnabled && magicEnabled && (
          <button type="button" className="text-primary hover:underline" onClick={() => switchMode(isMagicMode ? "password" : "magic")}>
            {isMagicMode ? "Use password instead" : "Sign in with a one-time code"}
          </button>
        )}
        {passwordEnabled && !isMagicMode && (
          <button
            type="button"
            className="text-muted-foreground hover:underline"
            onClick={() => switchMode(mode === "forgot" ? "password" : "forgot")}
          >
            {mode === "forgot" ? "Back to sign in" : "Forgot password?"}
          </button>
        )}
        {setupQuery.data?.required ? (
          <Link to="/setup" className="text-muted-foreground hover:underline">
            First-time setup
          </Link>
        ) : null}
      </div>
      {ssoSchemes.length > 0 && (
        <ul className={mode === null ? "space-y-2" : "space-y-2 border-t border-border pt-4"}>
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
      )}
    </AuthCard>
  );
}

export function ResetPasswordPage() {
  useDocumentTitle(["Reset password"]);
  const params = useParams({ strict: false });
  const token = (params as { token?: string }).token ?? "";
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [pending, setPending] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      await completeForgotPassword(token, newPassword, confirmNewPassword);
      setDone(true);
    } catch (err) {
      setError(formatError(err));
    } finally {
      setPending(false);
    }
  };

  return (
    <AuthCard title="Reset password">
      {done ? (
        <Notices error={null} message="Password changed. Sign in with your new password." />
      ) : (
        <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
          <Notices error={error} message={null} />
          <div className="space-y-1.5">
            <Label htmlFor="new-password">New password</Label>
            <Input
              id="new-password"
              type="password"
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="confirm-new-password">Re-type new password</Label>
            <Input
              id="confirm-new-password"
              type="password"
              autoComplete="new-password"
              value={confirmNewPassword}
              onChange={(e) => setConfirmNewPassword(e.target.value)}
              required
            />
          </div>
          <Button type="submit" className="w-full" loading={pending}>
            Change password
          </Button>
        </form>
      )}
      <div className="text-center text-sm">
        <Link to="/login" className="text-primary hover:underline">
          Back to sign in
        </Link>
      </div>
    </AuthCard>
  );
}

import type { LoginScheme } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createMemoryHistory, createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "./login";

const api = vi.hoisted(() => ({
  schemes: [] as LoginScheme[],
  login: vi.fn(async () => ({ id: "admin", requiresTwoFactor: false })),
  schemesFor: vi.fn(),
}));

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  return {
    ...actual,
    isAuthenticated: () => false,
    getAdminSetupStatus: async () => ({ required: false }),
    getEnabledAdminSchemes: async (returnUrl?: string) => {
      api.schemesFor(returnUrl);
      return api.schemes;
    },
    login: api.login,
  };
});

const PASSWORD: LoginScheme = { label: "Email and password", developerName: "email_and_password", schemeType: "email_and_password", signInUrl: null };
const MAGIC: LoginScheme = { label: "Magic link", developerName: "magic_link", schemeType: "magic_link", signInUrl: null };
const OKTA: LoginScheme = { label: "Okta", developerName: "okta", schemeType: "saml", signInUrl: "/raytha/login/sso/okta" };

const assign = vi.fn();
const replace = vi.fn();
const realLocation = window.location;

function renderLogin(url = "/login") {
  const rootRoute = createRootRoute();
  const login = createRoute({ getParentRoute: () => rootRoute, path: "/login", component: LoginPage });
  const router = createRouter({
    routeTree: rootRoute.addChildren([login]),
    history: createMemoryHistory({ initialEntries: [url] }),
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
}

describe("admin login", () => {
  beforeEach(() => {
    api.schemes = [PASSWORD, MAGIC];
    api.login.mockClear();
    api.schemesFor.mockClear();
    assign.mockClear();
    replace.mockClear();
    Object.defineProperty(window, "location", { configurable: true, value: { ...realLocation, assign, replace } });
  });

  afterEach(() => {
    Object.defineProperty(window, "location", { configurable: true, value: realLocation });
  });

  it("continues to a local returnUrl after sign-in, remembering only when asked", async () => {
    renderLogin("/login?returnUrl=%2Fraytha%2Fusers");
    fireEvent.change(await screen.findByLabelText("Email"), { target: { value: "ada@example.com" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "secret" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(assign).toHaveBeenCalledWith("/raytha/users"));
    expect(api.login).toHaveBeenCalledWith("ada@example.com", "secret", false);
    expect(api.schemesFor).toHaveBeenCalledWith("/raytha/users");
  });

  it("ignores a returnUrl that leaves the site", async () => {
    renderLogin("/login?returnUrl=%2F%2Fevil.example%2F");
    fireEvent.change(await screen.findByLabelText("Email"), { target: { value: "ada@example.com" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "secret" } });
    fireEvent.click(screen.getByRole("checkbox", { name: "Keep me signed in" }));
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(assign).toHaveBeenCalledWith("/raytha"));
    expect(api.login).toHaveBeenCalledWith("ada@example.com", "secret", true);
  });

  it("goes straight to the only sign-in scheme when it is SSO", async () => {
    api.schemes = [OKTA];
    renderLogin();

    await waitFor(() => expect(replace).toHaveBeenCalledWith("/raytha/login/sso/okta"));
  });

  it("offers only the one-time code when password sign-in is disabled", async () => {
    api.schemes = [MAGIC, OKTA];
    renderLogin();

    expect(await screen.findByRole("button", { name: "Email me a code" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Password")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Forgot password?" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Use password instead" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Continue with Okta" })).toHaveAttribute("href", "/raytha/login/sso/okta");
    expect(replace).not.toHaveBeenCalled();
  });

  it("hides the one-time code option when magic link is disabled", async () => {
    api.schemes = [PASSWORD];
    renderLogin();

    expect(await screen.findByLabelText("Password")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Sign in with a one-time code" })).not.toBeInTheDocument();
  });
});

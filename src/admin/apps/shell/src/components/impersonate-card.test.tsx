import type { Me } from "@raytha/api";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ImpersonateCard, type ImpersonationTarget } from "./impersonate-card";

const api = vi.hoisted(() => ({
  session: null as Partial<Me> | null,
  startUser: vi.fn(async () => ({ redirectUrl: "/" })),
  startAdmin: vi.fn(async () => ({ redirectUrl: "/raytha" })),
}));

vi.mock("@raytha/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@raytha/api")>();
  return {
    ...actual,
    currentSession: () => api.session,
    hasPermission: (permission: string) => api.session?.permissions?.includes(permission) ?? false,
    adminApi: {
      ...actual.adminApi,
      impersonation: { startUser: api.startUser, startAdmin: api.startAdmin, stop: vi.fn() },
    },
  };
});

const superAdmin: Partial<Me> = { id: "me", roles: ["super_admin"], permissions: ["users"], impersonation: null };
const userManager: Partial<Me> = { id: "me", roles: ["people"], permissions: ["users"], impersonation: null };

function renderCard(kind: ImpersonationTarget, props: { id?: string; isActive?: boolean } = {}) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <ImpersonateCard id={props.id ?? "target"} name="Jane Doe" kind={kind} isActive={props.isActive ?? true} />
    </QueryClientProvider>,
  );
}

describe("ImpersonateCard", () => {
  const assign = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(window, "location", { value: { ...window.location, assign }, configurable: true });
  });

  it("offers website users to an admin with the users permission", () => {
    api.session = userManager;
    renderCard("websiteUser");
    expect(screen.getByRole("button", { name: "Impersonate" })).toBeInTheDocument();
  });

  it("offers admins only to a super admin", () => {
    api.session = userManager;
    const { unmount } = renderCard("admin");
    expect(screen.queryByRole("button", { name: "Impersonate" })).toBeNull();
    unmount();

    api.session = superAdmin;
    renderCard("admin");
    expect(screen.getByRole("button", { name: "Impersonate" })).toBeInTheDocument();
  });

  it("is hidden for yourself, a suspended account, and while already impersonating", () => {
    api.session = superAdmin;
    const self = renderCard("websiteUser", { id: "me" });
    expect(screen.queryByRole("button", { name: "Impersonate" })).toBeNull();
    self.unmount();

    const suspended = renderCard("websiteUser", { isActive: false });
    expect(screen.queryByRole("button", { name: "Impersonate" })).toBeNull();
    suspended.unmount();

    api.session = {
      ...superAdmin,
      impersonation: {
        impersonatorId: "a",
        impersonatorName: "A",
        impersonatorEmail: "a@x.com",
        startedAt: "",
        expiresAt: "",
      },
    };
    renderCard("websiteUser");
    expect(screen.queryByRole("button", { name: "Impersonate" })).toBeNull();
  });

  it("confirms before starting, then follows the redirect", async () => {
    api.session = superAdmin;
    renderCard("admin");

    fireEvent.click(screen.getByRole("button", { name: "Impersonate" }));
    expect(api.startAdmin).not.toHaveBeenCalled();
    expect(screen.getByText("Sign in as Jane Doe?")).toBeInTheDocument();

    const confirm = screen.getAllByRole("button", { name: "Impersonate" }).at(-1);
    if (!confirm) throw new Error("confirm button missing");
    fireEvent.click(confirm);

    await waitFor(() => expect(assign).toHaveBeenCalledWith("/raytha"));
    expect(api.startAdmin).toHaveBeenCalledWith("target");
    expect(api.startUser).not.toHaveBeenCalled();
  });
});

import {
  parseAdminApiKey,
  parseBackgroundTask,
  parseCreatedApiKey,
  parseClearedLog,
  parseEmailTemplate,
  parseFunction,
  parseIdResponse,
  parseMaintenanceSnapshot,
  parseMenu,
  parseMenuItem,
  parseMenuItems,
  parsePaged,
  parseRevision,
  parseSchemaImportResult,
  parseThemeMediaItems,
  parseWebTemplate,
  parseWidgetDefinitions,
  parseWidgetFieldTypeOptions,
  parseWidgetTemplate,
  requireValue,
} from "./parse";
import type {
  AdminSetupStatus,
  BackgroundTaskDetail,
  AuthenticationSchemeRequest,
  ClearedLog,
  ConfigurationOptions,
  CreateWidgetTemplateInput,
  DuplicateThemeInput,
  EmailTemplateDetail,
  EntityRef,
  ExportContentItemsToCsvInput,
  FunctionDetail,
  IdResponse,
  ImportContentItemsFromCsvInput,
  ImportThemeFromUrlInput,
  JsonObject,
  LoginResponse,
  LoginScheme,
  LogRetention,
  MaintenanceSnapshot,
  Me,
  MediaConfig,
  MediaItem,
  MediaItemUsage,
  MenuDetail,
  MenuItemDetail,
  PagedResult,
  PlatformVersion,
  RetainedLogKey,
  RolePermissionCatalog,
  SitePageWidgetDefinition,
  ThemeMediaItem,
  UpdateWidgetTemplateInput,
  WidgetFieldTypeOption,
} from "./types";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

/** One widget in PUT /site-pages/{id}/widgets. Matches SaveWidgets.WidgetInput. */
export interface SitePageWidgetInput {
  id?: string;
  widgetType: string;
  settingsJson: string;
  row: number;
  column: number;
  columnSpan: number;
  cssClass: string;
  htmlId: string;
  customAttributes: string;
}

/** Body for PUT /site-pages/{id}/widgets. Matches SaveWidgets.Command. */
export interface SaveSitePageWidgetsInput {
  sectionName: string;
  widgets: SitePageWidgetInput[];
}

// Cookie session is cached so route guards can stay synchronous.
let currentUser: Me | null = null;

export function isAuthenticated(): boolean {
  return currentUser !== null;
}

export function currentSession(): Me | null {
  return currentUser;
}

export function patchSession(patch: Partial<Me>): void {
  if (currentUser) {
    currentUser = { ...currentUser, ...patch };
  }
}

export function clearSession(): void {
  currentUser = null;
}

/**
 * Built-in system permission keys used to gate admin navigation and routes. Values match
 * `BuiltInSystemPermission` on the server, which is what `/raytha/api/auth/me` returns in
 * `permissions` (super admins hold every key).
 */
export const platformPermissions = {
  users: "users",
  admins: "administrators",
  sitePages: "site_pages",
  contentTypes: "content_types",
  templates: "templates",
  systemSettings: "system_settings",
  auditLogs: "audit_logs",
  media: "media_items",
} as const;

export function hasPermission(permission: string): boolean {
  if (currentUser === null) {
    return false;
  }
  // `isAdmin` only means "may use the admin area"; granular access comes from role permissions.
  return currentUser.permissions.includes(permission);
}

export type ContentTypeAccess = "read" | "edit" | "config";

/** Mirrors RaythaAdminAuthorizationHandler: manage-content-types grants every content type. */
export function hasContentTypePermission(developerName: string, access: ContentTypeAccess): boolean {
  if (currentUser === null) {
    return false;
  }
  if (currentUser.permissions.includes(platformPermissions.contentTypes)) {
    return true;
  }
  return currentUser.contentTypePermissions.includes(`${developerName}_${access}`);
}

/** Resolves the cookie session before the router renders anything. */
export async function bootstrapSession(): Promise<Me | null> {
  try {
    currentUser = await apiFetch<Me>("/raytha/api/auth/me");
  } catch {
    currentUser = null;
  }
  return currentUser;
}

async function requireBootstrappedSession(): Promise<void> {
  if ((await bootstrapSession()) === null) {
    throw new ApiError(401, "Sign-in succeeded but the session could not be loaded.");
  }
}

async function readProblem(response: Response, fallback: string): Promise<string> {
  try {
    const problem = (await response.json()) as { detail?: string; title?: string };
    return problem.detail ?? problem.title ?? fallback;
  } catch {
    return fallback;
  }
}

export async function getAdminSetupStatus(): Promise<AdminSetupStatus> {
  return apiFetch<AdminSetupStatus>("/raytha/api/auth/setup");
}

export async function createFirstAdmin(input: {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  websiteUrl: string;
}): Promise<void> {
  const response = await fetch("/raytha/api/auth/setup", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Setup failed."));
  }

  await bootstrapSession();
}

export async function login(email: string, password: string, rememberMe = false): Promise<LoginResponse> {
  const response = await fetch("/raytha/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, rememberMe }),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Sign-in failed."));
  }

  const result = (await response.json()) as LoginResponse;
  if (!result.requiresTwoFactor) {
    await requireBootstrappedSession();
  }
  return result;
}

export async function requestMagicLink(email: string): Promise<void> {
  const response = await fetch("/raytha/api/auth/magic-link", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Could not send a sign-in code."));
  }
}

export async function completeMagicLink(email: string, code: string): Promise<void> {
  const response = await fetch("/raytha/api/auth/magic-link/complete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, code }),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Invalid or expired code."));
  }

  await requireBootstrappedSession();
}

export async function requestForgotPassword(email: string): Promise<void> {
  const response = await fetch("/raytha/api/auth/forgot-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email }),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Could not send a reset email."));
  }
}

export async function completeForgotPassword(token: string, newPassword: string, confirmNewPassword: string): Promise<void> {
  const response = await fetch("/raytha/api/auth/forgot-password/complete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword, confirmNewPassword }),
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Could not reset the password."));
  }
}

export async function getEnabledAdminSchemes(returnUrl?: string): Promise<LoginScheme[]> {
  const query = returnUrl ? `?${new URLSearchParams({ returnUrl }).toString()}` : "";
  const response = await fetch(`/raytha/api/auth/schemes${query}`);
  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response, "Could not load sign-in methods."));
  }

  return (await response.json()) as LoginScheme[];
}

export async function logout(): Promise<void> {
  try {
    await fetch("/raytha/api/auth/logout", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: "{}",
    });
  } finally {
    currentUser = null;
  }
}

async function fetchUnknown(path: string, init?: RequestInit): Promise<unknown> {
  const method = (init?.method ?? "GET").toUpperCase();
  const isMutating = method !== "GET" && method !== "HEAD";
  const response = await fetch(path, {
    ...init,
    headers: {
      ...(init?.body || isMutating ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (response.status === 401) {
    currentUser = null;
    // An unread body keeps the request open until garbage collection.
    await response.body?.cancel();
    throw new ApiError(401, "Not authenticated");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new ApiError(response.status, text || response.statusText);
  }

  if (response.status === 204) {
    return undefined;
  }

  const text = await response.text();
  if (text.length === 0) {
    return undefined;
  }

  return JSON.parse(text);
}

const CLEAR_LOG_PATH: Record<RetainedLogKey, string> = {
  audit_logs: "/raytha/api/admin/audit-logs",
  email_logs: "/raytha/api/admin/email-log",
  webhook_deliveries: "/raytha/api/admin/webhooks/deliveries",
  background_tasks: "/raytha/api/admin/background-tasks",
};

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const method = (init?.method ?? "GET").toUpperCase();
  const isMutating = method !== "GET" && method !== "HEAD";
  const response = await fetch(path, {
    ...init,
    headers: {
      ...(init?.body || isMutating ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (response.status === 401) {
    currentUser = null;
    // An unread body keeps the request open until garbage collection.
    await response.body?.cancel();
    throw new ApiError(401, "Not authenticated");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new ApiError(response.status, text || response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** Human-readable message from an ApiError carrying an RFC 7807 problem body. */
export function formatError(e: unknown): string {
  if (e instanceof ApiError) {
    try {
      const problem = JSON.parse(e.message) as {
        errors?: Record<string, string[]>;
        detail?: string;
        title?: string;
      };
      const messages = Object.values(problem.errors ?? {}).flat();
      if (messages.length > 0) {
        return messages.join(" ");
      }
      if (problem.detail || problem.title) {
        return problem.detail ?? problem.title ?? "Request failed.";
      }
    } catch {
      // Not a JSON problem body; fall through to the raw message.
    }
    return e.message || "Request failed.";
  }
  return e instanceof Error ? e.message : "Request failed.";
}

/** Every message from a 400 validation problem, in server order; empty for any other error. */
export function problemMessages(e: unknown): string[] {
  if (!(e instanceof ApiError) || e.status !== 400) {
    return [];
  }
  try {
    const problem = JSON.parse(e.message) as { errors?: Record<string, unknown> };
    return Object.values(problem.errors ?? {})
      .flatMap((messages) => (Array.isArray(messages) ? messages : []))
      .filter((message): message is string => typeof message === "string" && message.length > 0);
  } catch {
    return [];
  }
}

/**
 * Per-field messages from a 400 validation problem, keyed by the server property name
 * (`DeveloperName`); form-level failures are under `""`. Empty for any other error.
 */
export function problemFieldErrors(e: unknown): Record<string, string> {
  if (!(e instanceof ApiError) || e.status !== 400) {
    return {};
  }
  try {
    const problem = JSON.parse(e.message) as { errors?: Record<string, unknown> };
    const fields: Record<string, string> = {};
    for (const [key, messages] of Object.entries(problem.errors ?? {})) {
      if (Array.isArray(messages)) {
        fields[key] = messages.filter((message) => typeof message === "string").join(" ");
      }
    }
    return fields;
  } catch {
    return {};
  }
}

function listQuery(path: string, params?: Record<string, string | number | boolean | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params ?? {})) {
    if (value !== undefined && value !== "") {
      search.set(key, String(value));
    }
  }
  const qs = search.toString();
  return qs ? `${path}?${qs}` : path;
}

function crud<T extends EntityRef>(base: string) {
  return {
    list: (params?: Record<string, string | number | boolean | undefined>) =>
      apiFetch<PagedResult<T>>(listQuery(base, params)),
    get: (id: string) => apiFetch<T>(`${base}/${id}`),
    create: (input: JsonObject) => apiFetch<T>(base, { method: "POST", body: JSON.stringify(input) }),
    update: (id: string, input: JsonObject) =>
      apiFetch<T>(`${base}/${id}`, { method: "PUT", body: JSON.stringify(input) }),
    remove: (id: string) => apiFetch<void>(`${base}/${id}`, { method: "DELETE" }),
  };
}

/** Typed admin API client for the cookie-authenticated endpoints under /raytha/api/admin. */
export const adminApi = {
  dashboard: () => apiFetch<JsonObject>("/raytha/api/admin/dashboard"),

  users: {
    ...crud<EntityRef>("/raytha/api/admin/users"),
    suspend: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/users/${id}/suspend`, { method: "POST", body: "{}" }),
    restore: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/users/${id}/restore`, { method: "POST", body: "{}" }),
    resetPassword: (
      id: string,
      input: { newPassword?: string; confirmNewPassword?: string; sendEmail?: boolean },
    ) =>
      apiFetch<void>(`/raytha/api/admin/users/${id}/reset-password`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
  },
  userGroups: crud<EntityRef>("/raytha/api/admin/user-groups"),
  admins: {
    ...crud<EntityRef>("/raytha/api/admin/admins"),
    suspend: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/admins/${id}/suspend`, { method: "POST", body: "{}" }),
    restore: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/admins/${id}/restore`, { method: "POST", body: "{}" }),
    removeAccess: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/admins/${id}/remove-access`, { method: "POST", body: "{}" }),
    resetPassword: (
      id: string,
      input: { newPassword?: string; confirmNewPassword?: string; sendEmail?: boolean },
    ) =>
      apiFetch<void>(`/raytha/api/admin/admins/${id}/reset-password`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    apiKeys: (adminId: string) => {
      const base = `/raytha/api/admin/admins/${adminId}/api-keys`;
      return {
        list: () =>
          apiFetch<unknown>(listQuery(base, { pageSize: 10 })).then((value) =>
            parsePaged(value, parseAdminApiKey),
          ),
        create: () => apiFetch<unknown>(base, { method: "POST", body: "{}" }).then(parseCreatedApiKey),
        remove: (keyId: string) => apiFetch<void>(`${base}/${keyId}`, { method: "DELETE" }),
      };
    },
  },
  roles: {
    ...crud<EntityRef>("/raytha/api/admin/roles"),
    permissions: () => apiFetch<RolePermissionCatalog>("/raytha/api/admin/roles/permissions"),
  },

  contentTypes: {
    ...crud<EntityRef>("/raytha/api/admin/content-types"),
    byDeveloperName: (developerName: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/content-types/${encodeURIComponent(developerName)}`),
    updateByDeveloperName: (developerName: string, input: JsonObject) =>
      apiFetch<EntityRef>(`/raytha/api/admin/content-types/${encodeURIComponent(developerName)}`, {
        method: "PUT",
        body: JSON.stringify(input),
      }),
    removeByDeveloperName: (developerName: string) =>
      apiFetch<void>(`/raytha/api/admin/content-types/${encodeURIComponent(developerName)}`, {
        method: "DELETE",
      }),
    fieldTypes: () => apiFetch<unknown>("/raytha/api/admin/content-types/field-types"),
    /** Every content type, field, choice, and view as one portable document. */
    exportSchema: () => apiFetch<JsonObject>("/raytha/api/admin/content-types/schema/export"),
    /** `dryRun` reports what would change and applies nothing. */
    importSchema: (schema: JsonObject, dryRun: boolean) =>
      apiFetch<unknown>(`/raytha/api/admin/content-types/schema/import${dryRun ? "?dryRun=true" : ""}`, {
        method: "POST",
        body: JSON.stringify(schema),
      }).then((value) => requireValue(parseSchemaImportResult(value), "schema import result")),
    templates: (developerName: string) =>
      apiFetch<unknown>(`/raytha/api/admin/content-types/${encodeURIComponent(developerName)}/templates`),
  },
  fields: (contentType: string) => {
    const encoded = encodeURIComponent(contentType);
    const base = `/raytha/api/admin/content-types/${encoded}/fields`;
    return {
      ...crud<EntityRef>(base),
      reorder: (id: string, newFieldOrder: number) =>
        apiFetch<EntityRef>(`${base}/${id}/reorder`, {
          method: "POST",
          body: JSON.stringify({ newFieldOrder }),
        }),
    };
  },
  views: (contentType: string) => {
    const encoded = encodeURIComponent(contentType);
    const base = `/raytha/api/admin/content-types/${encoded}/views`;
    return {
      ...crud<EntityRef>(base),
      favorites: (params?: Record<string, string | number | boolean | undefined>) =>
        apiFetch<PagedResult<EntityRef>>(listQuery(`${base}/favorites`, params)),
      favorite: (id: string, setAsFavorite: boolean) =>
        apiFetch<EntityRef>(`${base}/${id}/favorite`, {
          method: "POST",
          body: JSON.stringify({ setAsFavorite }),
        }),
      updateColumns: (id: string, input: { developerName: string; showColumn: boolean }) =>
        apiFetch<EntityRef>(`${base}/${id}/columns`, { method: "PUT", body: JSON.stringify(input) }),
      reorderColumns: (id: string, input: { developerName: string; newFieldOrder: number }) =>
        apiFetch<EntityRef>(`${base}/${id}/columns/reorder`, {
          method: "POST",
          body: JSON.stringify(input),
        }),
      updateSort: (id: string, input: { developerName: string; showColumn: boolean; orderByDirection: string }) =>
        apiFetch<EntityRef>(`${base}/${id}/sort`, { method: "PUT", body: JSON.stringify(input) }),
      reorderSort: (id: string, input: { developerName: string; newFieldOrder: number }) =>
        apiFetch<EntityRef>(`${base}/${id}/sort/reorder`, {
          method: "POST",
          body: JSON.stringify(input),
        }),
      updateFilter: (id: string, filter: JsonObject[]) =>
        apiFetch<EntityRef>(`${base}/${id}/filter`, {
          method: "PUT",
          body: JSON.stringify({ filter }),
        }),
      updatePublicSettings: (id: string, input: JsonObject) =>
        apiFetch<EntityRef>(`${base}/${id}/public-settings`, {
          method: "PUT",
          body: JSON.stringify(input),
        }),
      setAsHomePage: (id: string) => apiFetch<EntityRef>(`${base}/${id}/set-as-home-page`, { method: "POST" }),
      template: (id: string) => apiFetch<unknown>(`${base}/${id}/template`),
      exportCsv: (id: string, input: ExportContentItemsToCsvInput): Promise<IdResponse> =>
        fetchUnknown(`${base}/${id}/export`, {
          method: "POST",
          body: JSON.stringify(input),
        }).then(parseIdResponse),
    };
  },
  contentItems: (contentType: string) => {
    const encoded = encodeURIComponent(contentType);
    const base = `/raytha/api/admin/content-types/${encoded}/items`;
    return {
      ...crud<EntityRef>(base),
      trash: (params?: Record<string, string | number | boolean | undefined>) =>
        apiFetch<PagedResult<EntityRef>>(listQuery(`${base}/trash`, params)),
      restore: (id: string) => apiFetch<EntityRef>(`${base}/${id}/restore`, { method: "POST" }),
      deleteTrash: (id: string) => apiFetch<void>(`${base}/trash/${id}`, { method: "DELETE" }),
      revisions: (id: string, params?: Record<string, string | number | boolean | undefined>) =>
        apiFetch<PagedResult<EntityRef>>(listQuery(`${base}/${id}/revisions`, params)),
      revert: (revisionId: string) =>
        apiFetch<EntityRef>(`${base}/revisions/${revisionId}/revert`, { method: "POST" }),
      unpublish: (id: string) => apiFetch<EntityRef>(`${base}/${id}/unpublish`, { method: "POST" }),
      discardDraft: (id: string) => apiFetch<EntityRef>(`${base}/${id}/discard-draft`, { method: "POST" }),
      updateSettings: (id: string, input: JsonObject) =>
        apiFetch<EntityRef>(`${base}/${id}/settings`, { method: "PUT", body: JSON.stringify(input) }),
      setAsHomePage: (id: string) => apiFetch<EntityRef>(`${base}/${id}/set-as-home-page`, { method: "POST" }),
      importCsv: (input: ImportContentItemsFromCsvInput): Promise<IdResponse> =>
        fetchUnknown(`${base}/import`, {
          method: "POST",
          body: JSON.stringify(input),
        }).then(parseIdResponse),
    };
  },

  media: {
    list: (params?: Record<string, string | number | boolean | undefined>) =>
      apiFetch<PagedResult<MediaItem>>(listQuery("/raytha/api/admin/media", params)),
    get: (id: string) => apiFetch<MediaItem>(`/raytha/api/admin/media/${id}`),
    usage: (id: string) => apiFetch<MediaItemUsage>(`/raytha/api/admin/media/${id}/usage`),
    remove: (id: string) => apiFetch<void>(`/raytha/api/admin/media/${id}`, { method: "DELETE" }),
    config: () => apiFetch<MediaConfig>("/raytha/api/admin/media/config"),
  },

  sitePages: {
    ...crud<EntityRef>("/raytha/api/admin/site-pages"),
    widgetDefinitions: (): Promise<SitePageWidgetDefinition[]> =>
      fetchUnknown("/raytha/api/admin/site-pages/widget-definitions").then(parseWidgetDefinitions),
    updateSettings: (id: string, input: { routePath: string }) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/${id}/settings`, {
        method: "PUT",
        body: JSON.stringify(input),
      }),
    saveWidgets: (id: string, input: SaveSitePageWidgetsInput) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/${id}/widgets`, {
        method: "PUT",
        body: JSON.stringify(input),
      }),
    publish: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/${id}/publish`, { method: "POST" }),
    unpublish: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/${id}/unpublish`, { method: "POST" }),
    discardDraft: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/${id}/discard-draft`, { method: "POST" }),
    setAsHomePage: (id: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/${id}/set-as-home-page`, { method: "POST" }),
    revisions: (id: string, params?: Record<string, string | number | boolean | undefined>) =>
      apiFetch<PagedResult<EntityRef>>(listQuery(`/raytha/api/admin/site-pages/${id}/revisions`, params)),
    revertRevision: (revisionId: string) =>
      apiFetch<EntityRef>(`/raytha/api/admin/site-pages/revisions/${revisionId}/revert`, { method: "POST" }),
  },
  themes: {
    ...crud<EntityRef>("/raytha/api/admin/themes"),
    setActive: (id: string): Promise<IdResponse> =>
      fetchUnknown(`/raytha/api/admin/themes/${id}/set-active`, { method: "POST", body: "{}" }).then(
        parseIdResponse,
      ),
    setExportability: (id: string, input: { isExportable: boolean }): Promise<IdResponse> =>
      fetchUnknown(`/raytha/api/admin/themes/${id}/exportability`, {
        method: "PUT",
        body: JSON.stringify(input),
      }).then(parseIdResponse),
    duplicate: (id: string, input: DuplicateThemeInput): Promise<IdResponse> =>
      fetchUnknown(`/raytha/api/admin/themes/${id}/duplicate`, {
        method: "POST",
        body: JSON.stringify(input),
      }).then(parseIdResponse),
    importFromUrl: (input: ImportThemeFromUrlInput): Promise<IdResponse> =>
      fetchUnknown("/raytha/api/admin/themes/import", {
        method: "POST",
        body: JSON.stringify(input),
      }).then(parseIdResponse),
    media: (id: string): Promise<ThemeMediaItem[]> =>
      fetchUnknown(`/raytha/api/admin/themes/${id}/media`).then(parseThemeMediaItems),
    widgetFieldTypes: (): Promise<WidgetFieldTypeOption[]> =>
      fetchUnknown("/raytha/api/admin/themes/widget-field-types").then(parseWidgetFieldTypeOptions),
  },
  webTemplates: (themeId: string) => {
    const base = `/raytha/api/admin/themes/${themeId}/web-templates`;
    return {
      list: (params?: Record<string, string | number | boolean | undefined>) =>
        fetchUnknown(listQuery(base, params)).then((value) => parsePaged(value, parseWebTemplate)),
      get: (id: string) =>
        fetchUnknown(`${base}/${id}`).then((value) => requireValue(parseWebTemplate(value), "web template")),
      create: (input: JsonObject) =>
        fetchUnknown(base, { method: "POST", body: JSON.stringify(input) }).then(parseIdResponse),
      update: (id: string, input: JsonObject) =>
        fetchUnknown(`${base}/${id}`, { method: "PUT", body: JSON.stringify(input) }).then(parseIdResponse),
      remove: async (id: string) => {
        await fetchUnknown(`${base}/${id}`, { method: "DELETE" });
      },
      favorite: (id: string, setAsFavorite: boolean) =>
        fetchUnknown(`${base}/${id}/favorite`, { method: "POST", body: JSON.stringify({ setAsFavorite }) }).then(
          parseIdResponse,
        ),
      revisions: (id: string, params?: Record<string, string | number | boolean | undefined>) =>
        fetchUnknown(listQuery(`${base}/${id}/revisions`, params)).then((value) =>
          parsePaged(value, parseRevision),
        ),
      revert: (revisionId: string) =>
        fetchUnknown(`${base}/revisions/${revisionId}/revert`, { method: "POST", body: "{}" }).then(
          parseIdResponse,
        ),
    };
  },
  widgetTemplates: (themeId: string) => {
    const base = `/raytha/api/admin/themes/${themeId}/widget-templates`;
    return {
      list: (params?: Record<string, string | number | boolean | undefined>) =>
        fetchUnknown(listQuery(base, params)).then((value) => parsePaged(value, parseWidgetTemplate)),
      get: (id: string) =>
        fetchUnknown(`${base}/${id}`).then((value) => requireValue(parseWidgetTemplate(value), "widget template")),
      create: (input: CreateWidgetTemplateInput) =>
        fetchUnknown(base, { method: "POST", body: JSON.stringify(input) }).then(parseIdResponse),
      update: (id: string, input: UpdateWidgetTemplateInput) =>
        fetchUnknown(`${base}/${id}`, { method: "PUT", body: JSON.stringify(input) }).then(parseIdResponse),
      remove: async (id: string) => {
        await fetchUnknown(`${base}/${id}`, { method: "DELETE" });
      },
      revisions: (id: string, params?: Record<string, string | number | boolean | undefined>) =>
        fetchUnknown(listQuery(`${base}/${id}/revisions`, params)).then((value) =>
          parsePaged(value, parseRevision),
        ),
      revert: (revisionId: string) =>
        fetchUnknown(`${base}/revisions/${revisionId}/revert`, { method: "POST", body: "{}" }).then(
          parseIdResponse,
        ),
      reset: () => fetchUnknown(`${base}/reset`, { method: "POST", body: "{}" }).then(parseIdResponse),
    };
  },
  emailTemplates: {
    ...crud<EntityRef>("/raytha/api/admin/email-templates"),
    get: (id: string): Promise<EmailTemplateDetail> =>
      fetchUnknown(`/raytha/api/admin/email-templates/${id}`).then((value) =>
        requireValue(parseEmailTemplate(value), "email template"),
      ),
    update: (id: string, input: JsonObject): Promise<IdResponse> =>
      fetchUnknown(`/raytha/api/admin/email-templates/${id}`, {
        method: "PUT",
        body: JSON.stringify(input),
      }).then(parseIdResponse),
    revisions: (id: string, params?: Record<string, string | number | boolean | undefined>) =>
      fetchUnknown(listQuery(`/raytha/api/admin/email-templates/${id}/revisions`, params)).then((value) =>
        parsePaged(value, parseRevision),
      ),
    revert: (revisionId: string) =>
      fetchUnknown(`/raytha/api/admin/email-templates/revisions/${revisionId}/revert`, {
        method: "POST",
        body: "{}",
      }).then(parseIdResponse),
  },
  menus: {
    ...crud<EntityRef>("/raytha/api/admin/navigation-menus"),
    get: (id: string): Promise<MenuDetail> =>
      fetchUnknown(`/raytha/api/admin/navigation-menus/${id}`).then((value) =>
        requireValue(parseMenu(value), "menu"),
      ),
    setMain: (id: string) =>
      fetchUnknown(`/raytha/api/admin/navigation-menus/${id}/set-main`, {
        method: "POST",
        body: "{}",
      }).then(parseIdResponse),
    items: (menuId: string) => {
      const base = `/raytha/api/admin/navigation-menus/${menuId}/items`;
      return {
        list: (): Promise<MenuItemDetail[]> => fetchUnknown(base).then(parseMenuItems),
        get: (id: string): Promise<MenuItemDetail> =>
          fetchUnknown(`${base}/${id}`).then((value) => requireValue(parseMenuItem(value), "menu item")),
        create: (input: JsonObject) =>
          fetchUnknown(base, { method: "POST", body: JSON.stringify(input) }).then(parseIdResponse),
        update: (id: string, input: JsonObject) =>
          fetchUnknown(`${base}/${id}`, { method: "PUT", body: JSON.stringify(input) }).then(parseIdResponse),
        remove: async (id: string) => {
          await fetchUnknown(`${base}/${id}`, { method: "DELETE" });
        },
        reorder: (id: string, ordinal: number) =>
          fetchUnknown(`${base}/${id}/reorder`, {
            method: "POST",
            body: JSON.stringify({ ordinal }),
          }).then(parseIdResponse),
      };
    },
  },
  functions: {
    ...crud<EntityRef>("/raytha/api/admin/functions"),
    get: (id: string): Promise<FunctionDetail> =>
      fetchUnknown(`/raytha/api/admin/functions/${id}`).then((value) =>
        requireValue(parseFunction(value), "function"),
      ),
    update: (id: string, input: JsonObject): Promise<IdResponse> =>
      fetchUnknown(`/raytha/api/admin/functions/${id}`, {
        method: "PUT",
        body: JSON.stringify(input),
      }).then(parseIdResponse),
    revisions: (id: string, params?: Record<string, string | number | boolean | undefined>) =>
      fetchUnknown(listQuery(`/raytha/api/admin/functions/${id}/revisions`, params)).then((value) =>
        parsePaged(value, parseRevision),
      ),
    revert: (revisionId: string) =>
      fetchUnknown(`/raytha/api/admin/functions/revisions/${revisionId}/revert`, {
        method: "POST",
        body: "{}",
      }).then(parseIdResponse),
  },
  authSchemes: {
    ...crud<EntityRef>("/raytha/api/admin/authentication-schemes"),
    createScheme: (input: AuthenticationSchemeRequest) =>
      apiFetch<EntityRef>("/raytha/api/admin/authentication-schemes", {
        method: "POST",
        body: JSON.stringify(input),
      }),
    updateScheme: (id: string, input: AuthenticationSchemeRequest) =>
      apiFetch<EntityRef>(`/raytha/api/admin/authentication-schemes/${id}`, {
        method: "PUT",
        body: JSON.stringify(input),
      }),
  },

  configuration: {
    get: () => apiFetch<JsonObject>("/raytha/api/admin/configuration"),
    update: (input: JsonObject) =>
      apiFetch<JsonObject>("/raytha/api/admin/configuration", { method: "PUT", body: JSON.stringify(input) }),
    options: () => apiFetch<ConfigurationOptions>("/raytha/api/admin/configuration/options"),
  },
  smtp: {
    get: () => apiFetch<JsonObject>("/raytha/api/admin/smtp"),
    update: (input: JsonObject) =>
      apiFetch<JsonObject>("/raytha/api/admin/smtp", { method: "PUT", body: JSON.stringify(input) }),
    sendTest: () => apiFetch<void>("/raytha/api/admin/smtp/test", { method: "POST" }),
  },

  auditLogs: {
    list: (params?: Record<string, string | number | boolean | undefined>) =>
      apiFetch<PagedResult<EntityRef>>(listQuery("/raytha/api/admin/audit-logs", params)),
    get: (id: string) => apiFetch<EntityRef>(`/raytha/api/admin/audit-logs/${id}`),
    categories: () => apiFetch<string[]>("/raytha/api/admin/audit-logs/categories"),
  },

  /** Each call swaps the session cookie; follow `redirectUrl` with a full page load. */
  impersonation: {
    startUser: (id: string) =>
      apiFetch<{ redirectUrl: string }>(`/raytha/api/auth/impersonation/users/${id}`, { method: "POST", body: "{}" }),
    startAdmin: (id: string) =>
      apiFetch<{ redirectUrl: string }>(`/raytha/api/auth/impersonation/admins/${id}`, { method: "POST", body: "{}" }),
    stop: () => apiFetch<{ redirectUrl: string }>("/raytha/api/auth/impersonation/stop", { method: "POST", body: "{}" }),
  },

  profile: {
    get: () => apiFetch<Me>("/raytha/api/auth/me"),
    update: (input: { firstName: string; lastName: string }) =>
      apiFetch<Me>("/raytha/api/admin/profile", { method: "PUT", body: JSON.stringify(input) }),
    changePassword: (input: { currentPassword: string; newPassword: string }) =>
      apiFetch<void>("/raytha/api/admin/profile/password", { method: "POST", body: JSON.stringify(input) }),
  },

  backgroundTasks: {
    list: (params?: Record<string, string | number | boolean | undefined>): Promise<PagedResult<BackgroundTaskDetail>> =>
      fetchUnknown(listQuery("/raytha/api/admin/background-tasks", params)).then((value) =>
        parsePaged(value, parseBackgroundTask),
      ),
    get: (id: string): Promise<BackgroundTaskDetail> =>
      fetchUnknown(`/raytha/api/admin/background-tasks/${id}`).then((value) =>
        requireValue(parseBackgroundTask(value), "background task"),
      ),
  },

  webhooks: {
    ...crud<EntityRef>("/raytha/api/admin/webhooks"),
    events: () => apiFetch<unknown>("/raytha/api/admin/webhooks/events"),
  },
  emailLog: {
    list: (params?: Record<string, string | number | boolean | undefined>) =>
      apiFetch<PagedResult<EntityRef>>(listQuery("/raytha/api/admin/email-log", params)),
    get: (id: string) => apiFetch<EntityRef>(`/raytha/api/admin/email-log/${id}`),
  },
  maintenance: {
    snapshot: (): Promise<MaintenanceSnapshot> =>
      fetchUnknown("/raytha/api/admin/maintenance").then(parseMaintenanceSnapshot),
    retention: () => apiFetch<LogRetention>("/raytha/api/admin/maintenance/retention"),
    updateRetention: (input: LogRetention) =>
      apiFetch<LogRetention>("/raytha/api/admin/maintenance/retention", { method: "PUT", body: JSON.stringify(input) }),
    /** Deletes every row of a log now (finished tasks only for background tasks). */
    clearLog: (key: RetainedLogKey): Promise<ClearedLog> =>
      fetchUnknown(CLEAR_LOG_PATH[key], { method: "DELETE" }).then(parseClearedLog),
  },
  version: () => apiFetch<PlatformVersion>("/raytha/api/admin/version"),
};

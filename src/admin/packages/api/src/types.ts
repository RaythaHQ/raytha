export interface Me {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  permissions: string[];
  /** `${contentTypeDeveloperName}_${read|edit|config}` claims from roles. */
  contentTypePermissions: string[];
  /** Role developer names, e.g. `super_admin`. */
  roles: string[];
  isAdmin: boolean;
  /** Id of the view, content item, or site page serving `/`; null when unset. */
  homePageId: string | null;
  organization: OrganizationSummary;
  /** Present only while an admin is signed in as this account. */
  impersonation: ImpersonationSession | null;
}

/** `dateFormat` is a .NET pattern from organization settings: `MM/dd/yyyy` or `dd/MM/yyyy`. */
export interface OrganizationSummary {
  name: string;
  websiteUrl: string;
  timeZone: string;
  dateFormat: string;
  pathBase: string;
}

export interface ImpersonationSession {
  impersonatorId: string;
  impersonatorName: string;
  impersonatorEmail: string;
  startedAt: string;
  expiresAt: string;
}

export interface LoginResponse {
  requiresTwoFactor?: boolean;
}

export type AuthSchemeType = "email_and_password" | "magic_link" | "jwt" | "saml";

export const AUTH_SCHEME_TYPES: readonly AuthSchemeType[] = [
  "email_and_password",
  "magic_link",
  "jwt",
  "saml",
];

export interface LoginScheme {
  label: string;
  developerName: string;
  schemeType: AuthSchemeType;
  signInUrl: string | null;
}

export interface AdminSetupStatus {
  required: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

/** A stored admin API key. The secret is not included; it is returned once at creation. */
export interface AdminApiKey {
  id: string;
  creationTime: string;
  creatorName: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface MediaConfig {
  useDirectUploadToCloud: boolean;
  maxUploadBytes: number;
  allowedMimeTypes: string;
  fileStorageProvider: string;
}

export interface MediaItem {
  id: string;
  fileName: string;
  contentType: string;
  length: number;
  objectKey: string;
  fileStorageProvider: string;
  creationTime: string;
  url: string;
}

export interface MediaItemUsage {
  themes: { id: string; title: string }[];
  webTemplates: { id: string; label: string; themeId: string; themeTitle: string }[];
}

export type ConfigOption = {
  value: string;
  label: string;
};

export type ConfigurationOptions = {
  timeZones: ConfigOption[];
  dateFormats: ConfigOption[];
};

export type PermissionOption = {
  label: string;
  developerName: string;
};

export type RolePermissionCatalog = {
  systemPermissions: PermissionOption[];
  contentTypePermissions: PermissionOption[];
};

export type AuthenticationSchemeRequest = {
  label: string;
  developerName?: string;
  authenticationSchemeType: AuthSchemeType;
  loginButtonText?: string;
  signInUrl?: string;
  signOutUrl?: string;
  isEnabledForUsers: boolean;
  isEnabledForAdmins: boolean;
  jwtSecretKey?: string;
  jwtUseHighSecurity: boolean;
  samlCertificate?: string;
  samlIdpEntityId?: string;
  magicLinkExpiresInSeconds: number;
  bruteForceProtectionMaxFailedAttempts: number;
  bruteForceProtectionWindowInSeconds: number;
};

export interface PlatformVersion {
  version: string;
}

export interface EntityRef {
  id: string;
}

export type JsonObject = Record<string, unknown>;

export interface IdResponse {
  id: string;
}

export interface TemplateVariable {
  path: string;
  description: string | null;
  example: string | null;
}

export interface TemplateVariableGroup {
  category: string;
  variables: TemplateVariable[];
}

export interface EmailTemplateDetail {
  id: string;
  subject: string;
  developerName: string;
  content: string;
  cc: string;
  bcc: string;
  availableVariables: TemplateVariableGroup[];
}

export interface WebTemplateDetail {
  id: string;
  themeId: string;
  label: string;
  developerName: string;
  content: string;
  isBaseLayout: boolean;
  isBuiltInTemplate: boolean;
  parentTemplateId: string | null;
  allowAccessForNewContentTypes: boolean;
  templateAccessToModelDefinitions: string[];
  availableVariables: TemplateVariableGroup[];
  /** Starred by the signed-in admin. */
  isFavorite: boolean;
}

export type JsonValue = string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue };

/** Mirrors `WidgetFieldType.SupportedTypes` on the server. */
export const WIDGET_FIELD_TYPES = [
  "single_line_text",
  "long_text",
  "wysiwyg",
  "number",
  "checkbox",
  "date",
  "dropdown",
  "radio",
  "color",
  "repeater",
  "image",
  "content_type",
  "view",
] as const;

export type WidgetFieldTypeName = (typeof WIDGET_FIELD_TYPES)[number];

export interface WidgetFieldChoice {
  label: string;
  developerName: string;
  disabled: boolean;
}

/** One field of a widget template's settings form, or a repeater sub-field. Mirrors `FieldDefinition`. */
export interface WidgetField {
  /** The SettingsJson key, read in Liquid as `widget.settings.<developerName>`. */
  developerName: string;
  label: string;
  fieldType: WidgetFieldTypeName;
  description: string;
  isRequired: boolean;
  /** Applied only to a newly added widget; null for none. */
  defaultValue: JsonValue;
  choices: WidgetFieldChoice[];
  subFields: WidgetField[];
  /** For a view field, the sibling content_type field whose views it lists; empty otherwise. */
  contentTypeField: string;
}

/** One entry of GET /themes/widget-field-types. */
export interface WidgetFieldTypeOption {
  developerName: WidgetFieldTypeName;
  label: string;
  hasChoices: boolean;
  allowedInRepeater: boolean;
}

export interface WidgetTemplateDetail {
  id: string;
  themeId: string;
  label: string;
  developerName: string;
  content: string;
  isBuiltInTemplate: boolean;
  fields: WidgetField[];
}

/** Body for POST /themes/{themeId}/widget-templates. */
export interface CreateWidgetTemplateInput {
  label: string;
  developerName: string;
  content: string;
  fields: WidgetField[];
}

/** Body for PUT /themes/{themeId}/widget-templates/{id}. */
export interface UpdateWidgetTemplateInput {
  label: string;
  content: string;
  fields: WidgetField[];
}

/** One widget type a site page can use: a widget template of the active theme. */
export interface SitePageWidgetDefinition {
  developerName: string;
  displayName: string;
  description: string;
  iconClass: string;
  isBuiltInTemplate: boolean;
  fields: WidgetField[];
}

export interface FunctionDetail {
  id: string;
  name: string;
  developerName: string;
  triggerType: string;
  triggerLabel: string;
  isActive: boolean;
  code: string;
  /** Public path without a leading slash; empty when the function has none. */
  routePath: string;
}

export interface MenuDetail {
  id: string;
  label: string;
  developerName: string;
  isMainMenu: boolean;
}

export interface MenuItemDetail {
  id: string;
  label: string;
  url: string;
  isDisabled: boolean;
  openInNewTab: boolean;
  cssClassName: string;
  ordinal: number;
  parentNavigationMenuItemId: string | null;
  navigationMenuId: string;
}

export interface TemplateRevision {
  id: string;
  creationTime: string;
  creatorName: string;
  subject: string;
  label: string;
  content: string;
  code: string;
}

export interface DuplicateThemeInput {
  title: string;
  developerName: string;
  description: string;
}

export interface ImportThemeFromUrlInput {
  title: string;
  developerName: string;
  description: string;
  url: string;
}

export interface ThemeMediaItem {
  id: string;
  fileName: string;
  contentType: string;
  length: number;
  objectKey: string;
  url: string;
}

export type BackgroundTaskStatusName = "enqueued" | "processing" | "complete" | "error";

export interface BackgroundTaskDetail {
  id: string;
  name: string;
  status: BackgroundTaskStatusName;
  statusLabel: string;
  statusInfo: string;
  errorMessage: string;
  percentComplete: number;
  taskStep: number;
  creationTime: string;
  lastModificationTime: string;
  completionTime: string;
}

export type RetainedLogKey = "audit_logs" | "email_logs" | "webhook_deliveries" | "background_tasks";

/** Retention windows in days; 0 keeps entries forever. */
export interface LogRetention {
  auditLogRetentionDays: number;
  emailLogRetentionDays: number;
  webhookDeliveryRetentionDays: number;
  backgroundTaskRetentionDays: number;
}

export const RETENTION_FIELD: Record<RetainedLogKey, keyof LogRetention> = {
  audit_logs: "auditLogRetentionDays",
  email_logs: "emailLogRetentionDays",
  webhook_deliveries: "webhookDeliveryRetentionDays",
  background_tasks: "backgroundTaskRetentionDays",
};

export interface RetainedLogStats {
  key: RetainedLogKey;
  label: string;
  rowCount: number;
  oldestEntry: string | null;
  /** Null when the server predates configurable retention. */
  retentionDays: number | null;
}

export interface SizeUsage {
  sizeBytes: number;
  sizeDisplay: string;
  maxBytes: number;
  maxDisplay: string;
}

export interface MaintenanceSnapshot {
  version: string;
  environment: string;
  database: SizeUsage;
  storage: SizeUsage & { provider: string; fileCount: number };
  logs: RetainedLogStats[];
  backgroundTasks: Record<BackgroundTaskStatusName, number>;
}

export interface ClearedLog {
  deleted: number | null;
}

export interface WebhookEventDescriptor {
  eventName: string;
  displayName: string;
  group: string;
}

export interface WebhookEventGroup {
  group: string;
  events: WebhookEventDescriptor[];
}

export interface SentTestEmail {
  emailAddress: string;
  sentAt: string;
}

export interface TaskMediaItem {
  id: string;
  fileName: string;
  contentType: string;
  objectKey: string;
  downloadUrl: string;
}

export type CsvImportMethod =
  | "add_new_records_only"
  | "update_existing_records_only"
  | "upsert_all_records";

export const CSV_IMPORT_METHODS: CsvImportMethod[] = [
  "add_new_records_only",
  "update_existing_records_only",
  "upsert_all_records",
];

export interface ImportContentItemsFromCsvInput {
  contentTypeId: string;
  importMethod: CsvImportMethod;
  importAsDraft: boolean;
  csvAsBytes: string;
}

export interface ExportContentItemsToCsvInput {
  exportOnlyColumnsFromView: boolean;
}

/** One content type, field, or view that a schema import created or would create/update. */
export interface SchemaChange {
  kind: "content_type" | "field" | "view";
  contentType: string;
  /** Field or view developer name; null for a content type. */
  name: string | null;
  action: "created" | "updated";
  /** For an update, the properties that differ. */
  details: string[];
}

export interface SchemaImportResult {
  dryRun: boolean;
  created: number;
  updated: number;
  unchanged: number;
  changes: SchemaChange[];
  warnings: string[];
}

export interface Me {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  permissions: string[];
  isAdmin: boolean;
}

export interface LoginResponse {
  requiresTwoFactor?: boolean;
}

export type AuthSchemeType = "email_and_password" | "magic_link" | "saml" | "oidc" | string;

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

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface MediaConfig {
  useDirectUploadToCloud: boolean;
  maxUploadBytes?: number;
}

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

export interface EmailTemplateDetail {
  id: string;
  subject: string;
  developerName: string;
  content: string;
  cc: string;
  bcc: string;
  availableVariables: string[] | null;
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
}

export interface WidgetTemplateDetail {
  id: string;
  themeId: string;
  label: string;
  developerName: string;
  content: string;
  isBuiltInTemplate: boolean;
}

export interface FunctionDetail {
  id: string;
  name: string;
  developerName: string;
  triggerType: string;
  triggerLabel: string;
  isActive: boolean;
  code: string;
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

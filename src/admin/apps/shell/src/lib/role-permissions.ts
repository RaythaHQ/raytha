import type { EntityRef, Me } from "@raytha/api";
import { entityFields, isRecord, readString } from "../pages/entity";

/**
 * Client mirror of `AdminAuthorityGuard` so the forms can explain a denial before the
 * server returns it. The server remains the control; these only decide what to disable.
 */

export const SUPER_ADMIN_ROLE = "super_admin";
export const BUILT_IN_ROLES: ReadonlySet<string> = new Set([SUPER_ADMIN_ROLE, "admin", "editor"]);

export const CONTENT_TYPES_PERMISSION = "content_types";

export type ContentAccess = "read" | "edit" | "config";

export const CONTENT_ACCESS: readonly { value: ContentAccess; label: string; description: string }[] = [
  { value: "read", label: "Read", description: "View items" },
  { value: "edit", label: "Edit", description: "Create, edit, publish, and delete items" },
  { value: "config", label: "Configure", description: "Change fields, views, and settings" },
];

export const SYSTEM_PERMISSION_DESCRIPTIONS: Readonly<Record<string, string>> = {
  system_settings: "Organization settings, SMTP, authentication methods, and maintenance.",
  administrators: "Admin accounts, roles, and API keys.",
  audit_logs: "View the audit log and email log.",
  content_types: "Create content types, and read, edit, and configure every one of them.",
  templates: "Themes, web templates, widget templates, email templates, and functions.",
  users: "Website user accounts and user groups.",
  site_pages: "Build site pages, menus, and navigation.",
  media_items: "Browse and delete files in the media library.",
};

/** The server rejects a role that holds one of these without the other. */
export const PAIRED_SYSTEM_PERMISSIONS: Readonly<Record<string, string>> = {
  system_settings: "administrators",
  administrators: "system_settings",
};

export function withImpliedRead(access: readonly string[]): ContentAccess[] {
  const set = new Set(access.filter(isContentAccess));
  if (set.has("edit") || set.has("config")) {
    set.add("read");
  }
  return CONTENT_ACCESS.map((option) => option.value).filter((value) => set.has(value));
}

export function impliedBy(access: readonly string[], value: ContentAccess): ContentAccess | null {
  if (value !== "read") {
    return null;
  }
  if (access.includes("edit")) {
    return "edit";
  }
  if (access.includes("config")) {
    return "config";
  }
  return null;
}

export type Ceiling = {
  unlimited: boolean;
  system: ReadonlySet<string>;
  contentTypes: ReadonlySet<string>;
};

export function callerCeiling(me: Me | null): Ceiling {
  return {
    unlimited: me?.roles?.includes(SUPER_ADMIN_ROLE) ?? false,
    system: new Set(me?.permissions ?? []),
    contentTypes: new Set(me?.contentTypePermissions ?? []),
  };
}

export function canGrantSystem(ceiling: Ceiling, permission: string): boolean {
  return ceiling.unlimited || ceiling.system.has(permission);
}

export function canGrantContent(ceiling: Ceiling, contentTypeDeveloperName: string, access: ContentAccess): boolean {
  return (
    ceiling.unlimited ||
    ceiling.system.has(CONTENT_TYPES_PERMISSION) ||
    ceiling.contentTypes.has(`${contentTypeDeveloperName}_${access}`)
  );
}

export type RoleSummary = {
  id: string;
  label: string;
  developerName: string;
  systemPermissions: string[];
  /** Keyed by content type developer name. */
  contentTypes: Record<string, string[]>;
};

export function readRoleSummaries(value: unknown): RoleSummary[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value
    .filter((item): item is EntityRef => isRecord(item) && typeof item.id === "string")
    .map(readRoleSummary);
}

export function readRoleSummary(entity: EntityRef): RoleSummary {
  const fields = entityFields(entity);
  const friendly = fields.contentTypePermissionsFriendlyNames;
  const contentTypes: Record<string, string[]> = {};
  if (isRecord(friendly)) {
    for (const [developerName, access] of Object.entries(friendly)) {
      if (Array.isArray(access)) {
        contentTypes[developerName] = withImpliedRead(access.filter((item): item is string => typeof item === "string"));
      }
    }
  }
  const system = Array.isArray(fields.systemPermissions)
    ? fields.systemPermissions.filter((item): item is string => typeof item === "string")
    : [];
  return {
    id: entity.id,
    label: readString(fields, "label") || readString(fields, "developerName"),
    developerName: readString(fields, "developerName"),
    systemPermissions: system,
    contentTypes,
  };
}

export function isSuperAdminRole(role: { developerName: string }): boolean {
  return role.developerName === SUPER_ADMIN_ROLE;
}

export function isBuiltInRole(developerName: string): boolean {
  return BUILT_IN_ROLES.has(developerName);
}

export function roleWithinCeiling(ceiling: Ceiling, role: RoleSummary): boolean {
  if (ceiling.unlimited) {
    return true;
  }
  if (role.systemPermissions.some((permission) => !ceiling.system.has(permission))) {
    return false;
  }
  return Object.entries(role.contentTypes).every(([developerName, access]) =>
    withImpliedRead(access).every((value) => canGrantContent(ceiling, developerName, value)),
  );
}

export function roleAssignmentBlock(ceiling: Ceiling, role: RoleSummary): string | null {
  if (isSuperAdminRole(role) && !ceiling.unlimited) {
    return "Only a super admin can assign or remove the Super Admin role.";
  }
  if (!roleWithinCeiling(ceiling, role)) {
    return "Grants permissions you do not have.";
  }
  return null;
}

export function roleEditBlock(ceiling: Ceiling, role: RoleSummary): string | null {
  if (isSuperAdminRole(role)) {
    return "The Super Admin role always holds every permission. It cannot be edited or deleted, and only a super admin can assign it.";
  }
  if (!roleWithinCeiling(ceiling, role)) {
    return `You cannot edit the ${role.label} role because it grants permissions you do not have.`;
  }
  return null;
}

export function accountManageBlock(ceiling: Ceiling, roles: readonly RoleSummary[]): string | null {
  if (ceiling.unlimited) {
    return null;
  }
  if (roles.some(isSuperAdminRole)) {
    return "Only a super admin can manage an account that holds the Super Admin role.";
  }
  if (roles.some((role) => !roleWithinCeiling(ceiling, role))) {
    return "This administrator has permissions you do not have, so you cannot edit, suspend, reset, or delete them.";
  }
  return null;
}

function isContentAccess(value: string): value is ContentAccess {
  return value === "read" || value === "edit" || value === "config";
}

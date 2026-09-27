START TRANSACTION;
ALTER TABLE "OrganizationSettings" ADD "AuditLogRetentionDays" integer NOT NULL DEFAULT 180;

ALTER TABLE "OrganizationSettings" ADD "BackgroundTaskRetentionDays" integer NOT NULL DEFAULT 180;

ALTER TABLE "OrganizationSettings" ADD "EmailLogRetentionDays" integer NOT NULL DEFAULT 180;

ALTER TABLE "OrganizationSettings" ADD "WebhookDeliveryRetentionDays" integer NOT NULL DEFAULT 180;

UPDATE "Roles" SET "SystemPermissions" = "SystemPermissions" | 128 WHERE ("SystemPermissions" & 5) <> 0 OR EXISTS (SELECT 1 FROM "ContentTypeRolePermission" p WHERE p."RoleId" = "Roles"."Id" AND (p."ContentTypePermissions" & 2) <> 0);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260926222411_v2_2_0', '10.0.0');

COMMIT;


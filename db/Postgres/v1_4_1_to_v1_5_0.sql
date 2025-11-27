BEGIN;

-- Create SitePages table
CREATE TABLE "SitePages" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "IsPublished" boolean NOT NULL,
    "IsDraft" boolean NOT NULL,
    "RouteId" uuid NOT NULL,
    "WebTemplateId" uuid NOT NULL,
    "_WidgetsJson" text NOT NULL DEFAULT '{}',
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone NULL,
    "CreatorUserId" uuid NULL,
    "LastModifierUserId" uuid NULL,
    CONSTRAINT "PK_SitePages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SitePages_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SitePages_WebTemplates_WebTemplateId" FOREIGN KEY ("WebTemplateId") REFERENCES "WebTemplates" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SitePages_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_SitePages_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id")
);

-- Add SitePageId column to Routes table
ALTER TABLE "Routes" ADD "SitePageId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

-- Create indexes for SitePages
CREATE UNIQUE INDEX "IX_SitePages_RouteId" ON "SitePages" ("RouteId");
CREATE INDEX "IX_SitePages_WebTemplateId" ON "SitePages" ("WebTemplateId");
CREATE INDEX "IX_SitePages_CreatorUserId" ON "SitePages" ("CreatorUserId");
CREATE INDEX "IX_SitePages_LastModifierUserId" ON "SitePages" ("LastModifierUserId");

-- Create WidgetTemplates table
CREATE TABLE "WidgetTemplates" (
    "Id" uuid NOT NULL,
    "ThemeId" uuid NOT NULL,
    "Label" text NULL,
    "DeveloperName" character varying(450) NULL,
    "Content" text NULL,
    "IsBuiltInTemplate" boolean NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone NULL,
    "CreatorUserId" uuid NULL,
    "LastModifierUserId" uuid NULL,
    CONSTRAINT "PK_WidgetTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WidgetTemplates_Themes_ThemeId" FOREIGN KEY ("ThemeId") REFERENCES "Themes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WidgetTemplates_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_WidgetTemplates_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id")
);

-- Create WidgetTemplateRevisions table
CREATE TABLE "WidgetTemplateRevisions" (
    "Id" uuid NOT NULL,
    "Label" text NULL,
    "Content" text NULL,
    "WidgetTemplateId" uuid NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone NULL,
    "CreatorUserId" uuid NULL,
    "LastModifierUserId" uuid NULL,
    CONSTRAINT "PK_WidgetTemplateRevisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId" FOREIGN KEY ("WidgetTemplateId") REFERENCES "WidgetTemplates" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WidgetTemplateRevisions_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_WidgetTemplateRevisions_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id")
);

-- Create indexes for WidgetTemplates
CREATE INDEX "IX_WidgetTemplates_ThemeId" ON "WidgetTemplates" ("ThemeId");
CREATE UNIQUE INDEX "IX_WidgetTemplates_DeveloperName_ThemeId" ON "WidgetTemplates" ("DeveloperName", "ThemeId");
CREATE INDEX "IX_WidgetTemplates_CreatorUserId" ON "WidgetTemplates" ("CreatorUserId");
CREATE INDEX "IX_WidgetTemplates_LastModifierUserId" ON "WidgetTemplates" ("LastModifierUserId");

-- Create indexes for WidgetTemplateRevisions
CREATE INDEX "IX_WidgetTemplateRevisions_WidgetTemplateId" ON "WidgetTemplateRevisions" ("WidgetTemplateId");
CREATE INDEX "IX_WidgetTemplateRevisions_CreatorUserId" ON "WidgetTemplateRevisions" ("CreatorUserId");
CREATE INDEX "IX_WidgetTemplateRevisions_LastModifierUserId" ON "WidgetTemplateRevisions" ("LastModifierUserId");

-- Update super_admin and admin roles to include ManageSitePages permission (64)
UPDATE "Roles"
SET "SystemPermissions" = "SystemPermissions" | 64
WHERE "DeveloperName" IN ('super_admin', 'admin');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241127000000_v1_5_0', '8.0.10');

COMMIT;


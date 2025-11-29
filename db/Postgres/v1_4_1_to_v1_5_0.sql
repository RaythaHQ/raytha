START TRANSACTION;
ALTER TABLE "Routes" ADD "SitePageId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE "AuthenticationSchemes" ADD "BruteForceProtectionMaxFailedAttempts" integer NOT NULL DEFAULT 0;

ALTER TABLE "AuthenticationSchemes" ADD "BruteForceProtectionWindowInSeconds" integer NOT NULL DEFAULT 0;

CREATE TABLE "FailedLoginAttempts" (
    "Id" uuid NOT NULL,
    "EmailAddress" text NOT NULL,
    "FailedAttemptCount" integer NOT NULL,
    "LastFailedAttemptAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_FailedLoginAttempts" PRIMARY KEY ("Id")
);

CREATE TABLE "SitePages" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "IsPublished" boolean NOT NULL,
    "IsDraft" boolean NOT NULL,
    "RouteId" uuid NOT NULL,
    "WebTemplateId" uuid NOT NULL,
    "_DraftWidgetsJson" jsonb,
    "_PublishedWidgetsJson" jsonb,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone,
    "CreatorUserId" uuid,
    "LastModifierUserId" uuid,
    CONSTRAINT "PK_SitePages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SitePages_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id"),
    CONSTRAINT "FK_SitePages_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_SitePages_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_SitePages_WebTemplates_WebTemplateId" FOREIGN KEY ("WebTemplateId") REFERENCES "WebTemplates" ("Id") ON DELETE CASCADE
);

CREATE TABLE "WidgetTemplates" (
    "Id" uuid NOT NULL,
    "ThemeId" uuid NOT NULL,
    "Label" text,
    "DeveloperName" text,
    "Content" text,
    "IsBuiltInTemplate" boolean NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone,
    "CreatorUserId" uuid,
    "LastModifierUserId" uuid,
    CONSTRAINT "PK_WidgetTemplates" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WidgetTemplates_Themes_ThemeId" FOREIGN KEY ("ThemeId") REFERENCES "Themes" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WidgetTemplates_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_WidgetTemplates_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id")
);

CREATE TABLE "SitePageRevisions" (
    "Id" uuid NOT NULL,
    "_PublishedWidgetsJson" jsonb,
    "SitePageId" uuid NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone,
    "CreatorUserId" uuid,
    "LastModifierUserId" uuid,
    CONSTRAINT "PK_SitePageRevisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SitePageRevisions_SitePages_SitePageId" FOREIGN KEY ("SitePageId") REFERENCES "SitePages" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SitePageRevisions_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_SitePageRevisions_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id")
);

CREATE TABLE "WidgetTemplateRevisions" (
    "Id" uuid NOT NULL,
    "Label" text,
    "Content" text,
    "WidgetTemplateId" uuid NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone,
    "CreatorUserId" uuid,
    "LastModifierUserId" uuid,
    CONSTRAINT "PK_WidgetTemplateRevisions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WidgetTemplateRevisions_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_WidgetTemplateRevisions_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId" FOREIGN KEY ("WidgetTemplateId") REFERENCES "WidgetTemplates" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_FailedLoginAttempts_EmailAddress" ON "FailedLoginAttempts" ("EmailAddress");

CREATE INDEX "IX_SitePageRevisions_CreatorUserId" ON "SitePageRevisions" ("CreatorUserId");

CREATE INDEX "IX_SitePageRevisions_LastModifierUserId" ON "SitePageRevisions" ("LastModifierUserId");

CREATE INDEX "IX_SitePageRevisions_SitePageId" ON "SitePageRevisions" ("SitePageId");

CREATE INDEX "IX_SitePages_CreatorUserId" ON "SitePages" ("CreatorUserId");

CREATE INDEX "IX_SitePages_LastModifierUserId" ON "SitePages" ("LastModifierUserId");

CREATE UNIQUE INDEX "IX_SitePages_RouteId" ON "SitePages" ("RouteId");

CREATE INDEX "IX_SitePages_WebTemplateId" ON "SitePages" ("WebTemplateId");

CREATE INDEX "IX_WidgetTemplateRevisions_CreatorUserId" ON "WidgetTemplateRevisions" ("CreatorUserId");

CREATE INDEX "IX_WidgetTemplateRevisions_LastModifierUserId" ON "WidgetTemplateRevisions" ("LastModifierUserId");

CREATE INDEX "IX_WidgetTemplateRevisions_WidgetTemplateId" ON "WidgetTemplateRevisions" ("WidgetTemplateId");

CREATE INDEX "IX_WidgetTemplates_CreatorUserId" ON "WidgetTemplates" ("CreatorUserId");

CREATE UNIQUE INDEX "IX_WidgetTemplates_DeveloperName_ThemeId" ON "WidgetTemplates" ("DeveloperName", "ThemeId");

CREATE INDEX "IX_WidgetTemplates_LastModifierUserId" ON "WidgetTemplates" ("LastModifierUserId");

CREATE INDEX "IX_WidgetTemplates_ThemeId" ON "WidgetTemplates" ("ThemeId");


                UPDATE "Roles"
                SET "SystemPermissions" = "SystemPermissions" | 64
                WHERE "DeveloperName" IN ('super_admin', 'admin', 'editor');
            

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251129000211_v1_5_0', '10.0.0');

COMMIT;


START TRANSACTION;
CREATE TABLE "EmailLogs" (
    "Id" uuid NOT NULL,
    "ToAddress" text NOT NULL,
    "FromAddress" text NOT NULL,
    "Subject" text NOT NULL,
    "Body" text NOT NULL,
    "IsHtml" boolean NOT NULL,
    "IsSuccess" boolean NOT NULL,
    "ErrorMessage" text,
    "DurationMs" bigint NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_EmailLogs" PRIMARY KEY ("Id")
);

CREATE TABLE "Webhooks" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "Url" text NOT NULL,
    "Description" text,
    "Secret" text NOT NULL,
    "IsActive" boolean NOT NULL,
    "SubscribedEvents" jsonb NOT NULL,
    "MaxAttempts" integer NOT NULL,
    "TimeoutSeconds" integer NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone,
    "CreatorUserId" uuid,
    "LastModifierUserId" uuid,
    CONSTRAINT "PK_Webhooks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Webhooks_Users_CreatorUserId" FOREIGN KEY ("CreatorUserId") REFERENCES "Users" ("Id"),
    CONSTRAINT "FK_Webhooks_Users_LastModifierUserId" FOREIGN KEY ("LastModifierUserId") REFERENCES "Users" ("Id")
);

CREATE TABLE "WebhookDeliveries" (
    "Id" uuid NOT NULL,
    "WebhookId" uuid NOT NULL,
    "EventName" text NOT NULL,
    "Payload" text NOT NULL,
    "Status" text NOT NULL,
    "AttemptCount" integer NOT NULL,
    "LastAttemptAt" timestamp with time zone,
    "NextRetryAt" timestamp with time zone,
    "ResponseCode" integer,
    "ResponseBody" text,
    "ErrorMessage" text,
    "DurationMs" bigint,
    "CreationTime" timestamp with time zone NOT NULL,
    "CompletionTime" timestamp with time zone,
    CONSTRAINT "PK_WebhookDeliveries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WebhookDeliveries_Webhooks_WebhookId" FOREIGN KEY ("WebhookId") REFERENCES "Webhooks" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EmailLogs_CreationTime" ON "EmailLogs" ("CreationTime");

CREATE INDEX "IX_EmailLogs_IsSuccess" ON "EmailLogs" ("IsSuccess");

CREATE INDEX "IX_EmailLogs_ToAddress" ON "EmailLogs" ("ToAddress");

CREATE INDEX "IX_WebhookDeliveries_CreationTime" ON "WebhookDeliveries" ("CreationTime");

CREATE INDEX "IX_WebhookDeliveries_EventName" ON "WebhookDeliveries" ("EventName");

CREATE INDEX "IX_WebhookDeliveries_WebhookId" ON "WebhookDeliveries" ("WebhookId");

CREATE INDEX "IX_Webhooks_CreatorUserId" ON "Webhooks" ("CreatorUserId");

CREATE INDEX "IX_Webhooks_IsActive" ON "Webhooks" ("IsActive");

CREATE INDEX "IX_Webhooks_LastModifierUserId" ON "Webhooks" ("LastModifierUserId");

ALTER TABLE "OrganizationSettings" ADD "AuditLogRetentionDays" integer NOT NULL DEFAULT 180;

ALTER TABLE "OrganizationSettings" ADD "BackgroundTaskRetentionDays" integer NOT NULL DEFAULT 180;

ALTER TABLE "OrganizationSettings" ADD "EmailLogRetentionDays" integer NOT NULL DEFAULT 180;

ALTER TABLE "OrganizationSettings" ADD "WebhookDeliveryRetentionDays" integer NOT NULL DEFAULT 180;

UPDATE "Roles" SET "SystemPermissions" = "SystemPermissions" | 128
WHERE ("SystemPermissions" & 5) <> 0
   OR EXISTS (
       SELECT 1 FROM "ContentTypeRolePermission" p
       WHERE p."RoleId" = "Roles"."Id" AND (p."ContentTypePermissions" & 2) <> 0);

ALTER TABLE "WidgetTemplates" ADD "_FieldsJson" jsonb NOT NULL DEFAULT '[]';

ALTER TABLE "WidgetTemplateRevisions" ADD "_FieldsJson" jsonb NOT NULL DEFAULT '[]';

ALTER TABLE "Routes" ADD "RaythaFunctionId" uuid;

ALTER TABLE "RaythaFunctions" ADD "RouteId" uuid;

ALTER TABLE "ContentTypeFields" ADD "_SubFieldsJson" jsonb NOT NULL DEFAULT '[]';

CREATE TABLE "UserWebTemplate" (
    "FavoriteWebTemplatesId" uuid NOT NULL,
    "UserFavoritesId" uuid NOT NULL,
    CONSTRAINT "PK_UserWebTemplate" PRIMARY KEY ("FavoriteWebTemplatesId", "UserFavoritesId"),
    CONSTRAINT "FK_UserWebTemplate_Users_UserFavoritesId" FOREIGN KEY ("UserFavoritesId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserWebTemplate_WebTemplates_FavoriteWebTemplatesId" FOREIGN KEY ("FavoriteWebTemplatesId") REFERENCES "WebTemplates" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_RaythaFunctions_RouteId" ON "RaythaFunctions" ("RouteId");

CREATE INDEX "IX_UserWebTemplate_UserFavoritesId" ON "UserWebTemplate" ("UserFavoritesId");

ALTER TABLE "RaythaFunctions" ADD CONSTRAINT "FK_RaythaFunctions_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE SET NULL;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundImage","label":"Background image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#1e293b","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"textColor","label":"Text color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#ffffff","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"light","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"alignment","label":"Alignment","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"center","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Center","developerName":"center","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"minHeight","label":"Minimum height","fieldType":"number","description":"In pixels.","isRequired":false,"defaultValue":400,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'hero' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundImage","label":"Background image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#1e293b","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"textColor","label":"Text color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#ffffff","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"light","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"alignment","label":"Alignment","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"center","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Center","developerName":"center","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"minHeight","label":"Minimum height","fieldType":"number","description":"In pixels.","isRequired":false,"defaultValue":400,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'hero' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"padding","label":"Padding","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"medium","choices":[{"label":"None","developerName":"none","disabled":false},{"label":"Small","developerName":"small","disabled":false},{"label":"Medium","developerName":"medium","disabled":false},{"label":"Large","developerName":"large","disabled":false}],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'wysiwyg' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"padding","label":"Padding","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"medium","choices":[{"label":"None","developerName":"none","disabled":false},{"label":"Small","developerName":"small","disabled":false},{"label":"Medium","developerName":"medium","disabled":false},{"label":"Large","developerName":"large","disabled":false}],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'wysiwyg' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"imageUrl","label":"Image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageAlt","label":"Image alt text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imagePosition","label":"Image position","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"left","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"primary","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'imagetext' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"imageUrl","label":"Image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageAlt","label":"Image alt text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imagePosition","label":"Image position","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"left","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"primary","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'imagetext' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"title","label":"Title","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"description","label":"Description","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageUrl","label":"Image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageAlt","label":"Image alt text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"primary","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'card' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"title","label":"Title","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"description","label":"Description","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageUrl","label":"Image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageAlt","label":"Image alt text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"primary","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'card' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"expandFirst","label":"Expand first item","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"items","label":"Questions","fieldType":"repeater","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[{"developerName":"question","label":"Question","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"answer","label":"Answer","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'faq' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"expandFirst","label":"Expand first item","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"items","label":"Questions","fieldType":"repeater","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[{"developerName":"question","label":"Question","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"answer","label":"Answer","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'faq' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"light","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#0d6efd","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"textColor","label":"Text color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#ffffff","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"alignment","label":"Alignment","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"center","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Center","developerName":"center","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'cta' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"light","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#0d6efd","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"textColor","label":"Text color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#ffffff","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"alignment","label":"Alignment","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"center","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Center","developerName":"center","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'cta' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"embedType","label":"Embed type","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"iframe","choices":[{"label":"Iframe","developerName":"iframe","disabled":false},{"label":"HTML","developerName":"html","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"iframeUrl","label":"Iframe URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"htmlContent","label":"Embed HTML","fieldType":"long_text","description":"Rendered as raw HTML, scripts included.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"aspectRatio","label":"Aspect ratio","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"16x9","choices":[{"label":"16:9","developerName":"16x9","disabled":false},{"label":"4:3","developerName":"4x3","disabled":false},{"label":"1:1","developerName":"1x1","disabled":false},{"label":"21:9","developerName":"21x9","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"maxWidth","label":"Max width","fieldType":"number","description":"In pixels. Leave empty for full width.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"caption","label":"Caption","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'embed' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"embedType","label":"Embed type","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"iframe","choices":[{"label":"Iframe","developerName":"iframe","disabled":false},{"label":"HTML","developerName":"html","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"iframeUrl","label":"Iframe URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"htmlContent","label":"Embed HTML","fieldType":"long_text","description":"Rendered as raw HTML, scripts included.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"aspectRatio","label":"Aspect ratio","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"16x9","choices":[{"label":"16:9","developerName":"16x9","disabled":false},{"label":"4:3","developerName":"4x3","disabled":false},{"label":"1:1","developerName":"1x1","disabled":false},{"label":"21:9","developerName":"21x9","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"maxWidth","label":"Max width","fieldType":"number","description":"In pixels. Leave empty for full width.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"caption","label":"Caption","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'embed' AND r."_FieldsJson" = '[]'::jsonb;

UPDATE "WidgetTemplates" SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"contentType","label":"Content type","fieldType":"content_type","description":null,"isRequired":true,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"viewId","label":"View","fieldType":"view","description":"Leave empty for the default view.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":"contentType"},{"developerName":"filter","label":"Filter","fieldType":"single_line_text","description":"OData filter expression.","isRequired":false,"defaultValue":"IsPublished eq \u0027true\u0027","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"orderBy","label":"Order by","fieldType":"single_line_text","description":"OData order by expression. Overrides the view\u0027s sort.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"pageSize","label":"Page size","fieldType":"number","description":null,"isRequired":false,"defaultValue":3,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"displayStyle","label":"Display style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"cards","choices":[{"label":"Cards","developerName":"cards","disabled":false},{"label":"List","developerName":"list","disabled":false},{"label":"Compact","developerName":"compact","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"showImage","label":"Show image","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"showDate","label":"Show date","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"showExcerpt","label":"Show excerpt","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"linkText","label":"View all text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"linkUrl","label":"View all URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
WHERE "DeveloperName" = 'contentlist' AND "_FieldsJson" = '[]'::jsonb;
UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"contentType","label":"Content type","fieldType":"content_type","description":null,"isRequired":true,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"viewId","label":"View","fieldType":"view","description":"Leave empty for the default view.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":"contentType"},{"developerName":"filter","label":"Filter","fieldType":"single_line_text","description":"OData filter expression.","isRequired":false,"defaultValue":"IsPublished eq \u0027true\u0027","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"orderBy","label":"Order by","fieldType":"single_line_text","description":"OData order by expression. Overrides the view\u0027s sort.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"pageSize","label":"Page size","fieldType":"number","description":null,"isRequired":false,"defaultValue":3,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"displayStyle","label":"Display style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"cards","choices":[{"label":"Cards","developerName":"cards","disabled":false},{"label":"List","developerName":"list","disabled":false},{"label":"Compact","developerName":"compact","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"showImage","label":"Show image","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"showDate","label":"Show date","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"showExcerpt","label":"Show excerpt","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"linkText","label":"View all text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"linkUrl","label":"View all URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]'::jsonb
FROM "WidgetTemplates" AS t
WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = 'contentlist' AND r."_FieldsJson" = '[]'::jsonb;

INSERT INTO "EmailTemplateRevisions" ("Id", "Subject", "Content", "Cc", "Bcc", "EmailTemplateId", "CreationTime")
SELECT gen_random_uuid(), "Subject", "Content", "Cc", "Bcc", "Id", now()
FROM "EmailTemplates"
WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;

UPDATE "EmailTemplates" SET "Content" = '<p>Hello {{ Target.FirstName }},</p>

<p>Use this one-time code to login to the {{ CurrentOrganization.OrganizationName }} website.</p>

<p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">{{ Target.Code }}</p>

<p>The code expires after {{ Target.MagicLinkExpiresInSeconds }} seconds. If you did not request it, you can ignore this email.</p>

<p>Thank you,<br/>
  {{ CurrentOrganization.OrganizationName }}</p>', "LastModificationTime" = now()
WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;

INSERT INTO "WebTemplateRevisions" ("Id", "Label", "Content", "WebTemplateId", "AllowAccessForNewContentTypes", "CreationTime")
SELECT gen_random_uuid(), "Label", "Content", "Id", "AllowAccessForNewContentTypes", now()
FROM "WebTemplates"
WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;

UPDATE "WebTemplates" SET "Content" = '<h3>Enter your code</h3>
<p>We emailed you a one-time code. Enter it below to login.</p>
{% if Target.ValidationFailures["__ValidationSummary"] %}
  <div class="alert alert-danger">
    {{ Target.ValidationFailures["__ValidationSummary"] }}
  </div>
{% endif %}
<form
  action="{{ PathBase }}/account/login/magic-link/complete?returnUrl={{ Target.ReturnUrl }}"
  method="post">
  <div class="mb-3">
    <label for="email" class="form-label">Email address</label>
    <input
      id="email"
      type="email"
      name="EmailAddress"
      value="{{ Target.EmailAddress }}"
      class="form-control">
  </div>
  <div class="mb-3">
    <label for="code" class="form-label">One-time code</label>
    <input
      id="code"
      type="text"
      name="Code"
      inputmode="numeric"
      autocomplete="one-time-code"
      class="form-control">
  </div>
  <div>
    <button type="submit" class="btn btn-primary w-100">Login</button>
  </div>
  <input
    name="__RequestVerificationToken"
    type="hidden"
    value="{{ Target.RequestVerificationToken }}" />
</form>', "LastModificationTime" = now()
WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;

CREATE TEMP TABLE raytha_legacy_dates AS
WITH stored AS (
    SELECT 'published' AS source, ci."Id" AS row_id, f."Id" AS field_id, f."DeveloperName" AS field,
           ci."_PublishedContent" ->> f."DeveloperName" AS raw
    FROM "ContentItems" ci
    JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
    UNION ALL
    SELECT 'draft', ci."Id", f."Id", f."DeveloperName",
           NULLIF(ci."_DraftContent", '')::jsonb ->> f."DeveloperName"
    FROM "ContentItems" ci
    JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
    UNION ALL
    SELECT 'revision', r."Id", f."Id", f."DeveloperName",
           NULLIF(r."_PublishedContent", '')::jsonb ->> f."DeveloperName"
    FROM "ContentItemRevisions" r
    JOIN "ContentItems" ci ON ci."Id" = r."ContentItemId"
    JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
    UNION ALL
    SELECT 'deleted', d."Id", f."Id", f."DeveloperName",
           NULLIF(d."_PublishedContent", '')::jsonb ->> f."DeveloperName"
    FROM "DeletedContentItems" d
    JOIN "ContentTypeFields" f ON f."ContentTypeId" = d."ContentTypeId" AND f."FieldType" = 'date'
)
SELECT source, row_id, field_id, field, raw,
       regexp_match(raw, '^[[:space:]\u00A0\u202F]*([0-9]{1,2})[/.-]([0-9]{1,2})[/.-]([0-9]{4})(?:[[:space:]\u00A0\u202F]+([0-9]{1,2}):([0-9]{2})(?::([0-9]{2}))?(?:[[:space:]\u00A0\u202F]*([AaPp])[Mm])?)?[[:space:]\u00A0\u202F]*$') AS parts
FROM stored
WHERE raw IS NOT NULL
  AND btrim(raw) <> ''
  AND raw !~ '^[0-9]{4}-[0-9]{2}-[0-9]{2}';

CREATE TEMP TABLE raytha_legacy_date_order AS
SELECT field_id,
       bool_or(parts[1]::int > 12) AS day_first,
       bool_or(parts[2]::int > 12) AS month_first
FROM raytha_legacy_dates
WHERE parts IS NOT NULL
GROUP BY field_id;

CREATE TEMP TABLE raytha_legacy_date_iso AS
WITH oriented AS (
    SELECT d.source, d.row_id, d.field_id, d.field, d.parts,
           CASE WHEN NOT (o.day_first AND o.month_first) THEN o.day_first
                WHEN d.parts[1]::int > 12 THEN true
                WHEN d.parts[2]::int > 12 THEN false
           END AS day_first
    FROM raytha_legacy_dates d
    JOIN raytha_legacy_date_order o ON o.field_id = d.field_id
    WHERE d.parts IS NOT NULL
),
split AS (
    SELECT source, row_id, field_id, field,
           parts[3]::int AS y,
           CASE WHEN day_first THEN parts[2]::int ELSE parts[1]::int END AS m,
           CASE WHEN day_first THEN parts[1]::int ELSE parts[2]::int END AS d,
           CASE
               WHEN parts[4] IS NULL THEN 0
               WHEN parts[7] IS NULL THEN parts[4]::int
               WHEN upper(parts[7]) = 'A' THEN parts[4]::int % 12
               ELSE parts[4]::int % 12 + 12
           END AS h,
           COALESCE(parts[5]::int, 0) AS mi,
           COALESCE(parts[6]::int, 0) AS s,
           parts[7] IS NOT NULL AND parts[4]::int NOT BETWEEN 1 AND 12 AS bad_clock
    FROM oriented
    WHERE day_first IS NOT NULL
),
valid AS (
    SELECT * FROM split
    WHERE y > 0 AND m BETWEEN 1 AND 12 AND h BETWEEN 0 AND 23 AND mi BETWEEN 0 AND 59
      AND s BETWEEN 0 AND 59 AND NOT bad_clock
      AND d BETWEEN 1 AND CASE WHEN y > 0 AND m BETWEEN 1 AND 12
          THEN extract(day FROM make_date(y, m, 1) + interval '1 month - 1 day')::int END
)
SELECT source, row_id, field_id, field,
       to_char(make_date(y, m, d), 'YYYY-MM-DD')
       || CASE WHEN h = 0 AND mi = 0 AND s = 0 THEN ''
               ELSE 'T' || to_char(make_time(h, mi, s), 'HH24:MI:SS') END AS iso
FROM valid;

CREATE TEMP TABLE raytha_legacy_date_patch AS
SELECT source, row_id, jsonb_object_agg(field, iso) AS patch
FROM raytha_legacy_date_iso
GROUP BY source, row_id;

UPDATE "ContentItems" ci
SET "_PublishedContent" = ci."_PublishedContent" || p.patch
FROM raytha_legacy_date_patch p
WHERE p.source = 'published' AND p.row_id = ci."Id";

UPDATE "ContentItems" ci
SET "_DraftContent" = (ci."_DraftContent"::jsonb || p.patch)::text
FROM raytha_legacy_date_patch p
WHERE p.source = 'draft' AND p.row_id = ci."Id";

UPDATE "ContentItemRevisions" r
SET "_PublishedContent" = (r."_PublishedContent"::jsonb || p.patch)::text
FROM raytha_legacy_date_patch p
WHERE p.source = 'revision' AND p.row_id = r."Id";

UPDATE "DeletedContentItems" d
SET "_PublishedContent" = (d."_PublishedContent"::jsonb || p.patch)::text
FROM raytha_legacy_date_patch p
WHERE p.source = 'deleted' AND p.row_id = d."Id";

DO $$
DECLARE
    skipped record;
BEGIN
    FOR skipped IN
        SELECT t."DeveloperName" AS content_type, f."DeveloperName" AS field,
               CASE WHEN d.parts IS NULL THEN 'unrecognized format'
                    WHEN o.day_first AND o.month_first AND d.parts[1]::int <= 12 AND d.parts[2]::int <= 12
                        THEN 'ambiguous day/month in a mixed-order field'
                    ELSE 'not a valid date' END AS reason,
               count(*) AS value_count
        FROM raytha_legacy_dates d
        JOIN "ContentTypeFields" f ON f."Id" = d.field_id
        JOIN "ContentTypes" t ON t."Id" = f."ContentTypeId"
        LEFT JOIN raytha_legacy_date_order o ON o.field_id = d.field_id
        WHERE NOT EXISTS (
            SELECT 1 FROM raytha_legacy_date_iso i
            WHERE i.source = d.source AND i.row_id = d.row_id AND i.field_id = d.field_id
        )
        GROUP BY 1, 2, 3
    LOOP
        RAISE NOTICE 'Date values left unchanged in %.%: % (% values)',
            skipped.content_type, skipped.field, skipped.reason, skipped.value_count;
    END LOOP;
END
$$;

DROP TABLE raytha_legacy_date_patch;
DROP TABLE raytha_legacy_date_iso;
DROP TABLE raytha_legacy_date_order;
DROP TABLE raytha_legacy_dates;

ALTER TABLE "AuditLogs" ADD "ImpersonatorEmail" text;

CREATE FUNCTION pg_temp.raytha_relative_media_urls(input text) RETURNS text
LANGUAGE plpgsql AS $fn$
DECLARE
    found text[];
    result text := input;
BEGIN
    FOR found IN
        SELECT regexp_matches(
            input,
            '(https?://[A-Za-z0-9.-]+(?::[0-9]+)?)((?:/[A-Za-z0-9._~-]+)*/(?:raytha/media-items/objectkey|_static-files)/([A-Za-z0-9._~-]+))',
            'g')
    LOOP
        IF EXISTS (SELECT 1 FROM "MediaItems" WHERE "ObjectKey" = found[3]) THEN
            result := replace(result, found[1] || found[2], found[2]);
        END IF;
    END LOOP;
    RETURN result;
END
$fn$;

UPDATE "ContentItems"
SET "_PublishedContent" = pg_temp.raytha_relative_media_urls("_PublishedContent"::text)::jsonb
WHERE "_PublishedContent"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

UPDATE "ContentItems"
SET "_DraftContent" = pg_temp.raytha_relative_media_urls("_DraftContent")
WHERE "_DraftContent" ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

UPDATE "ContentItemRevisions"
SET "_PublishedContent" = pg_temp.raytha_relative_media_urls("_PublishedContent")
WHERE "_PublishedContent" ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

UPDATE "DeletedContentItems"
SET "_PublishedContent" = pg_temp.raytha_relative_media_urls("_PublishedContent")
WHERE "_PublishedContent" ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

UPDATE "SitePages"
SET "_PublishedWidgetsJson" = pg_temp.raytha_relative_media_urls("_PublishedWidgetsJson"::text)::jsonb
WHERE "_PublishedWidgetsJson"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

UPDATE "SitePages"
SET "_DraftWidgetsJson" = pg_temp.raytha_relative_media_urls("_DraftWidgetsJson"::text)::jsonb
WHERE "_DraftWidgetsJson"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

UPDATE "SitePageRevisions"
SET "_PublishedWidgetsJson" = pg_temp.raytha_relative_media_urls("_PublishedWidgetsJson"::text)::jsonb
WHERE "_PublishedWidgetsJson"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

DROP FUNCTION pg_temp.raytha_relative_media_urls(text);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260921002825_v2_0_0', '10.0.11');

COMMIT;


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

CREATE TABLE "FeatureFlags" (
    "Id" uuid NOT NULL,
    "Key" text NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "Scope" text NOT NULL,
    "CreationTime" timestamp with time zone NOT NULL,
    "LastModificationTime" timestamp with time zone,
    CONSTRAINT "PK_FeatureFlags" PRIMARY KEY ("Id")
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

CREATE UNIQUE INDEX "IX_FeatureFlags_Scope_Key" ON "FeatureFlags" ("Scope", "Key");

CREATE INDEX "IX_WebhookDeliveries_CreationTime" ON "WebhookDeliveries" ("CreationTime");

CREATE INDEX "IX_WebhookDeliveries_EventName" ON "WebhookDeliveries" ("EventName");

CREATE INDEX "IX_WebhookDeliveries_WebhookId" ON "WebhookDeliveries" ("WebhookId");

CREATE INDEX "IX_Webhooks_CreatorUserId" ON "Webhooks" ("CreatorUserId");

CREATE INDEX "IX_Webhooks_IsActive" ON "Webhooks" ("IsActive");

CREATE INDEX "IX_Webhooks_LastModifierUserId" ON "Webhooks" ("LastModifierUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260921002825_v2_0_0', '10.0.0');

COMMIT;


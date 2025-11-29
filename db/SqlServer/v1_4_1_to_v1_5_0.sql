BEGIN TRANSACTION;
ALTER TABLE [Routes] ADD [SitePageId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

ALTER TABLE [AuthenticationSchemes] ADD [BruteForceProtectionMaxFailedAttempts] int NOT NULL DEFAULT 0;

ALTER TABLE [AuthenticationSchemes] ADD [BruteForceProtectionWindowInSeconds] int NOT NULL DEFAULT 0;

CREATE TABLE [FailedLoginAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [EmailAddress] nvarchar(450) NOT NULL,
    [FailedAttemptCount] int NOT NULL,
    [LastFailedAttemptAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FailedLoginAttempts] PRIMARY KEY ([Id])
);

CREATE TABLE [WidgetTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [ThemeId] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NULL,
    [DeveloperName] nvarchar(450) NULL,
    [Content] nvarchar(max) NULL,
    [IsBuiltInTemplate] bit NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_WidgetTemplates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WidgetTemplates_Themes_ThemeId] FOREIGN KEY ([ThemeId]) REFERENCES [Themes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WidgetTemplates_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WidgetTemplates_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [SitePages] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [IsPublished] bit NOT NULL,
    [IsDraft] bit NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [_DraftWidgetsJson] nvarchar(max) NULL,
    [_PublishedWidgetsJson] nvarchar(max) NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_SitePages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SitePages_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]),
    CONSTRAINT [FK_SitePages_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SitePages_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SitePages_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SitePageRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [_PublishedWidgetsJson] nvarchar(max) NULL,
    [SitePageId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_SitePageRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SitePageRevisions_SitePages_SitePageId] FOREIGN KEY ([SitePageId]) REFERENCES [SitePages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SitePageRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SitePageRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [WidgetTemplateRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NULL,
    [Content] nvarchar(max) NULL,
    [WidgetTemplateId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_WidgetTemplateRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WidgetTemplateRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WidgetTemplateRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId] FOREIGN KEY ([WidgetTemplateId]) REFERENCES [WidgetTemplates] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_FailedLoginAttempts_EmailAddress] ON [FailedLoginAttempts] ([EmailAddress]);

CREATE INDEX [IX_SitePageRevisions_CreatorUserId] ON [SitePageRevisions] ([CreatorUserId]);

CREATE INDEX [IX_SitePageRevisions_LastModifierUserId] ON [SitePageRevisions] ([LastModifierUserId]);

CREATE INDEX [IX_SitePageRevisions_SitePageId] ON [SitePageRevisions] ([SitePageId]);

CREATE INDEX [IX_SitePages_CreatorUserId] ON [SitePages] ([CreatorUserId]);

CREATE INDEX [IX_SitePages_LastModifierUserId] ON [SitePages] ([LastModifierUserId]);

CREATE UNIQUE INDEX [IX_SitePages_RouteId] ON [SitePages] ([RouteId]);

CREATE INDEX [IX_SitePages_WebTemplateId] ON [SitePages] ([WebTemplateId]);

CREATE INDEX [IX_WidgetTemplateRevisions_CreatorUserId] ON [WidgetTemplateRevisions] ([CreatorUserId]);

CREATE INDEX [IX_WidgetTemplateRevisions_LastModifierUserId] ON [WidgetTemplateRevisions] ([LastModifierUserId]);

CREATE INDEX [IX_WidgetTemplateRevisions_WidgetTemplateId] ON [WidgetTemplateRevisions] ([WidgetTemplateId]);

CREATE INDEX [IX_WidgetTemplates_CreatorUserId] ON [WidgetTemplates] ([CreatorUserId]);

CREATE UNIQUE INDEX [IX_WidgetTemplates_DeveloperName_ThemeId] ON [WidgetTemplates] ([DeveloperName], [ThemeId]) WHERE [DeveloperName] IS NOT NULL;

CREATE INDEX [IX_WidgetTemplates_LastModifierUserId] ON [WidgetTemplates] ([LastModifierUserId]);

CREATE INDEX [IX_WidgetTemplates_ThemeId] ON [WidgetTemplates] ([ThemeId]);


                UPDATE [Roles]
                SET [SystemPermissions] = [SystemPermissions] | 64
                WHERE [DeveloperName] IN ('super_admin', 'admin', 'editor');
                

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251129154657_v1_5_0', N'10.0.0');

COMMIT;
GO


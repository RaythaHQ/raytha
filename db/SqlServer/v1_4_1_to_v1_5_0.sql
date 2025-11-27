BEGIN TRANSACTION;
GO

-- Create SitePages table
CREATE TABLE [SitePages] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [IsPublished] bit NOT NULL,
    [IsDraft] bit NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [_WidgetsJson] nvarchar(max) NOT NULL DEFAULT N'{}',
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_SitePages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SitePages_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SitePages_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SitePages_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SitePages_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

-- Add SitePageId column to Routes table
ALTER TABLE [Routes] ADD [SitePageId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
GO

-- Create indexes for SitePages
CREATE UNIQUE INDEX [IX_SitePages_RouteId] ON [SitePages] ([RouteId]);
GO

CREATE INDEX [IX_SitePages_WebTemplateId] ON [SitePages] ([WebTemplateId]);
GO

CREATE INDEX [IX_SitePages_CreatorUserId] ON [SitePages] ([CreatorUserId]);
GO

CREATE INDEX [IX_SitePages_LastModifierUserId] ON [SitePages] ([LastModifierUserId]);
GO

-- Create WidgetTemplates table
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
GO

-- Create WidgetTemplateRevisions table
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
    CONSTRAINT [FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId] FOREIGN KEY ([WidgetTemplateId]) REFERENCES [WidgetTemplates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WidgetTemplateRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WidgetTemplateRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

-- Create indexes for WidgetTemplates
CREATE INDEX [IX_WidgetTemplates_ThemeId] ON [WidgetTemplates] ([ThemeId]);
GO

CREATE UNIQUE INDEX [IX_WidgetTemplates_DeveloperName_ThemeId] ON [WidgetTemplates] ([DeveloperName], [ThemeId]) WHERE [DeveloperName] IS NOT NULL;
GO

CREATE INDEX [IX_WidgetTemplates_CreatorUserId] ON [WidgetTemplates] ([CreatorUserId]);
GO

CREATE INDEX [IX_WidgetTemplates_LastModifierUserId] ON [WidgetTemplates] ([LastModifierUserId]);
GO

-- Create indexes for WidgetTemplateRevisions
CREATE INDEX [IX_WidgetTemplateRevisions_WidgetTemplateId] ON [WidgetTemplateRevisions] ([WidgetTemplateId]);
GO

CREATE INDEX [IX_WidgetTemplateRevisions_CreatorUserId] ON [WidgetTemplateRevisions] ([CreatorUserId]);
GO

CREATE INDEX [IX_WidgetTemplateRevisions_LastModifierUserId] ON [WidgetTemplateRevisions] ([LastModifierUserId]);
GO

-- Update super_admin and admin roles to include ManageSitePages permission (64)
UPDATE Roles
SET SystemPermissions = SystemPermissions | 64
WHERE DeveloperName IN ('super_admin', 'admin');
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20241127000000_v1_5_0', N'8.0.10');
GO

COMMIT;
GO


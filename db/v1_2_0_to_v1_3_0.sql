BEGIN TRANSACTION;
GO

CREATE TABLE [NavigationMenus] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NOT NULL,
    [DeveloperName] nvarchar(450) NOT NULL,
    [IsMainMenu] bit NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_NavigationMenus] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NavigationMenus_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_NavigationMenus_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [NavigationMenuItems] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NOT NULL,
    [IsDisabled] bit NOT NULL,
    [OpenInNewTab] bit NOT NULL,
    [CssClassName] nvarchar(max) NULL,
    [Ordinal] int NOT NULL,
    [ParentNavigationMenuItemId] uniqueidentifier NULL,
    [NavigationMenuId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_NavigationMenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NavigationMenuItems_NavigationMenuItems_ParentNavigationMenuItemId] FOREIGN KEY ([ParentNavigationMenuItemId]) REFERENCES [NavigationMenuItems] ([Id]),
    CONSTRAINT [FK_NavigationMenuItems_NavigationMenus_NavigationMenuId] FOREIGN KEY ([NavigationMenuId]) REFERENCES [NavigationMenus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_NavigationMenuItems_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_NavigationMenuItems_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [NavigationMenuRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [NavigationMenuItemsJson] nvarchar(max) NOT NULL,
    [NavigationMenuId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_NavigationMenuRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NavigationMenuRevisions_NavigationMenus_NavigationMenuId] FOREIGN KEY ([NavigationMenuId]) REFERENCES [NavigationMenus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_NavigationMenuRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_NavigationMenuRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE INDEX [IX_NavigationMenuItems_CreatorUserId] ON [NavigationMenuItems] ([CreatorUserId]);
GO

CREATE INDEX [IX_NavigationMenuItems_LastModifierUserId] ON [NavigationMenuItems] ([LastModifierUserId]);
GO

CREATE INDEX [IX_NavigationMenuItems_NavigationMenuId] ON [NavigationMenuItems] ([NavigationMenuId]);
GO

CREATE INDEX [IX_NavigationMenuItems_ParentNavigationMenuItemId] ON [NavigationMenuItems] ([ParentNavigationMenuItemId]);
GO

CREATE INDEX [IX_NavigationMenuRevisions_CreatorUserId] ON [NavigationMenuRevisions] ([CreatorUserId]);
GO

CREATE INDEX [IX_NavigationMenuRevisions_LastModifierUserId] ON [NavigationMenuRevisions] ([LastModifierUserId]);
GO

CREATE INDEX [IX_NavigationMenuRevisions_NavigationMenuId] ON [NavigationMenuRevisions] ([NavigationMenuId]);
GO

CREATE INDEX [IX_NavigationMenus_CreatorUserId] ON [NavigationMenus] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_NavigationMenus_DeveloperName] ON [NavigationMenus] ([DeveloperName]);
GO

CREATE INDEX [IX_NavigationMenus_LastModifierUserId] ON [NavigationMenus] ([LastModifierUserId]);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'DeveloperName', N'IsMainMenu', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenus]'))
    SET IDENTITY_INSERT [NavigationMenus] ON;
INSERT INTO [NavigationMenus] ([Id], [Label], [DeveloperName], [IsMainMenu], [CreationTime])
VALUES ('b49f9abf-8e2c-4c25-950c-0ced2f378a37', N'Main menu', N'mainmenu', CAST(1 AS bit), '2024-09-03T09:38:27.0216661Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'DeveloperName', N'IsMainMenu', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenus]'))
    SET IDENTITY_INSERT [NavigationMenus] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'Url', N'IsDisabled', N'OpenInNewTab', N'CssClassName', N'Ordinal', N'NavigationMenuId', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenuItems]'))
    SET IDENTITY_INSERT [NavigationMenuItems] ON;
INSERT INTO [NavigationMenuItems] ([Id], [Label], [Url], [IsDisabled], [OpenInNewTab], [CssClassName], [Ordinal], [NavigationMenuId], [CreationTime])
VALUES ('5982c677-5f8e-4134-8870-84227258f6e0', N'Home', N'/home', CAST(0 AS bit), CAST(0 AS bit), N'nav-link', 1, 'b49f9abf-8e2c-4c25-950c-0ced2f378a37', '2024-09-03T09:38:27.0222388Z'),
('ed7dfb20-e443-4212-8c24-4365d0d5f1e3', N'About', N'/about', CAST(0 AS bit), CAST(0 AS bit), N'nav-link', 2, 'b49f9abf-8e2c-4c25-950c-0ced2f378a37', '2024-09-03T09:38:27.0222392Z'),
('ac03cd99-5fb8-452d-9aa1-8047c040fcb2', N'Posts', N'/posts', CAST(0 AS bit), CAST(0 AS bit), N'nav-link', 3, 'b49f9abf-8e2c-4c25-950c-0ced2f378a37', '2024-09-03T09:38:27.0222398Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'Url', N'IsDisabled', N'OpenInNewTab', N'CssClassName', N'Ordinal', N'NavigationMenuId', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenuItems]'))
    SET IDENTITY_INSERT [NavigationMenuItems] OFF;
GO

DROP INDEX [IX_WebTemplates_DeveloperName] ON [WebTemplates];
GO

ALTER TABLE [DeletedContentItems] ADD [WebTemplateIdsJson] nvarchar(max) NOT NULL DEFAULT N'[]';
GO


                UPDATE DeletedContentItems
                SET WebTemplateIdsJson = '["' + CAST(WebTemplateId AS NVARCHAR(36)) + '"]'
            
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DeletedContentItems]') AND [c].[name] = N'WebTemplateId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [DeletedContentItems] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [DeletedContentItems] DROP COLUMN [WebTemplateId];
GO

CREATE TABLE [Themes] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [DeveloperName] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsExportable] bit NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Themes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Themes_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Themes_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Title', N'DeveloperName', N'IsExportable', N'Description', N'CreationTime') AND [object_id] = OBJECT_ID(N'[Themes]'))
    SET IDENTITY_INSERT [Themes] ON;
INSERT INTO [Themes] ([Id], [Title], [DeveloperName], [IsExportable], [Description], [CreationTime])
VALUES ('944ca79b-1ce5-47c4-8557-c8e6172df1dd', N'Raytha default theme', N'raytha_default_theme', CAST(0 AS bit), N'Raytha default theme', '2024-09-03T09:38:27.0238368Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Title', N'DeveloperName', N'IsExportable', N'Description', N'CreationTime') AND [object_id] = OBJECT_ID(N'[Themes]'))
    SET IDENTITY_INSERT [Themes] OFF;
GO

ALTER TABLE [WebTemplates] ADD [ThemeId] uniqueidentifier NOT NULL DEFAULT '944ca79b-1ce5-47c4-8557-c8e6172df1dd';
GO

ALTER TABLE [OrganizationSettings] ADD [ActiveThemeId] uniqueidentifier NOT NULL DEFAULT '944ca79b-1ce5-47c4-8557-c8e6172df1dd';
GO

ALTER TABLE [ContentItems] DROP CONSTRAINT [FK_ContentItems_WebTemplates_WebTemplateId];
GO

DROP INDEX [IX_ContentItems_WebTemplateId] ON [ContentItems];
GO

CREATE TABLE [WebTemplateContentItemRelations] (
    [Id] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_WebTemplateContentItemRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateContentItemRelations_ContentItems_ContentItemId] FOREIGN KEY ([ContentItemId]) REFERENCES [ContentItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplateContentItemRelations_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
GO


                INSERT INTO WebTemplateContentItemRelations (Id, WebTemplateId, ContentItemId)
                SELECT NEWID(), WebTemplateId, Id
                FROM ContentItems
            
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ContentItems]') AND [c].[name] = N'WebTemplateId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [ContentItems] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [ContentItems] DROP COLUMN [WebTemplateId];
GO

ALTER TABLE [Views] DROP CONSTRAINT [FK_Views_WebTemplates_WebTemplateId];
GO

DROP INDEX [IX_Views_WebTemplateId] ON [Views];
GO

CREATE TABLE [WebTemplateViewRelations] (
    [Id] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ViewId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_WebTemplateViewRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateViewRelations_Views_ViewId] FOREIGN KEY ([ViewId]) REFERENCES [Views] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplateViewRelations_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
GO


                INSERT INTO WebTemplateViewRelations (Id, WebTemplateId, ViewId)
                SELECT NEWID(), WebTemplateId, Id
                FROM Views
            
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Views]') AND [c].[name] = N'WebTemplateId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Views] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Views] DROP COLUMN [WebTemplateId];
GO

CREATE TABLE [ThemeAccessToMediaItems] (
    [Id] uniqueidentifier NOT NULL,
    [ThemeId] uniqueidentifier NOT NULL,
    [MediaItemId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ThemeAccessToMediaItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ThemeAccessToMediaItems_MediaItems_MediaItemId] FOREIGN KEY ([MediaItemId]) REFERENCES [MediaItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ThemeAccessToMediaItems_Themes_ThemeId] FOREIGN KEY ([ThemeId]) REFERENCES [Themes] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_WebTemplates_DeveloperName_ThemeId] ON [WebTemplates] ([DeveloperName], [ThemeId]);
GO

CREATE INDEX [IX_WebTemplates_ThemeId] ON [WebTemplates] ([ThemeId]);
GO

CREATE INDEX [IX_ThemeAccessToMediaItems_MediaItemId] ON [ThemeAccessToMediaItems] ([MediaItemId]);
GO

CREATE INDEX [IX_ThemeAccessToMediaItems_ThemeId] ON [ThemeAccessToMediaItems] ([ThemeId]);
GO

CREATE INDEX [IX_Themes_CreatorUserId] ON [Themes] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_Themes_DeveloperName] ON [Themes] ([DeveloperName]);
GO

CREATE INDEX [IX_Themes_LastModifierUserId] ON [Themes] ([LastModifierUserId]);
GO

CREATE INDEX [IX_WebTemplateContentItemRelations_ContentItemId] ON [WebTemplateContentItemRelations] ([ContentItemId]);
GO

CREATE UNIQUE INDEX [IX_WebTemplateContentItemRelations_WebTemplateId_ContentItemId] ON [WebTemplateContentItemRelations] ([WebTemplateId], [ContentItemId]);
GO

CREATE UNIQUE INDEX [IX_WebTemplateViewRelations_ViewId_WebTemplateId] ON [WebTemplateViewRelations] ([ViewId], [WebTemplateId]);
GO

CREATE INDEX [IX_WebTemplateViewRelations_WebTemplateId] ON [WebTemplateViewRelations] ([WebTemplateId]);
GO

ALTER TABLE [WebTemplates] ADD CONSTRAINT [FK_WebTemplates_Themes_ThemeId] FOREIGN KEY ([ThemeId]) REFERENCES [Themes] ([Id]) ON DELETE CASCADE;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240502121207_v1_3_0', N'8.0.0');
GO

COMMIT;
GO


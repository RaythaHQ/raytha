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
VALUES ('8be7e7d8-1dd7-439c-9b15-962a0f32adaf', N'Main menu', N'mainmenu', CAST(1 AS bit), '2024-05-03T09:08:46.9078055Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'DeveloperName', N'IsMainMenu', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenus]'))
    SET IDENTITY_INSERT [NavigationMenus] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'Url', N'IsDisabled', N'OpenInNewTab', N'CssClassName', N'Ordinal', N'NavigationMenuId', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenuItems]'))
    SET IDENTITY_INSERT [NavigationMenuItems] ON;
INSERT INTO [NavigationMenuItems] ([Id], [Label], [Url], [IsDisabled], [OpenInNewTab], [CssClassName], [Ordinal], [NavigationMenuId], [CreationTime])
VALUES ('8afc88f3-c69d-4d8b-9aa2-94918e613659', N'Home', N'/home', CAST(0 AS bit), CAST(0 AS bit), N'nav-link', 1, '23d9de61-674b-4cd5-826f-95749bd34bd4', '2024-05-15T10:06:43.7633830Z'),
('bb34845e-765c-4500-bf0e-1a8dccfd6980', N'About', N'/about', CAST(0 AS bit), CAST(0 AS bit), N'nav-link', 2, '23d9de61-674b-4cd5-826f-95749bd34bd4', '2024-05-15T10:06:43.7633834Z'),
('9bdbbc5f-7484-449d-97cc-682dab76a2ac', N'Posts', N'/posts', CAST(0 AS bit), CAST(0 AS bit), N'nav-link', 3, '473aa1af-189c-4bf0-b836-2f59d7c9d5d3', '2024-05-17T09:02:15.2319668Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Label', N'Url', N'IsDisabled', N'OpenInNewTab', N'CssClassName', N'Ordinal', N'NavigationMenuId', N'CreationTime') AND [object_id] = OBJECT_ID(N'[NavigationMenuItems]'))
    SET IDENTITY_INSERT [NavigationMenuItems] OFF;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240502121207_v1_3_0', N'8.0.0');
GO

COMMIT;
GO


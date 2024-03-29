BEGIN TRANSACTION;
GO

CREATE TABLE [RaythaFunctions] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [DeveloperName] nvarchar(450) NOT NULL,
    [TriggerType] nvarchar(max) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_RaythaFunctions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RaythaFunctions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_RaythaFunctions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [RaythaFunctionRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [RaythaFunctionId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_RaythaFunctionRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RaythaFunctionRevisions_RaythaFunctions_RaythaFunctionId] FOREIGN KEY ([RaythaFunctionId]) REFERENCES [RaythaFunctions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RaythaFunctionRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_RaythaFunctionRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE INDEX [IX_RaythaFunctionRevisions_CreatorUserId] ON [RaythaFunctionRevisions] ([CreatorUserId]);
GO

CREATE INDEX [IX_RaythaFunctionRevisions_LastModifierUserId] ON [RaythaFunctionRevisions] ([LastModifierUserId]);
GO

CREATE INDEX [IX_RaythaFunctionRevisions_RaythaFunctionId] ON [RaythaFunctionRevisions] ([RaythaFunctionId]);
GO

CREATE INDEX [IX_RaythaFunctions_CreatorUserId] ON [RaythaFunctions] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_RaythaFunctions_DeveloperName] ON [RaythaFunctions] ([DeveloperName]);
GO

CREATE INDEX [IX_RaythaFunctions_LastModifierUserId] ON [RaythaFunctions] ([LastModifierUserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240314124844_v1_2_0', N'8.0.0');
GO

COMMIT;
GO


BEGIN TRANSACTION;
GO

ALTER TABLE [Views] ADD [DefaultNumberOfItemsPerPage] int NOT NULL DEFAULT 25;
GO

ALTER TABLE [Views] ADD [IgnoreClientFilterAndSortQueryParams] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Views] ADD [MaxNumberOfItemsPerPage] int NOT NULL DEFAULT 1000;
GO

ALTER TABLE [OrganizationSettings] ADD [HomePageType] nvarchar(max) NOT NULL DEFAULT N'ContentItem';
GO

CREATE TABLE [ApiKeys] (
    [Id] uniqueidentifier NOT NULL,
    [ApiKeyHash] varbinary(900) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [CreationTime] datetime2 NOT NULL,
    CONSTRAINT [PK_ApiKeys] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApiKeys_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ApiKeys_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [DataProtectionKeys] (
    [Id] int NOT NULL IDENTITY,
    [FriendlyName] nvarchar(max) NULL,
    [Xml] nvarchar(max) NULL,
    CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_ApiKeys_ApiKeyHash] ON [ApiKeys] ([ApiKeyHash]);
GO

CREATE INDEX [IX_ApiKeys_CreatorUserId] ON [ApiKeys] ([CreatorUserId]);
GO

CREATE INDEX [IX_ApiKeys_UserId] ON [ApiKeys] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230211205159_v1_0_0', N'7.0.1');
GO

COMMIT;
GO


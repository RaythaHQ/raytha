BEGIN TRANSACTION;
GO

CREATE TABLE [BackgroundTasks] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Args] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [StatusInfo] nvarchar(max) NULL,
    [PercentComplete] int NOT NULL,
    [NumberOfRetries] int NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CompletionTime] datetime2 NULL,
    [TaskStep] int NOT NULL,
    CONSTRAINT [PK_BackgroundTasks] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230521175706_v1_1_0', N'7.0.1');
GO

COMMIT;
GO

ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
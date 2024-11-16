BEGIN TRANSACTION;
GO

DROP INDEX [IX_WebTemplates_DeveloperName_ThemeId] ON [WebTemplates];
GO

DROP INDEX [IX_Users_EmailAddress] ON [Users];
GO

DROP INDEX [IX_Users_SsoId_AuthenticationSchemeId] ON [Users];
GO

DROP INDEX [IX_UserGroups_DeveloperName] ON [UserGroups];
GO

DROP INDEX [IX_Routes_Path] ON [Routes];
GO

DROP INDEX [IX_Roles_DeveloperName] ON [Roles];
GO

DROP INDEX [IX_JwtLogins_Jti] ON [JwtLogins];
GO

DROP INDEX [IX_AuthenticationSchemes_DeveloperName] ON [AuthenticationSchemes];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmailTemplates]') AND [c].[name] = N'DeveloperName');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [EmailTemplates] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [EmailTemplates] ALTER COLUMN [DeveloperName] nvarchar(450) NULL;
GO

CREATE UNIQUE INDEX [IX_WebTemplates_DeveloperName_ThemeId] ON [WebTemplates] ([DeveloperName], [ThemeId]) WHERE [DeveloperName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Users_EmailAddress] ON [Users] ([EmailAddress]);
GO

CREATE UNIQUE INDEX [IX_Users_SsoId_AuthenticationSchemeId] ON [Users] ([SsoId], [AuthenticationSchemeId]) WHERE [SsoId] IS NOT NULL AND [AuthenticationSchemeId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_UserGroups_DeveloperName] ON [UserGroups] ([DeveloperName]);
GO

CREATE UNIQUE INDEX [IX_Routes_Path] ON [Routes] ([Path]);
GO

CREATE UNIQUE INDEX [IX_Roles_DeveloperName] ON [Roles] ([DeveloperName]);
GO

CREATE UNIQUE INDEX [IX_JwtLogins_Jti] ON [JwtLogins] ([Jti]) WHERE [Jti] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_EmailTemplates_DeveloperName] ON [EmailTemplates] ([DeveloperName]) WHERE [DeveloperName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_AuthenticationSchemes_DeveloperName] ON [AuthenticationSchemes] ([DeveloperName]) WHERE [DeveloperName] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20241116192521_v1_4_0', N'8.0.10');
GO

COMMIT;
GO


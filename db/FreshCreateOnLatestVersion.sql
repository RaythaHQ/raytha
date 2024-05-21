IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [EntityId] uniqueidentifier NULL,
    [Category] nvarchar(450) NOT NULL,
    [Request] nvarchar(max) NOT NULL,
    [UserEmail] nvarchar(max) NOT NULL,
    [IpAddress] nvarchar(max) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [JwtLogins] (
    [Id] uniqueidentifier NOT NULL,
    [Jti] nvarchar(450) NULL,
    [CreationTime] datetime2 NOT NULL,
    CONSTRAINT [PK_JwtLogins] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [OrganizationSettings] (
    [Id] uniqueidentifier NOT NULL,
    [OrganizationName] nvarchar(max) NULL,
    [WebsiteUrl] nvarchar(max) NULL,
    [TimeZone] nvarchar(max) NULL,
    [DateFormat] nvarchar(max) NULL,
    [SmtpOverrideSystem] bit NOT NULL,
    [SmtpHost] nvarchar(max) NULL,
    [SmtpPort] int NULL,
    [SmtpUsername] nvarchar(max) NULL,
    [SmtpPassword] nvarchar(max) NULL,
    [SmtpDefaultFromAddress] nvarchar(max) NULL,
    [SmtpDefaultFromName] nvarchar(max) NULL,
    [HomePageId] uniqueidentifier NULL,
    CONSTRAINT [PK_OrganizationSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Routes] (
    [Id] uniqueidentifier NOT NULL,
    [Path] nvarchar(450) NOT NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    [ViewId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AuthenticationSchemes] (
    [Id] uniqueidentifier NOT NULL,
    [IsBuiltInAuth] bit NOT NULL,
    [IsEnabledForUsers] bit NOT NULL,
    [IsEnabledForAdmins] bit NOT NULL,
    [AuthenticationSchemeType] nvarchar(max) NULL,
    [Label] nvarchar(max) NULL,
    [DeveloperName] nvarchar(450) NULL,
    [MagicLinkExpiresInSeconds] int NOT NULL,
    [SamlCertificate] nvarchar(max) NULL,
    [SamlIdpEntityId] nvarchar(max) NULL,
    [JwtSecretKey] nvarchar(max) NULL,
    [JwtUseHighSecurity] bit NOT NULL,
    [SignInUrl] nvarchar(max) NULL,
    [LoginButtonText] nvarchar(max) NULL,
    [SignOutUrl] nvarchar(max) NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_AuthenticationSchemes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [IsAdmin] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [LastLoggedInTime] datetime2 NULL,
    [Salt] varbinary(max) NOT NULL,
    [PasswordHash] varbinary(max) NOT NULL,
    [SsoId] nvarchar(450) NULL,
    [AuthenticationSchemeId] uniqueidentifier NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [EmailAddress] nvarchar(450) NOT NULL,
    [IsEmailAddressConfirmed] bit NOT NULL,
    [_RecentlyAccessedViews] nvarchar(max) NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_AuthenticationSchemes_AuthenticationSchemeId] FOREIGN KEY ([AuthenticationSchemeId]) REFERENCES [AuthenticationSchemes] ([Id]),
    CONSTRAINT [FK_Users_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Users_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [ContentTypes] (
    [Id] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL,
    [LabelPlural] nvarchar(max) NULL,
    [LabelSingular] nvarchar(max) NULL,
    [DeveloperName] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [DefaultRouteTemplate] nvarchar(max) NULL,
    [PrimaryFieldId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    [DeleterUserId] uniqueidentifier NULL,
    [DeletionTime] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ContentTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentTypes_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ContentTypes_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [EmailTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Subject] nvarchar(max) NULL,
    [DeveloperName] nvarchar(max) NULL,
    [Cc] nvarchar(max) NULL,
    [Bcc] nvarchar(max) NULL,
    [Content] nvarchar(max) NULL,
    [IsBuiltInTemplate] bit NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_EmailTemplates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailTemplates_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_EmailTemplates_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [MediaItems] (
    [Id] uniqueidentifier NOT NULL,
    [Length] bigint NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [FileStorageProvider] nvarchar(max) NOT NULL,
    [ObjectKey] nvarchar(450) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_MediaItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MediaItems_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_MediaItems_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [OneTimePasswords] (
    [Id] varbinary(900) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_OneTimePasswords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OneTimePasswords_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NOT NULL,
    [DeveloperName] nvarchar(450) NOT NULL,
    [SystemPermissions] int NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Roles_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Roles_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [UserGroups] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NOT NULL,
    [DeveloperName] nvarchar(450) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_UserGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserGroups_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_UserGroups_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [VerificationCodes] (
    [Id] uniqueidentifier NOT NULL,
    [Code] uniqueidentifier NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [Completed] bit NOT NULL,
    [EmailAddress] nvarchar(max) NULL,
    [VerificationCodeType] nvarchar(max) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_VerificationCodes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VerificationCodes_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_VerificationCodes_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [WebTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [IsBaseLayout] bit NOT NULL,
    [Label] nvarchar(max) NULL,
    [DeveloperName] nvarchar(450) NULL,
    [Content] nvarchar(max) NULL,
    [IsBuiltInTemplate] bit NOT NULL,
    [ParentTemplateId] uniqueidentifier NULL,
    [AllowAccessForNewContentTypes] bit NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_WebTemplates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplates_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WebTemplates_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WebTemplates_WebTemplates_ParentTemplateId] FOREIGN KEY ([ParentTemplateId]) REFERENCES [WebTemplates] ([Id])
);
GO

CREATE TABLE [ContentTypeFields] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NULL,
    [DeveloperName] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [FieldOrder] int NOT NULL,
    [IsRequired] bit NOT NULL,
    [RelatedContentTypeId] uniqueidentifier NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [FieldType] nvarchar(max) NOT NULL,
    [_Choices] nvarchar(max) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    [DeleterUserId] uniqueidentifier NULL,
    [DeletionTime] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ContentTypeFields] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentTypeFields_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]),
    CONSTRAINT [FK_ContentTypeFields_ContentTypes_RelatedContentTypeId] FOREIGN KEY ([RelatedContentTypeId]) REFERENCES [ContentTypes] ([Id]),
    CONSTRAINT [FK_ContentTypeFields_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ContentTypeFields_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [DeletedContentItems] (
    [Id] uniqueidentifier NOT NULL,
    [_PublishedContent] nvarchar(max) NULL,
    [PrimaryField] nvarchar(max) NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [OriginalContentItemId] uniqueidentifier NOT NULL,
    [RoutePath] nvarchar(max) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_DeletedContentItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeletedContentItems_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DeletedContentItems_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_DeletedContentItems_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [EmailTemplateRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [Subject] nvarchar(max) NULL,
    [Content] nvarchar(max) NULL,
    [Cc] nvarchar(max) NULL,
    [Bcc] nvarchar(max) NULL,
    [EmailTemplateId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_EmailTemplateRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailTemplateRevisions_EmailTemplates_EmailTemplateId] FOREIGN KEY ([EmailTemplateId]) REFERENCES [EmailTemplates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_EmailTemplateRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_EmailTemplateRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [ContentTypeRolePermission] (
    [Id] uniqueidentifier NOT NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [ContentTypePermissions] int NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_ContentTypeRolePermission] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentTypeRolePermission_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContentTypeRolePermission_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContentTypeRolePermission_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ContentTypeRolePermission_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [RoleUser] (
    [RolesId] uniqueidentifier NOT NULL,
    [UsersId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
    CONSTRAINT [FK_RoleUser_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserUserGroup] (
    [UserGroupsId] uniqueidentifier NOT NULL,
    [UsersId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserUserGroup] PRIMARY KEY ([UserGroupsId], [UsersId]),
    CONSTRAINT [FK_UserUserGroup_UserGroups_UserGroupsId] FOREIGN KEY ([UserGroupsId]) REFERENCES [UserGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserUserGroup_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ContentItems] (
    [Id] uniqueidentifier NOT NULL,
    [IsPublished] bit NOT NULL,
    [IsDraft] bit NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [_DraftContent] nvarchar(max) NULL,
    [_PublishedContent] nvarchar(max) NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_ContentItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentItems_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContentItems_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]),
    CONSTRAINT [FK_ContentItems_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ContentItems_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ContentItems_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Views] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NULL,
    [DeveloperName] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [IsPublished] bit NOT NULL,
    [_Columns] nvarchar(max) NULL,
    [_Filter] nvarchar(max) NULL,
    [_Sort] nvarchar(max) NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_Views] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Views_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Views_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]),
    CONSTRAINT [FK_Views_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Views_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_Views_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WebTemplateAccessToModelDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_WebTemplateAccessToModelDefinitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateAccessToModelDefinitions_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplateAccessToModelDefinitions_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WebTemplateRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NULL,
    [Content] nvarchar(max) NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [AllowAccessForNewContentTypes] bit NOT NULL,
    [EmailTemplateId] uniqueidentifier NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_WebTemplateRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateRevisions_EmailTemplates_EmailTemplateId] FOREIGN KEY ([EmailTemplateId]) REFERENCES [EmailTemplates] ([Id]),
    CONSTRAINT [FK_WebTemplateRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WebTemplateRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WebTemplateRevisions_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ContentItemRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [_PublishedContent] nvarchar(max) NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_ContentItemRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ContentItemRevisions_ContentItems_ContentItemId] FOREIGN KEY ([ContentItemId]) REFERENCES [ContentItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ContentItemRevisions_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ContentItemRevisions_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [UserView] (
    [FavoriteViewsId] uniqueidentifier NOT NULL,
    [UserFavoritesId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserView] PRIMARY KEY ([FavoriteViewsId], [UserFavoritesId]),
    CONSTRAINT [FK_UserView_Users_UserFavoritesId] FOREIGN KEY ([UserFavoritesId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserView_Views_FavoriteViewsId] FOREIGN KEY ([FavoriteViewsId]) REFERENCES [Views] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AuditLogs_Category] ON [AuditLogs] ([Category]);
GO

CREATE INDEX [IX_AuditLogs_CreationTime] ON [AuditLogs] ([CreationTime]);
GO

CREATE INDEX [IX_AuditLogs_EntityId] ON [AuditLogs] ([EntityId]);
GO

CREATE INDEX [IX_AuthenticationSchemes_CreatorUserId] ON [AuthenticationSchemes] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_AuthenticationSchemes_DeveloperName] ON [AuthenticationSchemes] ([DeveloperName]) INCLUDE ([Id], [Label]) WHERE [DeveloperName] IS NOT NULL;
GO

CREATE INDEX [IX_AuthenticationSchemes_LastModifierUserId] ON [AuthenticationSchemes] ([LastModifierUserId]);
GO

CREATE INDEX [IX_ContentItemRevisions_ContentItemId] ON [ContentItemRevisions] ([ContentItemId]);
GO

CREATE INDEX [IX_ContentItemRevisions_CreatorUserId] ON [ContentItemRevisions] ([CreatorUserId]);
GO

CREATE INDEX [IX_ContentItemRevisions_LastModifierUserId] ON [ContentItemRevisions] ([LastModifierUserId]);
GO

CREATE INDEX [IX_ContentItems_ContentTypeId] ON [ContentItems] ([ContentTypeId]);
GO

CREATE INDEX [IX_ContentItems_CreatorUserId] ON [ContentItems] ([CreatorUserId]);
GO

CREATE INDEX [IX_ContentItems_LastModifierUserId] ON [ContentItems] ([LastModifierUserId]);
GO

CREATE UNIQUE INDEX [IX_ContentItems_RouteId] ON [ContentItems] ([RouteId]);
GO

CREATE INDEX [IX_ContentItems_WebTemplateId] ON [ContentItems] ([WebTemplateId]);
GO

CREATE INDEX [IX_ContentTypeFields_ContentTypeId] ON [ContentTypeFields] ([ContentTypeId]);
GO

CREATE INDEX [IX_ContentTypeFields_CreatorUserId] ON [ContentTypeFields] ([CreatorUserId]);
GO

CREATE INDEX [IX_ContentTypeFields_LastModifierUserId] ON [ContentTypeFields] ([LastModifierUserId]);
GO

CREATE INDEX [IX_ContentTypeFields_RelatedContentTypeId] ON [ContentTypeFields] ([RelatedContentTypeId]);
GO

CREATE INDEX [IX_ContentTypeRolePermission_ContentTypeId] ON [ContentTypeRolePermission] ([ContentTypeId]);
GO

CREATE INDEX [IX_ContentTypeRolePermission_CreatorUserId] ON [ContentTypeRolePermission] ([CreatorUserId]);
GO

CREATE INDEX [IX_ContentTypeRolePermission_LastModifierUserId] ON [ContentTypeRolePermission] ([LastModifierUserId]);
GO

CREATE INDEX [IX_ContentTypeRolePermission_RoleId] ON [ContentTypeRolePermission] ([RoleId]);
GO

CREATE INDEX [IX_ContentTypes_CreatorUserId] ON [ContentTypes] ([CreatorUserId]);
GO

CREATE INDEX [IX_ContentTypes_LastModifierUserId] ON [ContentTypes] ([LastModifierUserId]);
GO

CREATE INDEX [IX_DeletedContentItems_ContentTypeId] ON [DeletedContentItems] ([ContentTypeId]);
GO

CREATE INDEX [IX_DeletedContentItems_CreatorUserId] ON [DeletedContentItems] ([CreatorUserId]);
GO

CREATE INDEX [IX_DeletedContentItems_LastModifierUserId] ON [DeletedContentItems] ([LastModifierUserId]);
GO

CREATE INDEX [IX_EmailTemplateRevisions_CreatorUserId] ON [EmailTemplateRevisions] ([CreatorUserId]);
GO

CREATE INDEX [IX_EmailTemplateRevisions_EmailTemplateId] ON [EmailTemplateRevisions] ([EmailTemplateId]);
GO

CREATE INDEX [IX_EmailTemplateRevisions_LastModifierUserId] ON [EmailTemplateRevisions] ([LastModifierUserId]);
GO

CREATE INDEX [IX_EmailTemplates_CreatorUserId] ON [EmailTemplates] ([CreatorUserId]);
GO

CREATE INDEX [IX_EmailTemplates_LastModifierUserId] ON [EmailTemplates] ([LastModifierUserId]);
GO

CREATE UNIQUE INDEX [IX_JwtLogins_Jti] ON [JwtLogins] ([Jti]) INCLUDE ([Id]) WHERE [Jti] IS NOT NULL;
GO

CREATE INDEX [IX_MediaItems_CreatorUserId] ON [MediaItems] ([CreatorUserId]);
GO

CREATE INDEX [IX_MediaItems_LastModifierUserId] ON [MediaItems] ([LastModifierUserId]);
GO

CREATE INDEX [IX_MediaItems_ObjectKey] ON [MediaItems] ([ObjectKey]);
GO

CREATE INDEX [IX_OneTimePasswords_UserId] ON [OneTimePasswords] ([UserId]);
GO

CREATE INDEX [IX_Roles_CreatorUserId] ON [Roles] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_Roles_DeveloperName] ON [Roles] ([DeveloperName]) INCLUDE ([Id], [Label]);
GO

CREATE INDEX [IX_Roles_LastModifierUserId] ON [Roles] ([LastModifierUserId]);
GO

CREATE INDEX [IX_RoleUser_UsersId] ON [RoleUser] ([UsersId]);
GO

CREATE UNIQUE INDEX [IX_Routes_Path] ON [Routes] ([Path]) INCLUDE ([Id], [ViewId], [ContentItemId]);
GO

CREATE INDEX [IX_UserGroups_CreatorUserId] ON [UserGroups] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_UserGroups_DeveloperName] ON [UserGroups] ([DeveloperName]) INCLUDE ([Id], [Label]);
GO

CREATE INDEX [IX_UserGroups_LastModifierUserId] ON [UserGroups] ([LastModifierUserId]);
GO

CREATE INDEX [IX_Users_AuthenticationSchemeId] ON [Users] ([AuthenticationSchemeId]);
GO

CREATE INDEX [IX_Users_CreatorUserId] ON [Users] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_Users_EmailAddress] ON [Users] ([EmailAddress]) INCLUDE ([Id], [FirstName], [LastName], [SsoId], [AuthenticationSchemeId]);
GO

CREATE INDEX [IX_Users_LastModifierUserId] ON [Users] ([LastModifierUserId]);
GO

CREATE UNIQUE INDEX [IX_Users_SsoId_AuthenticationSchemeId] ON [Users] ([SsoId], [AuthenticationSchemeId]) INCLUDE ([Id], [EmailAddress], [FirstName], [LastName]) WHERE [SsoId] IS NOT NULL AND [AuthenticationSchemeId] IS NOT NULL;
GO

CREATE INDEX [IX_UserUserGroup_UsersId] ON [UserUserGroup] ([UsersId]);
GO

CREATE INDEX [IX_UserView_UserFavoritesId] ON [UserView] ([UserFavoritesId]);
GO

CREATE INDEX [IX_VerificationCodes_CreatorUserId] ON [VerificationCodes] ([CreatorUserId]);
GO

CREATE INDEX [IX_VerificationCodes_LastModifierUserId] ON [VerificationCodes] ([LastModifierUserId]);
GO

CREATE INDEX [IX_Views_ContentTypeId] ON [Views] ([ContentTypeId]);
GO

CREATE INDEX [IX_Views_CreatorUserId] ON [Views] ([CreatorUserId]);
GO

CREATE INDEX [IX_Views_LastModifierUserId] ON [Views] ([LastModifierUserId]);
GO

CREATE UNIQUE INDEX [IX_Views_RouteId] ON [Views] ([RouteId]);
GO

CREATE INDEX [IX_Views_WebTemplateId] ON [Views] ([WebTemplateId]);
GO

CREATE INDEX [IX_WebTemplateAccessToModelDefinitions_ContentTypeId] ON [WebTemplateAccessToModelDefinitions] ([ContentTypeId]);
GO

CREATE INDEX [IX_WebTemplateAccessToModelDefinitions_WebTemplateId] ON [WebTemplateAccessToModelDefinitions] ([WebTemplateId]);
GO

CREATE INDEX [IX_WebTemplateRevisions_CreatorUserId] ON [WebTemplateRevisions] ([CreatorUserId]);
GO

CREATE INDEX [IX_WebTemplateRevisions_EmailTemplateId] ON [WebTemplateRevisions] ([EmailTemplateId]);
GO

CREATE INDEX [IX_WebTemplateRevisions_LastModifierUserId] ON [WebTemplateRevisions] ([LastModifierUserId]);
GO

CREATE INDEX [IX_WebTemplateRevisions_WebTemplateId] ON [WebTemplateRevisions] ([WebTemplateId]);
GO

CREATE INDEX [IX_WebTemplates_CreatorUserId] ON [WebTemplates] ([CreatorUserId]);
GO

CREATE UNIQUE INDEX [IX_WebTemplates_DeveloperName] ON [WebTemplates] ([DeveloperName]) INCLUDE ([Id], [Label]) WHERE [DeveloperName] IS NOT NULL;
GO

CREATE INDEX [IX_WebTemplates_LastModifierUserId] ON [WebTemplates] ([LastModifierUserId]);
GO

CREATE INDEX [IX_WebTemplates_ParentTemplateId] ON [WebTemplates] ([ParentTemplateId]);
GO

ALTER TABLE [AuthenticationSchemes] ADD CONSTRAINT [FK_AuthenticationSchemes_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]);
GO

ALTER TABLE [AuthenticationSchemes] ADD CONSTRAINT [FK_AuthenticationSchemes_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20221230221303_v0_9_0', N'7.0.1');
GO

COMMIT;
GO

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

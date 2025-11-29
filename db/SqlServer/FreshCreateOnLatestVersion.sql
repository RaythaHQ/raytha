IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL BEGIN CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
END;
GO BEGIN TRANSACTION;
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
CREATE TABLE [DataProtectionKeys] (
    [Id] int NOT NULL IDENTITY,
    [FriendlyName] nvarchar(max) NULL,
    [Xml] nvarchar(max) NULL,
    CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY ([Id])
);
CREATE TABLE [FailedLoginAttempts] (
    [Id] uniqueidentifier NOT NULL,
    [EmailAddress] nvarchar(450) NOT NULL,
    [FailedAttemptCount] int NOT NULL,
    [LastFailedAttemptAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FailedLoginAttempts] PRIMARY KEY ([Id])
);
CREATE TABLE [JwtLogins] (
    [Id] uniqueidentifier NOT NULL,
    [Jti] nvarchar(450) NULL,
    [CreationTime] datetime2 NOT NULL,
    CONSTRAINT [PK_JwtLogins] PRIMARY KEY ([Id])
);
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
    [HomePageType] nvarchar(max) NOT NULL,
    [ActiveThemeId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_OrganizationSettings] PRIMARY KEY ([Id])
);
CREATE TABLE [Routes] (
    [Id] uniqueidentifier NOT NULL,
    [Path] nvarchar(450) NOT NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    [ViewId] uniqueidentifier NOT NULL,
    [SitePageId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([Id])
);
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
    [BruteForceProtectionMaxFailedAttempts] int NOT NULL,
    [BruteForceProtectionWindowInSeconds] int NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_AuthenticationSchemes] PRIMARY KEY ([Id])
);
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
CREATE TABLE [EmailTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Subject] nvarchar(max) NULL,
    [DeveloperName] nvarchar(450) NULL,
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
CREATE TABLE [OneTimePasswords] (
    [Id] varbinary(900) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_OneTimePasswords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OneTimePasswords_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
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
CREATE TABLE [ContentItems] (
    [Id] uniqueidentifier NOT NULL,
    [IsPublished] bit NOT NULL,
    [IsDraft] bit NOT NULL,
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
    CONSTRAINT [FK_ContentItems_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
CREATE TABLE [DeletedContentItems] (
    [Id] uniqueidentifier NOT NULL,
    [_PublishedContent] nvarchar(max) NULL,
    [PrimaryField] nvarchar(max) NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [OriginalContentItemId] uniqueidentifier NOT NULL,
    [RoutePath] nvarchar(max) NOT NULL,
    [WebTemplateIdsJson] nvarchar(max) NOT NULL,
    [CreationTime] datetime2 NOT NULL,
    [LastModificationTime] datetime2 NULL,
    [CreatorUserId] uniqueidentifier NULL,
    [LastModifierUserId] uniqueidentifier NULL,
    CONSTRAINT [PK_DeletedContentItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DeletedContentItems_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DeletedContentItems_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_DeletedContentItems_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
CREATE TABLE [Views] (
    [Id] uniqueidentifier NOT NULL,
    [Label] nvarchar(max) NULL,
    [DeveloperName] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    [RouteId] uniqueidentifier NOT NULL,
    [IsPublished] bit NOT NULL,
    [DefaultNumberOfItemsPerPage] int NOT NULL,
    [MaxNumberOfItemsPerPage] int NOT NULL,
    [IgnoreClientFilterAndSortQueryParams] bit NOT NULL,
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
    CONSTRAINT [FK_Views_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id])
);
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
CREATE TABLE [RoleUser] (
    [RolesId] uniqueidentifier NOT NULL,
    [UsersId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RoleUser] PRIMARY KEY ([RolesId], [UsersId]),
    CONSTRAINT [FK_RoleUser_Roles_RolesId] FOREIGN KEY ([RolesId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleUser_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [ThemeAccessToMediaItems] (
    [Id] uniqueidentifier NOT NULL,
    [ThemeId] uniqueidentifier NOT NULL,
    [MediaItemId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ThemeAccessToMediaItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ThemeAccessToMediaItems_MediaItems_MediaItemId] FOREIGN KEY ([MediaItemId]) REFERENCES [MediaItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ThemeAccessToMediaItems_Themes_ThemeId] FOREIGN KEY ([ThemeId]) REFERENCES [Themes] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [WebTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [ThemeId] uniqueidentifier NOT NULL,
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
    CONSTRAINT [FK_WebTemplates_Themes_ThemeId] FOREIGN KEY ([ThemeId]) REFERENCES [Themes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplates_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WebTemplates_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_WebTemplates_WebTemplates_ParentTemplateId] FOREIGN KEY ([ParentTemplateId]) REFERENCES [WebTemplates] ([Id])
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
CREATE TABLE [UserUserGroup] (
    [UserGroupsId] uniqueidentifier NOT NULL,
    [UsersId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserUserGroup] PRIMARY KEY ([UserGroupsId], [UsersId]),
    CONSTRAINT [FK_UserUserGroup_UserGroups_UserGroupsId] FOREIGN KEY ([UserGroupsId]) REFERENCES [UserGroups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserUserGroup_Users_UsersId] FOREIGN KEY ([UsersId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
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
CREATE TABLE [UserView] (
    [FavoriteViewsId] uniqueidentifier NOT NULL,
    [UserFavoritesId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_UserView] PRIMARY KEY ([FavoriteViewsId], [UserFavoritesId]),
    CONSTRAINT [FK_UserView_Users_UserFavoritesId] FOREIGN KEY ([UserFavoritesId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserView_Views_FavoriteViewsId] FOREIGN KEY ([FavoriteViewsId]) REFERENCES [Views] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [WebTemplateAccessToModelDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ContentTypeId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_WebTemplateAccessToModelDefinitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateAccessToModelDefinitions_ContentTypes_ContentTypeId] FOREIGN KEY ([ContentTypeId]) REFERENCES [ContentTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplateAccessToModelDefinitions_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
CREATE TABLE [WebTemplateContentItemRelations] (
    [Id] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ContentItemId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_WebTemplateContentItemRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateContentItemRelations_ContentItems_ContentItemId] FOREIGN KEY ([ContentItemId]) REFERENCES [ContentItems] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplateContentItemRelations_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
);
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
CREATE TABLE [WebTemplateViewRelations] (
    [Id] uniqueidentifier NOT NULL,
    [WebTemplateId] uniqueidentifier NOT NULL,
    [ViewId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_WebTemplateViewRelations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WebTemplateViewRelations_Views_ViewId] FOREIGN KEY ([ViewId]) REFERENCES [Views] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WebTemplateViewRelations_WebTemplates_WebTemplateId] FOREIGN KEY ([WebTemplateId]) REFERENCES [WebTemplates] ([Id]) ON DELETE CASCADE
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
-- Indexes
CREATE UNIQUE INDEX [IX_ApiKeys_ApiKeyHash] ON [ApiKeys] ([ApiKeyHash]);
CREATE INDEX [IX_ApiKeys_CreatorUserId] ON [ApiKeys] ([CreatorUserId]);
CREATE INDEX [IX_ApiKeys_UserId] ON [ApiKeys] ([UserId]);
CREATE INDEX [IX_AuditLogs_Category] ON [AuditLogs] ([Category]);
CREATE INDEX [IX_AuditLogs_CreationTime] ON [AuditLogs] ([CreationTime]);
CREATE INDEX [IX_AuditLogs_EntityId] ON [AuditLogs] ([EntityId]);
CREATE INDEX [IX_AuthenticationSchemes_CreatorUserId] ON [AuthenticationSchemes] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_AuthenticationSchemes_DeveloperName] ON [AuthenticationSchemes] ([DeveloperName])
WHERE [DeveloperName] IS NOT NULL;
CREATE INDEX [IX_AuthenticationSchemes_LastModifierUserId] ON [AuthenticationSchemes] ([LastModifierUserId]);
CREATE INDEX [IX_ContentItemRevisions_ContentItemId] ON [ContentItemRevisions] ([ContentItemId]);
CREATE INDEX [IX_ContentItemRevisions_CreatorUserId] ON [ContentItemRevisions] ([CreatorUserId]);
CREATE INDEX [IX_ContentItemRevisions_LastModifierUserId] ON [ContentItemRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_ContentItems_ContentTypeId] ON [ContentItems] ([ContentTypeId]);
CREATE INDEX [IX_ContentItems_CreatorUserId] ON [ContentItems] ([CreatorUserId]);
CREATE INDEX [IX_ContentItems_LastModifierUserId] ON [ContentItems] ([LastModifierUserId]);
CREATE UNIQUE INDEX [IX_ContentItems_RouteId] ON [ContentItems] ([RouteId]);
CREATE INDEX [IX_ContentTypeFields_ContentTypeId] ON [ContentTypeFields] ([ContentTypeId]);
CREATE INDEX [IX_ContentTypeFields_CreatorUserId] ON [ContentTypeFields] ([CreatorUserId]);
CREATE INDEX [IX_ContentTypeFields_LastModifierUserId] ON [ContentTypeFields] ([LastModifierUserId]);
CREATE INDEX [IX_ContentTypeFields_RelatedContentTypeId] ON [ContentTypeFields] ([RelatedContentTypeId]);
CREATE INDEX [IX_ContentTypeRolePermission_ContentTypeId] ON [ContentTypeRolePermission] ([ContentTypeId]);
CREATE INDEX [IX_ContentTypeRolePermission_CreatorUserId] ON [ContentTypeRolePermission] ([CreatorUserId]);
CREATE INDEX [IX_ContentTypeRolePermission_LastModifierUserId] ON [ContentTypeRolePermission] ([LastModifierUserId]);
CREATE INDEX [IX_ContentTypeRolePermission_RoleId] ON [ContentTypeRolePermission] ([RoleId]);
CREATE INDEX [IX_ContentTypes_CreatorUserId] ON [ContentTypes] ([CreatorUserId]);
CREATE INDEX [IX_ContentTypes_LastModifierUserId] ON [ContentTypes] ([LastModifierUserId]);
CREATE INDEX [IX_DeletedContentItems_ContentTypeId] ON [DeletedContentItems] ([ContentTypeId]);
CREATE INDEX [IX_DeletedContentItems_CreatorUserId] ON [DeletedContentItems] ([CreatorUserId]);
CREATE INDEX [IX_DeletedContentItems_LastModifierUserId] ON [DeletedContentItems] ([LastModifierUserId]);
CREATE INDEX [IX_EmailTemplateRevisions_CreatorUserId] ON [EmailTemplateRevisions] ([CreatorUserId]);
CREATE INDEX [IX_EmailTemplateRevisions_EmailTemplateId] ON [EmailTemplateRevisions] ([EmailTemplateId]);
CREATE INDEX [IX_EmailTemplateRevisions_LastModifierUserId] ON [EmailTemplateRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_EmailTemplates_CreatorUserId] ON [EmailTemplates] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_EmailTemplates_DeveloperName] ON [EmailTemplates] ([DeveloperName])
WHERE [DeveloperName] IS NOT NULL;
CREATE INDEX [IX_EmailTemplates_LastModifierUserId] ON [EmailTemplates] ([LastModifierUserId]);
CREATE UNIQUE INDEX [IX_FailedLoginAttempts_EmailAddress] ON [FailedLoginAttempts] ([EmailAddress]);
CREATE UNIQUE INDEX [IX_JwtLogins_Jti] ON [JwtLogins] ([Jti])
WHERE [Jti] IS NOT NULL;
CREATE INDEX [IX_MediaItems_CreatorUserId] ON [MediaItems] ([CreatorUserId]);
CREATE INDEX [IX_MediaItems_LastModifierUserId] ON [MediaItems] ([LastModifierUserId]);
CREATE INDEX [IX_MediaItems_ObjectKey] ON [MediaItems] ([ObjectKey]);
CREATE INDEX [IX_NavigationMenuItems_CreatorUserId] ON [NavigationMenuItems] ([CreatorUserId]);
CREATE INDEX [IX_NavigationMenuItems_LastModifierUserId] ON [NavigationMenuItems] ([LastModifierUserId]);
CREATE INDEX [IX_NavigationMenuItems_NavigationMenuId] ON [NavigationMenuItems] ([NavigationMenuId]);
CREATE INDEX [IX_NavigationMenuItems_ParentNavigationMenuItemId] ON [NavigationMenuItems] ([ParentNavigationMenuItemId]);
CREATE INDEX [IX_NavigationMenuRevisions_CreatorUserId] ON [NavigationMenuRevisions] ([CreatorUserId]);
CREATE INDEX [IX_NavigationMenuRevisions_LastModifierUserId] ON [NavigationMenuRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_NavigationMenuRevisions_NavigationMenuId] ON [NavigationMenuRevisions] ([NavigationMenuId]);
CREATE INDEX [IX_NavigationMenus_CreatorUserId] ON [NavigationMenus] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_NavigationMenus_DeveloperName] ON [NavigationMenus] ([DeveloperName]);
CREATE INDEX [IX_NavigationMenus_LastModifierUserId] ON [NavigationMenus] ([LastModifierUserId]);
CREATE INDEX [IX_OneTimePasswords_UserId] ON [OneTimePasswords] ([UserId]);
CREATE INDEX [IX_RaythaFunctionRevisions_CreatorUserId] ON [RaythaFunctionRevisions] ([CreatorUserId]);
CREATE INDEX [IX_RaythaFunctionRevisions_LastModifierUserId] ON [RaythaFunctionRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_RaythaFunctionRevisions_RaythaFunctionId] ON [RaythaFunctionRevisions] ([RaythaFunctionId]);
CREATE INDEX [IX_RaythaFunctions_CreatorUserId] ON [RaythaFunctions] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_RaythaFunctions_DeveloperName] ON [RaythaFunctions] ([DeveloperName]);
CREATE INDEX [IX_RaythaFunctions_LastModifierUserId] ON [RaythaFunctions] ([LastModifierUserId]);
CREATE INDEX [IX_Roles_CreatorUserId] ON [Roles] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_Roles_DeveloperName] ON [Roles] ([DeveloperName]);
CREATE INDEX [IX_Roles_LastModifierUserId] ON [Roles] ([LastModifierUserId]);
CREATE INDEX [IX_RoleUser_UsersId] ON [RoleUser] ([UsersId]);
CREATE UNIQUE INDEX [IX_Routes_Path] ON [Routes] ([Path]);
CREATE INDEX [IX_SitePageRevisions_CreatorUserId] ON [SitePageRevisions] ([CreatorUserId]);
CREATE INDEX [IX_SitePageRevisions_LastModifierUserId] ON [SitePageRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_SitePageRevisions_SitePageId] ON [SitePageRevisions] ([SitePageId]);
CREATE INDEX [IX_SitePages_CreatorUserId] ON [SitePages] ([CreatorUserId]);
CREATE INDEX [IX_SitePages_LastModifierUserId] ON [SitePages] ([LastModifierUserId]);
CREATE UNIQUE INDEX [IX_SitePages_RouteId] ON [SitePages] ([RouteId]);
CREATE INDEX [IX_SitePages_WebTemplateId] ON [SitePages] ([WebTemplateId]);
CREATE INDEX [IX_ThemeAccessToMediaItems_MediaItemId] ON [ThemeAccessToMediaItems] ([MediaItemId]);
CREATE INDEX [IX_ThemeAccessToMediaItems_ThemeId] ON [ThemeAccessToMediaItems] ([ThemeId]);
CREATE INDEX [IX_Themes_CreatorUserId] ON [Themes] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_Themes_DeveloperName] ON [Themes] ([DeveloperName]);
CREATE INDEX [IX_Themes_LastModifierUserId] ON [Themes] ([LastModifierUserId]);
CREATE INDEX [IX_UserGroups_CreatorUserId] ON [UserGroups] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_UserGroups_DeveloperName] ON [UserGroups] ([DeveloperName]);
CREATE INDEX [IX_UserGroups_LastModifierUserId] ON [UserGroups] ([LastModifierUserId]);
CREATE INDEX [IX_Users_AuthenticationSchemeId] ON [Users] ([AuthenticationSchemeId]);
CREATE INDEX [IX_Users_CreatorUserId] ON [Users] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_Users_EmailAddress] ON [Users] ([EmailAddress]);
CREATE INDEX [IX_Users_LastModifierUserId] ON [Users] ([LastModifierUserId]);
CREATE UNIQUE INDEX [IX_Users_SsoId_AuthenticationSchemeId] ON [Users] ([SsoId], [AuthenticationSchemeId])
WHERE [SsoId] IS NOT NULL
    AND [AuthenticationSchemeId] IS NOT NULL;
CREATE INDEX [IX_UserUserGroup_UsersId] ON [UserUserGroup] ([UsersId]);
CREATE INDEX [IX_UserView_UserFavoritesId] ON [UserView] ([UserFavoritesId]);
CREATE INDEX [IX_VerificationCodes_CreatorUserId] ON [VerificationCodes] ([CreatorUserId]);
CREATE INDEX [IX_VerificationCodes_LastModifierUserId] ON [VerificationCodes] ([LastModifierUserId]);
CREATE INDEX [IX_Views_ContentTypeId] ON [Views] ([ContentTypeId]);
CREATE INDEX [IX_Views_CreatorUserId] ON [Views] ([CreatorUserId]);
CREATE INDEX [IX_Views_LastModifierUserId] ON [Views] ([LastModifierUserId]);
CREATE UNIQUE INDEX [IX_Views_RouteId] ON [Views] ([RouteId]);
CREATE INDEX [IX_WebTemplateAccessToModelDefinitions_ContentTypeId] ON [WebTemplateAccessToModelDefinitions] ([ContentTypeId]);
CREATE INDEX [IX_WebTemplateAccessToModelDefinitions_WebTemplateId] ON [WebTemplateAccessToModelDefinitions] ([WebTemplateId]);
CREATE INDEX [IX_WebTemplateContentItemRelations_ContentItemId] ON [WebTemplateContentItemRelations] ([ContentItemId]);
CREATE UNIQUE INDEX [IX_WebTemplateContentItemRelations_WebTemplateId_ContentItemId] ON [WebTemplateContentItemRelations] ([WebTemplateId], [ContentItemId]);
CREATE INDEX [IX_WebTemplateRevisions_CreatorUserId] ON [WebTemplateRevisions] ([CreatorUserId]);
CREATE INDEX [IX_WebTemplateRevisions_EmailTemplateId] ON [WebTemplateRevisions] ([EmailTemplateId]);
CREATE INDEX [IX_WebTemplateRevisions_LastModifierUserId] ON [WebTemplateRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_WebTemplateRevisions_WebTemplateId] ON [WebTemplateRevisions] ([WebTemplateId]);
CREATE INDEX [IX_WebTemplates_CreatorUserId] ON [WebTemplates] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_WebTemplates_DeveloperName_ThemeId] ON [WebTemplates] ([DeveloperName], [ThemeId])
WHERE [DeveloperName] IS NOT NULL;
CREATE INDEX [IX_WebTemplates_LastModifierUserId] ON [WebTemplates] ([LastModifierUserId]);
CREATE INDEX [IX_WebTemplates_ParentTemplateId] ON [WebTemplates] ([ParentTemplateId]);
CREATE INDEX [IX_WebTemplates_ThemeId] ON [WebTemplates] ([ThemeId]);
CREATE UNIQUE INDEX [IX_WebTemplateViewRelations_ViewId_WebTemplateId] ON [WebTemplateViewRelations] ([ViewId], [WebTemplateId]);
CREATE INDEX [IX_WebTemplateViewRelations_WebTemplateId] ON [WebTemplateViewRelations] ([WebTemplateId]);
CREATE INDEX [IX_WidgetTemplateRevisions_CreatorUserId] ON [WidgetTemplateRevisions] ([CreatorUserId]);
CREATE INDEX [IX_WidgetTemplateRevisions_LastModifierUserId] ON [WidgetTemplateRevisions] ([LastModifierUserId]);
CREATE INDEX [IX_WidgetTemplateRevisions_WidgetTemplateId] ON [WidgetTemplateRevisions] ([WidgetTemplateId]);
CREATE INDEX [IX_WidgetTemplates_CreatorUserId] ON [WidgetTemplates] ([CreatorUserId]);
CREATE UNIQUE INDEX [IX_WidgetTemplates_DeveloperName_ThemeId] ON [WidgetTemplates] ([DeveloperName], [ThemeId])
WHERE [DeveloperName] IS NOT NULL;
CREATE INDEX [IX_WidgetTemplates_LastModifierUserId] ON [WidgetTemplates] ([LastModifierUserId]);
CREATE INDEX [IX_WidgetTemplates_ThemeId] ON [WidgetTemplates] ([ThemeId]);
-- Add deferred foreign keys for AuthenticationSchemes
ALTER TABLE [AuthenticationSchemes]
ADD CONSTRAINT [FK_AuthenticationSchemes_Users_CreatorUserId] FOREIGN KEY ([CreatorUserId]) REFERENCES [Users] ([Id]);
ALTER TABLE [AuthenticationSchemes]
ADD CONSTRAINT [FK_AuthenticationSchemes_Users_LastModifierUserId] FOREIGN KEY ([LastModifierUserId]) REFERENCES [Users] ([Id]);
-- Insert default theme (like Postgres does)
INSERT INTO [Themes] (
        [Id],
        [Title],
        [DeveloperName],
        [IsExportable],
        [Description],
        [CreationTime]
    )
VALUES (
        'e31cb739-c764-4423-9f45-9dc6f3365766',
        N'Raytha default theme',
        N'raytha_default_theme',
        0,
        N'Raytha default theme',
        GETUTCDATE()
    );
-- Record all migrations as applied
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20221230221303_v0_9_0', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230211205159_v1_0_0', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20230521175706_v1_1_0', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240314124844_v1_2_0', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20240502121207_v1_3_0', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20241116192521_v1_4_0', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20241212094439_v1_4_1', N'10.0.0');
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251129154657_v1_5_0', N'10.0.0');
COMMIT;
GO
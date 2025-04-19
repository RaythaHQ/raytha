using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class v090 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Request = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JwtLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Jti = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwtLogins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpOverrideSystem = table.Column<bool>(type: "bit", nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: true),
                    SmtpUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpDefaultFromAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmtpDefaultFromName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomePageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsBuiltInAuth = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabledForUsers = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabledForAdmins = table.Column<bool>(type: "bit", nullable: false),
                    AuthenticationSchemeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MagicLinkExpiresInSeconds = table.Column<int>(type: "int", nullable: false),
                    SamlCertificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SamlIdpEntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JwtSecretKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JwtUseHighSecurity = table.Column<bool>(type: "bit", nullable: false),
                    SignInUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoginButtonText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignOutUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoggedInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Salt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SsoId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AuthenticationSchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsEmailAddressConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    RecentlyAccessedViews = table.Column<string>(name: "_RecentlyAccessedViews", type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_AuthenticationSchemes_AuthenticationSchemeId",
                        column: x => x.AuthenticationSchemeId,
                        principalTable: "AuthenticationSchemes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Users_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LabelPlural = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabelSingular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultRouteTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentTypes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bcc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBuiltInTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileStorageProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjectKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MediaItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OneTimePasswords",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "varbinary(900)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimePasswords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimePasswords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeveloperName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SystemPermissions = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Roles_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeveloperName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificationCodeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsBaseLayout = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsBuiltInTemplate = table.Column<bool>(type: "bit", nullable: false),
                    ParentTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllowAccessForNewContentTypes = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebTemplates_WebTemplates_ParentTemplateId",
                        column: x => x.ParentTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentTypeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    RelatedContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Choices = table.Column<string>(name: "_Choices", type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeleterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_RelatedContentTypeId",
                        column: x => x.RelatedContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeletedContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedContent = table.Column<string>(name: "_PublishedContent", type: "nvarchar(max)", nullable: true),
                    PrimaryField = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoutePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeletedContentItems_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeletedContentItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeletedContentItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bcc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContentTypeRolePermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypePermissions = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeRolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserUserGroup",
                columns: table => new
                {
                    UserGroupsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUserGroup", x => new { x.UserGroupsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserUserGroup_UserGroups_UserGroupsId",
                        column: x => x.UserGroupsId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUserGroup_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftContent = table.Column<string>(name: "_DraftContent", type: "nvarchar(max)", nullable: true),
                    PublishedContent = table.Column<string>(name: "_PublishedContent", type: "nvarchar(max)", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItems_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentItems_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentItems_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeveloperName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    Columns = table.Column<string>(name: "_Columns", type: "nvarchar(max)", nullable: true),
                    Filter = table.Column<string>(name: "_Filter", type: "nvarchar(max)", nullable: true),
                    Sort = table.Column<string>(name: "_Sort", type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Views_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Views_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Views_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Views_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Views_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebTemplateAccessToModelDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplateAccessToModelDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplateAccessToModelDefinitions_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebTemplateAccessToModelDefinitions_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowAccessForNewContentTypes = table.Column<bool>(type: "bit", nullable: false),
                    EmailTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentItemRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedContent = table.Column<string>(name: "_PublishedContent", type: "nvarchar(max)", nullable: true),
                    ContentItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItemRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItemRevisions_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentItemRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContentItemRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserView",
                columns: table => new
                {
                    FavoriteViewsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserFavoritesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserView", x => new { x.FavoriteViewsId, x.UserFavoritesId });
                    table.ForeignKey(
                        name: "FK_UserView_Users_UserFavoritesId",
                        column: x => x.UserFavoritesId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserView_Views_FavoriteViewsId",
                        column: x => x.FavoriteViewsId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreationTime",
                table: "AuditLogs",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_CreatorUserId",
                table: "AuthenticationSchemes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes",
                column: "DeveloperName",
                unique: true,
                filter: "[DeveloperName] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_LastModifierUserId",
                table: "AuthenticationSchemes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemRevisions_ContentItemId",
                table: "ContentItemRevisions",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemRevisions_CreatorUserId",
                table: "ContentItemRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemRevisions_LastModifierUserId",
                table: "ContentItemRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId",
                table: "ContentItems",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_CreatorUserId",
                table: "ContentItems",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_LastModifierUserId",
                table: "ContentItems",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_RouteId",
                table: "ContentItems",
                column: "RouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_WebTemplateId",
                table: "ContentItems",
                column: "WebTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_ContentTypeId",
                table: "ContentTypeFields",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_CreatorUserId",
                table: "ContentTypeFields",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_LastModifierUserId",
                table: "ContentTypeFields",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_RelatedContentTypeId",
                table: "ContentTypeFields",
                column: "RelatedContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_ContentTypeId",
                table: "ContentTypeRolePermission",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_CreatorUserId",
                table: "ContentTypeRolePermission",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_LastModifierUserId",
                table: "ContentTypeRolePermission",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_RoleId",
                table: "ContentTypeRolePermission",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_CreatorUserId",
                table: "ContentTypes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_LastModifierUserId",
                table: "ContentTypes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeletedContentItems_ContentTypeId",
                table: "DeletedContentItems",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeletedContentItems_CreatorUserId",
                table: "DeletedContentItems",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeletedContentItems_LastModifierUserId",
                table: "DeletedContentItems",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_CreatorUserId",
                table: "EmailTemplateRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_EmailTemplateId",
                table: "EmailTemplateRevisions",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_LastModifierUserId",
                table: "EmailTemplateRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CreatorUserId",
                table: "EmailTemplates",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_LastModifierUserId",
                table: "EmailTemplates",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins",
                column: "Jti",
                unique: true,
                filter: "[Jti] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CreatorUserId",
                table: "MediaItems",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_LastModifierUserId",
                table: "MediaItems",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_ObjectKey",
                table: "MediaItems",
                column: "ObjectKey");

            migrationBuilder.CreateIndex(
                name: "IX_OneTimePasswords_UserId",
                table: "OneTimePasswords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatorUserId",
                table: "Roles",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles",
                column: "DeveloperName",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_LastModifierUserId",
                table: "Roles",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Path",
                table: "Routes",
                column: "Path",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "ViewId", "ContentItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_CreatorUserId",
                table: "UserGroups",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups",
                column: "DeveloperName",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_LastModifierUserId",
                table: "UserGroups",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthenticationSchemeId",
                table: "Users",
                column: "AuthenticationSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatorUserId",
                table: "Users",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "FirstName", "LastName", "SsoId", "AuthenticationSchemeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastModifierUserId",
                table: "Users",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users",
                columns: new[] { "SsoId", "AuthenticationSchemeId" },
                unique: true,
                filter: "[SsoId] IS NOT NULL AND [AuthenticationSchemeId] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "EmailAddress", "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_UserUserGroup_UsersId",
                table: "UserUserGroup",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_UserView_UserFavoritesId",
                table: "UserView",
                column: "UserFavoritesId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_CreatorUserId",
                table: "VerificationCodes",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_LastModifierUserId",
                table: "VerificationCodes",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_ContentTypeId",
                table: "Views",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_CreatorUserId",
                table: "Views",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_LastModifierUserId",
                table: "Views",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Views_RouteId",
                table: "Views",
                column: "RouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Views_WebTemplateId",
                table: "Views",
                column: "WebTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateAccessToModelDefinitions_ContentTypeId",
                table: "WebTemplateAccessToModelDefinitions",
                column: "ContentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateAccessToModelDefinitions_WebTemplateId",
                table: "WebTemplateAccessToModelDefinitions",
                column: "WebTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_CreatorUserId",
                table: "WebTemplateRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_EmailTemplateId",
                table: "WebTemplateRevisions",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_LastModifierUserId",
                table: "WebTemplateRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_WebTemplateId",
                table: "WebTemplateRevisions",
                column: "WebTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_CreatorUserId",
                table: "WebTemplates",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_DeveloperName",
                table: "WebTemplates",
                column: "DeveloperName",
                unique: true,
                filter: "[DeveloperName] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_LastModifierUserId",
                table: "WebTemplates",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_ParentTemplateId",
                table: "WebTemplates",
                column: "ParentTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSchemes_Users_CreatorUserId",
                table: "AuthenticationSchemes",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSchemes_Users_LastModifierUserId",
                table: "AuthenticationSchemes",
                column: "LastModifierUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSchemes_Users_CreatorUserId",
                table: "AuthenticationSchemes");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSchemes_Users_LastModifierUserId",
                table: "AuthenticationSchemes");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ContentItemRevisions");

            migrationBuilder.DropTable(
                name: "ContentTypeFields");

            migrationBuilder.DropTable(
                name: "ContentTypeRolePermission");

            migrationBuilder.DropTable(
                name: "DeletedContentItems");

            migrationBuilder.DropTable(
                name: "EmailTemplateRevisions");

            migrationBuilder.DropTable(
                name: "JwtLogins");

            migrationBuilder.DropTable(
                name: "MediaItems");

            migrationBuilder.DropTable(
                name: "OneTimePasswords");

            migrationBuilder.DropTable(
                name: "OrganizationSettings");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "UserUserGroup");

            migrationBuilder.DropTable(
                name: "UserView");

            migrationBuilder.DropTable(
                name: "VerificationCodes");

            migrationBuilder.DropTable(
                name: "WebTemplateAccessToModelDefinitions");

            migrationBuilder.DropTable(
                name: "WebTemplateRevisions");

            migrationBuilder.DropTable(
                name: "ContentItems");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "Views");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "ContentTypes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "WebTemplates");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AuthenticationSchemes");
        }
    }
}

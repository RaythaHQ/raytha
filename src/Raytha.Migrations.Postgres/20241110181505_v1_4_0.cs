using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Raytha.Domain.Entities;

#nullable disable

namespace Raytha.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class v1_4_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Request = table.Column<string>(type: "text", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "BackgroundTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Args = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusInfo = table.Column<string>(type: "text", nullable: true),
                    PercentComplete = table.Column<int>(type: "integer", nullable: false),
                    NumberOfRetries = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CompletionTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    TaskStep = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundTasks", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "JwtLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jti = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwtLogins", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: true),
                    DateFormat = table.Column<string>(type: "text", nullable: true),
                    SmtpOverrideSystem = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpHost = table.Column<string>(type: "text", nullable: true),
                    SmtpPort = table.Column<int>(type: "integer", nullable: true),
                    SmtpUsername = table.Column<string>(type: "text", nullable: true),
                    SmtpPassword = table.Column<string>(type: "text", nullable: true),
                    SmtpDefaultFromAddress = table.Column<string>(type: "text", nullable: true),
                    SmtpDefaultFromName = table.Column<string>(type: "text", nullable: true),
                    HomePageId = table.Column<Guid>(type: "uuid", nullable: true),
                    HomePageType = table.Column<string>(type: "text", nullable: false),
                    ActiveThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "AuthenticationSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBuiltInAuth = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabledForUsers = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabledForAdmins = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticationSchemeType = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    MagicLinkExpiresInSeconds = table.Column<int>(type: "integer", nullable: false),
                    SamlCertificate = table.Column<string>(type: "text", nullable: true),
                    SamlIdpEntityId = table.Column<string>(type: "text", nullable: true),
                    JwtSecretKey = table.Column<string>(type: "text", nullable: true),
                    JwtUseHighSecurity = table.Column<bool>(type: "boolean", nullable: false),
                    SignInUrl = table.Column<string>(type: "text", nullable: true),
                    LoginButtonText = table.Column<string>(type: "text", nullable: true),
                    SignOutUrl = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationSchemes", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoggedInTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Salt = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    SsoId = table.Column<string>(type: "text", nullable: true),
                    AuthenticationSchemeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    IsEmailAddressConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    _RecentlyAccessedViews = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_AuthenticationSchemes_AuthenticationSchemeId",
                        column: x => x.AuthenticationSchemeId,
                        principalTable: "AuthenticationSchemes",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Users_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LabelPlural = table.Column<string>(type: "text", nullable: true),
                    LabelSingular = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultRouteTemplate = table.Column<string>(type: "text", nullable: true),
                    PrimaryFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    Cc = table.Column<string>(type: "text", nullable: true),
                    Bcc = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsBuiltInTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileStorageProvider = table.Column<string>(type: "text", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_MediaItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "NavigationMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    IsMainMenu = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenus_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_NavigationMenus_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "OneTimePasswords",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ExpiresAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimePasswords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OneTimePasswords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RaythaFunctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    TriggerType = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaythaFunctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaythaFunctions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RaythaFunctions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    SystemPermissions = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Roles_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsExportable = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Themes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Themes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    DeveloperName = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "VerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    VerificationCodeType = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false),
                    _DraftContent = table.Column<string>(type: "text", nullable: true),
                    _PublishedContent = table.Column<string>(type: "jsonb", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItems_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ContentItems_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContentTypeFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FieldOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedContentTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldType = table.Column<string>(type: "text", nullable: false),
                    _Choices = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleterUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletionTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_ContentTypes_RelatedContentTypeId",
                        column: x => x.RelatedContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypeFields_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DeletedContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    _PublishedContent = table.Column<string>(type: "text", nullable: true),
                    PrimaryField = table.Column<string>(type: "text", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutePath = table.Column<string>(type: "text", nullable: false),
                    WebTemplateIdsJson = table.Column<string>(type: "text", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeletedContentItems_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_DeletedContentItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_DeletedContentItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Views",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultNumberOfItemsPerPage = table.Column<int>(
                        type: "integer",
                        nullable: false
                    ),
                    MaxNumberOfItemsPerPage = table.Column<int>(type: "integer", nullable: false),
                    IgnoreClientFilterAndSortQueryParams = table.Column<bool>(
                        type: "boolean",
                        nullable: false
                    ),
                    _Columns = table.Column<string>(type: "text", nullable: true),
                    _Filter = table.Column<string>(type: "text", nullable: true),
                    _Sort = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Views_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Views_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Views_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_Views_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "EmailTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Cc = table.Column<string>(type: "text", nullable: true),
                    Bcc = table.Column<string>(type: "text", nullable: true),
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_EmailTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "NavigationMenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    OpenInNewTab = table.Column<bool>(type: "boolean", nullable: false),
                    CssClassName = table.Column<string>(type: "text", nullable: true),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ParentNavigationMenuItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    NavigationMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_NavigationMenuItems_ParentNavigationMen~",
                        column: x => x.ParentNavigationMenuItemId,
                        principalTable: "NavigationMenuItems",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_NavigationMenus_NavigationMenuId",
                        column: x => x.NavigationMenuId,
                        principalTable: "NavigationMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "NavigationMenuRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NavigationMenuItemsJson = table.Column<string>(type: "text", nullable: false),
                    NavigationMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenuRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenuRevisions_NavigationMenus_NavigationMenuId",
                        column: x => x.NavigationMenuId,
                        principalTable: "NavigationMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_NavigationMenuRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_NavigationMenuRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RaythaFunctionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    RaythaFunctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaythaFunctionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaythaFunctionRevisions_RaythaFunctions_RaythaFunctionId",
                        column: x => x.RaythaFunctionId,
                        principalTable: "RaythaFunctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RaythaFunctionRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_RaythaFunctionRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContentTypeRolePermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypePermissions = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypeRolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_ContentTypes_ContentTypeId",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentTypeRolePermission_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_RoleUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ThemeAccessToMediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThemeAccessToMediaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThemeAccessToMediaItems_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ThemeAccessToMediaItems_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WebTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBaseLayout = table.Column<bool>(type: "boolean", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsBuiltInTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    ParentTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowAccessForNewContentTypes = table.Column<bool>(
                        type: "boolean",
                        nullable: false
                    ),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplates_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplates_WebTemplates_ParentTemplateId",
                        column: x => x.ParentTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserUserGroup",
                columns: table => new
                {
                    UserGroupsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUserGroup", x => new { x.UserGroupsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_UserUserGroup_UserGroups_UserGroupsId",
                        column: x => x.UserGroupsId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserUserGroup_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContentItemRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    _PublishedContent = table.Column<string>(type: "text", nullable: true),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentItemRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentItemRevisions_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_ContentItemRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ContentItemRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "UserView",
                columns: table => new
                {
                    FavoriteViewsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserFavoritesId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_UserView",
                        x => new { x.FavoriteViewsId, x.UserFavoritesId }
                    );
                    table.ForeignKey(
                        name: "FK_UserView_Users_UserFavoritesId",
                        column: x => x.UserFavoritesId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserView_Views_FavoriteViewsId",
                        column: x => x.FavoriteViewsId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WebTemplateAccessToModelDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplateAccessToModelDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplateAccessToModelDefinitions_ContentTypes_ContentTyp~",
                        column: x => x.ContentTypeId,
                        principalTable: "ContentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplateAccessToModelDefinitions_WebTemplates_WebTemplat~",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WebTemplateContentItemRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplateContentItemRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplateContentItemRelations_ContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalTable: "ContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplateContentItemRelations_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WebTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    WebTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowAccessForNewContentTypes = table.Column<bool>(
                        type: "boolean",
                        nullable: false
                    ),
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastModificationTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplateRevisions_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WebTemplateViewRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebTemplateViewRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebTemplateViewRelations_Views_ViewId",
                        column: x => x.ViewId,
                        principalTable: "Views",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_WebTemplateViewRelations_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ApiKeyHash",
                table: "ApiKeys",
                column: "ApiKeyHash",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatorUserId",
                table: "ApiKeys",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                table: "ApiKeys",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreationTime",
                table: "AuditLogs",
                column: "CreationTime"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_CreatorUserId",
                table: "AuthenticationSchemes",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_LastModifierUserId",
                table: "AuthenticationSchemes",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemRevisions_ContentItemId",
                table: "ContentItemRevisions",
                column: "ContentItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemRevisions_CreatorUserId",
                table: "ContentItemRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItemRevisions_LastModifierUserId",
                table: "ContentItemRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_ContentTypeId",
                table: "ContentItems",
                column: "ContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_CreatorUserId",
                table: "ContentItems",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_LastModifierUserId",
                table: "ContentItems",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentItems_RouteId",
                table: "ContentItems",
                column: "RouteId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_ContentTypeId",
                table: "ContentTypeFields",
                column: "ContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_CreatorUserId",
                table: "ContentTypeFields",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_LastModifierUserId",
                table: "ContentTypeFields",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeFields_RelatedContentTypeId",
                table: "ContentTypeFields",
                column: "RelatedContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_ContentTypeId",
                table: "ContentTypeRolePermission",
                column: "ContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_CreatorUserId",
                table: "ContentTypeRolePermission",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_LastModifierUserId",
                table: "ContentTypeRolePermission",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypeRolePermission_RoleId",
                table: "ContentTypeRolePermission",
                column: "RoleId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_CreatorUserId",
                table: "ContentTypes",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypes_LastModifierUserId",
                table: "ContentTypes",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DeletedContentItems_ContentTypeId",
                table: "DeletedContentItems",
                column: "ContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DeletedContentItems_CreatorUserId",
                table: "DeletedContentItems",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_DeletedContentItems_LastModifierUserId",
                table: "DeletedContentItems",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_CreatorUserId",
                table: "EmailTemplateRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_EmailTemplateId",
                table: "EmailTemplateRevisions",
                column: "EmailTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplateRevisions_LastModifierUserId",
                table: "EmailTemplateRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_CreatorUserId",
                table: "EmailTemplates",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_DeveloperName",
                table: "EmailTemplates",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_LastModifierUserId",
                table: "EmailTemplates",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins",
                column: "Jti",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CreatorUserId",
                table: "MediaItems",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_LastModifierUserId",
                table: "MediaItems",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_ObjectKey",
                table: "MediaItems",
                column: "ObjectKey"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_CreatorUserId",
                table: "NavigationMenuItems",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_LastModifierUserId",
                table: "NavigationMenuItems",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_NavigationMenuId",
                table: "NavigationMenuItems",
                column: "NavigationMenuId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_ParentNavigationMenuItemId",
                table: "NavigationMenuItems",
                column: "ParentNavigationMenuItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRevisions_CreatorUserId",
                table: "NavigationMenuRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRevisions_LastModifierUserId",
                table: "NavigationMenuRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRevisions_NavigationMenuId",
                table: "NavigationMenuRevisions",
                column: "NavigationMenuId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_CreatorUserId",
                table: "NavigationMenus",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_DeveloperName",
                table: "NavigationMenus",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_LastModifierUserId",
                table: "NavigationMenus",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_OneTimePasswords_UserId",
                table: "OneTimePasswords",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctionRevisions_CreatorUserId",
                table: "RaythaFunctionRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctionRevisions_LastModifierUserId",
                table: "RaythaFunctionRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctionRevisions_RaythaFunctionId",
                table: "RaythaFunctionRevisions",
                column: "RaythaFunctionId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_CreatorUserId",
                table: "RaythaFunctions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_DeveloperName",
                table: "RaythaFunctions",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_LastModifierUserId",
                table: "RaythaFunctions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatorUserId",
                table: "Roles",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Roles_LastModifierUserId",
                table: "Roles",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Path",
                table: "Routes",
                column: "Path",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ThemeAccessToMediaItems_MediaItemId",
                table: "ThemeAccessToMediaItems",
                column: "MediaItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ThemeAccessToMediaItems_ThemeId",
                table: "ThemeAccessToMediaItems",
                column: "ThemeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Themes_CreatorUserId",
                table: "Themes",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Themes_DeveloperName",
                table: "Themes",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Themes_LastModifierUserId",
                table: "Themes",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_CreatorUserId",
                table: "UserGroups",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups",
                column: "DeveloperName",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_LastModifierUserId",
                table: "UserGroups",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_AuthenticationSchemeId",
                table: "Users",
                column: "AuthenticationSchemeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatorUserId",
                table: "Users",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastModifierUserId",
                table: "Users",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users",
                columns: new[] { "SsoId", "AuthenticationSchemeId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserUserGroup_UsersId",
                table: "UserUserGroup",
                column: "UsersId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserView_UserFavoritesId",
                table: "UserView",
                column: "UserFavoritesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_CreatorUserId",
                table: "VerificationCodes",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_LastModifierUserId",
                table: "VerificationCodes",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Views_ContentTypeId",
                table: "Views",
                column: "ContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Views_CreatorUserId",
                table: "Views",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Views_LastModifierUserId",
                table: "Views",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Views_RouteId",
                table: "Views",
                column: "RouteId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateAccessToModelDefinitions_ContentTypeId",
                table: "WebTemplateAccessToModelDefinitions",
                column: "ContentTypeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateAccessToModelDefinitions_WebTemplateId",
                table: "WebTemplateAccessToModelDefinitions",
                column: "WebTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateContentItemRelations_ContentItemId",
                table: "WebTemplateContentItemRelations",
                column: "ContentItemId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateContentItemRelations_WebTemplateId_ContentItemId",
                table: "WebTemplateContentItemRelations",
                columns: new[] { "WebTemplateId", "ContentItemId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_CreatorUserId",
                table: "WebTemplateRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_EmailTemplateId",
                table: "WebTemplateRevisions",
                column: "EmailTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_LastModifierUserId",
                table: "WebTemplateRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateRevisions_WebTemplateId",
                table: "WebTemplateRevisions",
                column: "WebTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_CreatorUserId",
                table: "WebTemplates",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_DeveloperName_ThemeId",
                table: "WebTemplates",
                columns: new[] { "DeveloperName", "ThemeId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_LastModifierUserId",
                table: "WebTemplates",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_ParentTemplateId",
                table: "WebTemplates",
                column: "ParentTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_ThemeId",
                table: "WebTemplates",
                column: "ThemeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateViewRelations_ViewId_WebTemplateId",
                table: "WebTemplateViewRelations",
                columns: new[] { "ViewId", "WebTemplateId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplateViewRelations_WebTemplateId",
                table: "WebTemplateViewRelations",
                column: "WebTemplateId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Users_CreatorUserId",
                table: "ApiKeys",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Users_UserId",
                table: "ApiKeys",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSchemes_Users_CreatorUserId",
                table: "AuthenticationSchemes",
                column: "CreatorUserId",
                principalTable: "Users",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationSchemes_Users_LastModifierUserId",
                table: "AuthenticationSchemes",
                column: "LastModifierUserId",
                principalTable: "Users",
                principalColumn: "Id"
            );

            var defaultThemeId = Guid.NewGuid();

            migrationBuilder.InsertData(
                table: "Themes",
                columns: new[]
                {
                    "Id",
                    "Title",
                    "DeveloperName",
                    "IsExportable",
                    "Description",
                    "CreationTime",
                },
                values: new object[,]
                {
                    {
                        defaultThemeId,
                        "Raytha default theme",
                        Theme.DEFAULT_THEME_DEVELOPER_NAME,
                        false,
                        "Raytha default theme",
                        DateTime.UtcNow,
                    },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSchemes_Users_CreatorUserId",
                table: "AuthenticationSchemes"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationSchemes_Users_LastModifierUserId",
                table: "AuthenticationSchemes"
            );

            migrationBuilder.DropTable(name: "ApiKeys");

            migrationBuilder.DropTable(name: "AuditLogs");

            migrationBuilder.DropTable(name: "BackgroundTasks");

            migrationBuilder.DropTable(name: "ContentItemRevisions");

            migrationBuilder.DropTable(name: "ContentTypeFields");

            migrationBuilder.DropTable(name: "ContentTypeRolePermission");

            migrationBuilder.DropTable(name: "DataProtectionKeys");

            migrationBuilder.DropTable(name: "DeletedContentItems");

            migrationBuilder.DropTable(name: "EmailTemplateRevisions");

            migrationBuilder.DropTable(name: "JwtLogins");

            migrationBuilder.DropTable(name: "NavigationMenuItems");

            migrationBuilder.DropTable(name: "NavigationMenuRevisions");

            migrationBuilder.DropTable(name: "OneTimePasswords");

            migrationBuilder.DropTable(name: "OrganizationSettings");

            migrationBuilder.DropTable(name: "RaythaFunctionRevisions");

            migrationBuilder.DropTable(name: "RoleUser");

            migrationBuilder.DropTable(name: "ThemeAccessToMediaItems");

            migrationBuilder.DropTable(name: "UserUserGroup");

            migrationBuilder.DropTable(name: "UserView");

            migrationBuilder.DropTable(name: "VerificationCodes");

            migrationBuilder.DropTable(name: "WebTemplateAccessToModelDefinitions");

            migrationBuilder.DropTable(name: "WebTemplateContentItemRelations");

            migrationBuilder.DropTable(name: "WebTemplateRevisions");

            migrationBuilder.DropTable(name: "WebTemplateViewRelations");

            migrationBuilder.DropTable(name: "NavigationMenus");

            migrationBuilder.DropTable(name: "RaythaFunctions");

            migrationBuilder.DropTable(name: "Roles");

            migrationBuilder.DropTable(name: "MediaItems");

            migrationBuilder.DropTable(name: "UserGroups");

            migrationBuilder.DropTable(name: "ContentItems");

            migrationBuilder.DropTable(name: "EmailTemplates");

            migrationBuilder.DropTable(name: "Views");

            migrationBuilder.DropTable(name: "WebTemplates");

            migrationBuilder.DropTable(name: "ContentTypes");

            migrationBuilder.DropTable(name: "Routes");

            migrationBuilder.DropTable(name: "Themes");

            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.DropTable(name: "AuthenticationSchemes");
        }
    }
}

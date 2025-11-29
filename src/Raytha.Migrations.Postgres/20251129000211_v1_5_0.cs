using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class v1_5_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SitePageId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.AddColumn<int>(
                name: "BruteForceProtectionMaxFailedAttempts",
                table: "AuthenticationSchemes",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "BruteForceProtectionWindowInSeconds",
                table: "AuthenticationSchemes",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "FailedLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastFailedAttemptAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedLoginAttempts", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SitePages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsDraft = table.Column<bool>(type: "boolean", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    WebTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    _DraftWidgetsJson = table.Column<string>(type: "jsonb", nullable: true),
                    _PublishedWidgetsJson = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_SitePages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SitePages_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SitePages_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SitePages_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SitePages_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WidgetTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_WidgetTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetTemplates_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_WidgetTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WidgetTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SitePageRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    _PublishedWidgetsJson = table.Column<string>(type: "jsonb", nullable: true),
                    SitePageId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SitePageRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SitePageRevisions_SitePages_SitePageId",
                        column: x => x.SitePageId,
                        principalTable: "SitePages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_SitePageRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_SitePageRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WidgetTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    WidgetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_WidgetTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId",
                        column: x => x.WidgetTemplateId,
                        principalTable: "WidgetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_FailedLoginAttempts_EmailAddress",
                table: "FailedLoginAttempts",
                column: "EmailAddress",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePageRevisions_CreatorUserId",
                table: "SitePageRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePageRevisions_LastModifierUserId",
                table: "SitePageRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePageRevisions_SitePageId",
                table: "SitePageRevisions",
                column: "SitePageId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_CreatorUserId",
                table: "SitePages",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_LastModifierUserId",
                table: "SitePages",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_RouteId",
                table: "SitePages",
                column: "RouteId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_WebTemplateId",
                table: "SitePages",
                column: "WebTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_CreatorUserId",
                table: "WidgetTemplateRevisions",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_LastModifierUserId",
                table: "WidgetTemplateRevisions",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_WidgetTemplateId",
                table: "WidgetTemplateRevisions",
                column: "WidgetTemplateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_CreatorUserId",
                table: "WidgetTemplates",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_DeveloperName_ThemeId",
                table: "WidgetTemplates",
                columns: new[] { "DeveloperName", "ThemeId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_LastModifierUserId",
                table: "WidgetTemplates",
                column: "LastModifierUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_ThemeId",
                table: "WidgetTemplates",
                column: "ThemeId"
            );

            // Grant ManageSitePages permission (64) to built-in roles
            migrationBuilder.Sql(
                @"
                UPDATE ""Roles""
                SET ""SystemPermissions"" = ""SystemPermissions"" | 64
                WHERE ""DeveloperName"" IN ('super_admin', 'admin', 'editor');
            "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove ManageSitePages permission (64) from built-in roles
            migrationBuilder.Sql(
                @"
                UPDATE ""Roles""
                SET ""SystemPermissions"" = ""SystemPermissions"" & ~64
                WHERE ""DeveloperName"" IN ('super_admin', 'admin', 'editor');
            "
            );

            migrationBuilder.DropTable(name: "FailedLoginAttempts");

            migrationBuilder.DropTable(name: "SitePageRevisions");

            migrationBuilder.DropTable(name: "WidgetTemplateRevisions");

            migrationBuilder.DropTable(name: "SitePages");

            migrationBuilder.DropTable(name: "WidgetTemplates");

            migrationBuilder.DropColumn(name: "SitePageId", table: "Routes");

            migrationBuilder.DropColumn(
                name: "BruteForceProtectionMaxFailedAttempts",
                table: "AuthenticationSchemes"
            );

            migrationBuilder.DropColumn(
                name: "BruteForceProtectionWindowInSeconds",
                table: "AuthenticationSchemes"
            );
        }
    }
}

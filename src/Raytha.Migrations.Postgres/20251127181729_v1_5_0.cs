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
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                    _WidgetsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitePages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SitePages_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SitePages_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SitePages_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SitePages_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetTemplates_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WidgetTemplates_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WidgetTemplates_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WidgetTemplateRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    WidgetTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId",
                        column: x => x.WidgetTemplateId,
                        principalTable: "WidgetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_CreatorUserId",
                table: "SitePages",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_LastModifierUserId",
                table: "SitePages",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_RouteId",
                table: "SitePages",
                column: "RouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_WebTemplateId",
                table: "SitePages",
                column: "WebTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_CreatorUserId",
                table: "WidgetTemplateRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_LastModifierUserId",
                table: "WidgetTemplateRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_WidgetTemplateId",
                table: "WidgetTemplateRevisions",
                column: "WidgetTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_CreatorUserId",
                table: "WidgetTemplates",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_LastModifierUserId",
                table: "WidgetTemplates",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_ThemeId",
                table: "WidgetTemplates",
                column: "ThemeId");

            // Update super_admin and admin roles to include ManageSitePages permission (64)
            migrationBuilder.Sql(
                @"
                UPDATE ""Roles""
                SET ""SystemPermissions"" = ""SystemPermissions"" | 64
                WHERE ""DeveloperName"" IN ('super_admin', 'admin')
                "
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove ManageSitePages permission from roles
            migrationBuilder.Sql(
                @"
                UPDATE ""Roles""
                SET ""SystemPermissions"" = ""SystemPermissions"" & ~64
                WHERE ""DeveloperName"" IN ('super_admin', 'admin')
                "
            );

            migrationBuilder.DropTable(
                name: "SitePages");

            migrationBuilder.DropTable(
                name: "WidgetTemplateRevisions");

            migrationBuilder.DropTable(
                name: "WidgetTemplates");

            migrationBuilder.DropColumn(
                name: "SitePageId",
                table: "Routes");
        }
    }
}

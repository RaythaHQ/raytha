using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Raytha.Domain.Entities;

#nullable disable

namespace Raytha.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class v1_5_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create SitePages table
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
                    _WidgetsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_SitePages_WebTemplates_WebTemplateId",
                        column: x => x.WebTemplateId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
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
                }
            );

            // Add SitePageId column to Routes table
            migrationBuilder.AddColumn<Guid>(
                name: "SitePageId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty
            );

            // Create indexes for SitePages
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
                name: "IX_SitePages_CreatorUserId",
                table: "SitePages",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SitePages_LastModifierUserId",
                table: "SitePages",
                column: "LastModifierUserId"
            );

            // Create WidgetTemplates table
            migrationBuilder.CreateTable(
                name: "WidgetTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: true),
                    DeveloperName = table.Column<string>(type: "character varying(450)", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsBuiltInTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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

            // Create WidgetTemplateRevisions table
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
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetTemplateRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetTemplateRevisions_WidgetTemplates_WidgetTemplateId",
                        column: x => x.WidgetTemplateId,
                        principalTable: "WidgetTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
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
                }
            );

            // Create indexes for WidgetTemplates
            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_ThemeId",
                table: "WidgetTemplates",
                column: "ThemeId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_DeveloperName_ThemeId",
                table: "WidgetTemplates",
                columns: new[] { "DeveloperName", "ThemeId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_CreatorUserId",
                table: "WidgetTemplates",
                column: "CreatorUserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplates_LastModifierUserId",
                table: "WidgetTemplates",
                column: "LastModifierUserId"
            );

            // Create indexes for WidgetTemplateRevisions
            migrationBuilder.CreateIndex(
                name: "IX_WidgetTemplateRevisions_WidgetTemplateId",
                table: "WidgetTemplateRevisions",
                column: "WidgetTemplateId"
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

            // Drop WidgetTemplateRevisions table
            migrationBuilder.DropTable(name: "WidgetTemplateRevisions");

            // Drop WidgetTemplates table
            migrationBuilder.DropTable(name: "WidgetTemplates");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_SitePages_RouteId",
                table: "SitePages"
            );

            migrationBuilder.DropIndex(
                name: "IX_SitePages_WebTemplateId",
                table: "SitePages"
            );

            migrationBuilder.DropIndex(
                name: "IX_SitePages_CreatorUserId",
                table: "SitePages"
            );

            migrationBuilder.DropIndex(
                name: "IX_SitePages_LastModifierUserId",
                table: "SitePages"
            );

            // Drop SitePageId column from Routes
            migrationBuilder.DropColumn(
                name: "SitePageId",
                table: "Routes"
            );

            // Drop SitePages table
            migrationBuilder.DropTable(name: "SitePages");
        }
    }
}


using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v1_3_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NavigationMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeveloperName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsMainMenu = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenus_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NavigationMenus_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NavigationMenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    OpenInNewTab = table.Column<bool>(type: "bit", nullable: false),
                    CssClassName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    ParentNavigationMenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NavigationMenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_NavigationMenuItems_ParentNavigationMenuItemId",
                        column: x => x.ParentNavigationMenuItemId,
                        principalTable: "NavigationMenuItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_NavigationMenus_NavigationMenuId",
                        column: x => x.NavigationMenuId,
                        principalTable: "NavigationMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NavigationMenuItems_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NavigationMenuRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NavigationMenuItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NavigationMenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationMenuRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NavigationMenuRevisions_NavigationMenus_NavigationMenuId",
                        column: x => x.NavigationMenuId,
                        principalTable: "NavigationMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NavigationMenuRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NavigationMenuRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_CreatorUserId",
                table: "NavigationMenuItems",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_LastModifierUserId",
                table: "NavigationMenuItems",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_NavigationMenuId",
                table: "NavigationMenuItems",
                column: "NavigationMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuItems_ParentNavigationMenuItemId",
                table: "NavigationMenuItems",
                column: "ParentNavigationMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRevisions_CreatorUserId",
                table: "NavigationMenuRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRevisions_LastModifierUserId",
                table: "NavigationMenuRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenuRevisions_NavigationMenuId",
                table: "NavigationMenuRevisions",
                column: "NavigationMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_CreatorUserId",
                table: "NavigationMenus",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_DeveloperName",
                table: "NavigationMenus",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NavigationMenus_LastModifierUserId",
                table: "NavigationMenus",
                column: "LastModifierUserId");

            var navigationMenuId = Guid.NewGuid();
            migrationBuilder.InsertData(
                table: "NavigationMenus",
                columns: new[] { "Id", "Label", "DeveloperName", "IsMainMenu", "CreationTime" },
                values: new object[] { navigationMenuId, "Main menu", "mainmenu", true, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "NavigationMenuItems",
                columns: new[] { "Id", "Label", "Url", "IsDisabled", "OpenInNewTab", "CssClassName", "Ordinal", "NavigationMenuId", "CreationTime" },
                values: new object[,]
                {
                    {Guid.NewGuid(), "Home", "/home", false, false, "nav-link", 1, navigationMenuId, DateTime.UtcNow },
                    {Guid.NewGuid(), "About", "/about", false, false, "nav-link", 2, navigationMenuId, DateTime.UtcNow },
                    {Guid.NewGuid(), "Posts", "/posts", false, false, "nav-link", 3, navigationMenuId, DateTime.UtcNow },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavigationMenuItems");

            migrationBuilder.DropTable(
                name: "NavigationMenuRevisions");

            migrationBuilder.DropTable(
                name: "NavigationMenus");
        }
    }
}

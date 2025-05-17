using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class v100 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultNumberOfItemsPerPage",
                table: "Views",
                type: "int",
                nullable: false,
                defaultValue: 25
            );

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreClientFilterAndSortQueryParams",
                table: "Views",
                type: "bit",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "MaxNumberOfItemsPerPage",
                table: "Views",
                type: "int",
                nullable: false,
                defaultValue: 1000
            );

            migrationBuilder.AddColumn<string>(
                name: "HomePageType",
                table: "OrganizationSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "ContentItem"
            );

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiKeyHash = table.Column<byte[]>(type: "varbinary(900)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id"
                    );
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ApiKeys");

            migrationBuilder.DropTable(name: "DataProtectionKeys");

            migrationBuilder.DropColumn(name: "DefaultNumberOfItemsPerPage", table: "Views");

            migrationBuilder.DropColumn(
                name: "IgnoreClientFilterAndSortQueryParams",
                table: "Views"
            );

            migrationBuilder.DropColumn(name: "MaxNumberOfItemsPerPage", table: "Views");

            migrationBuilder.DropColumn(name: "HomePageType", table: "OrganizationSettings");
        }
    }
}

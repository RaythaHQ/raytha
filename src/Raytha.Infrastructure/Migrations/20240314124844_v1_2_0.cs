using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v1_2_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaythaFunctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeveloperName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaythaFunctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaythaFunctions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RaythaFunctions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RaythaFunctionRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaythaFunctionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaythaFunctionRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaythaFunctionRevisions_RaythaFunctions_RaythaFunctionId",
                        column: x => x.RaythaFunctionId,
                        principalTable: "RaythaFunctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaythaFunctionRevisions_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RaythaFunctionRevisions_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctionRevisions_CreatorUserId",
                table: "RaythaFunctionRevisions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctionRevisions_LastModifierUserId",
                table: "RaythaFunctionRevisions",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctionRevisions_RaythaFunctionId",
                table: "RaythaFunctionRevisions",
                column: "RaythaFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_CreatorUserId",
                table: "RaythaFunctions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_DeveloperName",
                table: "RaythaFunctions",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_LastModifierUserId",
                table: "RaythaFunctions",
                column: "LastModifierUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaythaFunctionRevisions");

            migrationBuilder.DropTable(
                name: "RaythaFunctions");
        }
    }
}

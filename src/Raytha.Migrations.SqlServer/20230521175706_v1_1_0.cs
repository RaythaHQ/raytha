using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class v110 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Args = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PercentComplete = table.Column<int>(type: "int", nullable: false),
                    NumberOfRetries = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: true
                    ),
                    CompletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskStep = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundTasks", x => x.Id);
                }
            );

            migrationBuilder.Sql("ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON;", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BackgroundTasks");

            migrationBuilder.Sql("ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION OFF;", true);
        }
    }
}

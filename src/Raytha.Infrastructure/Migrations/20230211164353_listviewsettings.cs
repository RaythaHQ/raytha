using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class listviewsettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultNumberOfItemsPerPage",
                table: "Views",
                type: "int",
                nullable: false,
                defaultValue: 25);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreClientSideQueryParams",
                table: "Views",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxNumberOfItemsPerPage",
                table: "Views",
                type: "int",
                nullable: false,
                defaultValue: 1000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultNumberOfItemsPerPage",
                table: "Views");

            migrationBuilder.DropColumn(
                name: "IgnoreClientSideQueryParams",
                table: "Views");

            migrationBuilder.DropColumn(
                name: "MaxNumberOfItemsPerPage",
                table: "Views");
        }
    }
}

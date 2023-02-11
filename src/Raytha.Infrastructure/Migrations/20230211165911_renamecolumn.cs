using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class renamecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgnoreClientSideQueryParams",
                table: "Views",
                newName: "IgnoreClientFilterAndSortQueryParams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgnoreClientFilterAndSortQueryParams",
                table: "Views",
                newName: "IgnoreClientSideQueryParams");
        }
    }
}

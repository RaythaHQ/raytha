using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class v1_4_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebTemplates_DeveloperName_ThemeId",
                table: "WebTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailAddress",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Path",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes");

            migrationBuilder.AlterColumn<string>(
                name: "DeveloperName",
                table: "EmailTemplates",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_DeveloperName_ThemeId",
                table: "WebTemplates",
                columns: new[] { "DeveloperName", "ThemeId" },
                unique: true,
                filter: "[DeveloperName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users",
                columns: new[] { "SsoId", "AuthenticationSchemeId" },
                unique: true,
                filter: "[SsoId] IS NOT NULL AND [AuthenticationSchemeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Path",
                table: "Routes",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles",
                column: "DeveloperName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins",
                column: "Jti",
                unique: true,
                filter: "[Jti] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_DeveloperName",
                table: "EmailTemplates",
                column: "DeveloperName",
                unique: true,
                filter: "[DeveloperName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes",
                column: "DeveloperName",
                unique: true,
                filter: "[DeveloperName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WebTemplates_DeveloperName_ThemeId",
                table: "WebTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmailAddress",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups");

            migrationBuilder.DropIndex(
                name: "IX_Routes_Path",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_DeveloperName",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes");

            migrationBuilder.AlterColumn<string>(
                name: "DeveloperName",
                table: "EmailTemplates",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebTemplates_DeveloperName_ThemeId",
                table: "WebTemplates",
                columns: new[] { "DeveloperName", "ThemeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "FirstName", "LastName", "SsoId", "AuthenticationSchemeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SsoId_AuthenticationSchemeId",
                table: "Users",
                columns: new[] { "SsoId", "AuthenticationSchemeId" },
                unique: true,
                filter: "[SsoId] IS NOT NULL AND [AuthenticationSchemeId] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "EmailAddress", "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_DeveloperName",
                table: "UserGroups",
                column: "DeveloperName",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_Path",
                table: "Routes",
                column: "Path",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "ViewId", "ContentItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_DeveloperName",
                table: "Roles",
                column: "DeveloperName",
                unique: true)
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });

            migrationBuilder.CreateIndex(
                name: "IX_JwtLogins_Jti",
                table: "JwtLogins",
                column: "Jti",
                unique: true,
                filter: "[Jti] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationSchemes_DeveloperName",
                table: "AuthenticationSchemes",
                column: "DeveloperName",
                unique: true,
                filter: "[DeveloperName] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Id", "Label" });
        }
    }
}

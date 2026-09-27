using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class v2_2_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditLogRetentionDays",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 180);

            migrationBuilder.AddColumn<int>(
                name: "BackgroundTaskRetentionDays",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 180);

            migrationBuilder.AddColumn<int>(
                name: "EmailLogRetentionDays",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 180);

            migrationBuilder.AddColumn<int>(
                name: "WebhookDeliveryRetentionDays",
                table: "OrganizationSettings",
                type: "integer",
                nullable: false,
                defaultValue: 180);

            // Media access used to ride on Manage Content Types or Edit on any content type (admin
            // media library), and on Manage System Settings (API v1 listing). Keep all of it now that
            // Manage Media is its own permission: SystemPermissions 1 | 4 -> 128, content type Edit = 2.
            migrationBuilder.Sql(
                """
                UPDATE "Roles" SET "SystemPermissions" = "SystemPermissions" | 128
                WHERE ("SystemPermissions" & 5) <> 0
                   OR EXISTS (
                       SELECT 1 FROM "ContentTypeRolePermission" p
                       WHERE p."RoleId" = "Roles"."Id" AND (p."ContentTypePermissions" & 2) <> 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditLogRetentionDays",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "BackgroundTaskRetentionDays",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "EmailLogRetentionDays",
                table: "OrganizationSettings");

            migrationBuilder.DropColumn(
                name: "WebhookDeliveryRetentionDays",
                table: "OrganizationSettings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class v2_3_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "_FieldsJson",
                table: "WidgetTemplates",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "_FieldsJson",
                table: "WidgetTemplateRevisions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "RaythaFunctionId",
                table: "Routes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RouteId",
                table: "RaythaFunctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "_SubFieldsJson",
                table: "ContentTypeFields",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "UserWebTemplate",
                columns: table => new
                {
                    FavoriteWebTemplatesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserFavoritesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWebTemplate", x => new { x.FavoriteWebTemplatesId, x.UserFavoritesId });
                    table.ForeignKey(
                        name: "FK_UserWebTemplate_Users_UserFavoritesId",
                        column: x => x.UserFavoritesId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWebTemplate_WebTemplates_FavoriteWebTemplatesId",
                        column: x => x.FavoriteWebTemplatesId,
                        principalTable: "WebTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaythaFunctions_RouteId",
                table: "RaythaFunctions",
                column: "RouteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWebTemplate_UserFavoritesId",
                table: "UserWebTemplate",
                column: "UserFavoritesId");

            migrationBuilder.AddForeignKey(
                name: "FK_RaythaFunctions_Routes_RouteId",
                table: "RaythaFunctions",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Frozen copies of the built-in widget fields as of 2.3.0; the registry may change later,
            // this migration must not. .audit/qa4/W5a/migration_literals.cs checks them.
            (string DeveloperName, string FieldsJson)[] builtInWidgetFields =
            [
                ("hero", """[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundImage","label":"Background image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#1e293b","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"textColor","label":"Text color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#ffffff","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"light","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"alignment","label":"Alignment","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"center","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Center","developerName":"center","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"minHeight","label":"Minimum height","fieldType":"number","description":"In pixels.","isRequired":false,"defaultValue":400,"choices":[],"subFields":[],"contentTypeField":null}]"""),
                ("wysiwyg", """[{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"padding","label":"Padding","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"medium","choices":[{"label":"None","developerName":"none","disabled":false},{"label":"Small","developerName":"small","disabled":false},{"label":"Medium","developerName":"medium","disabled":false},{"label":"Large","developerName":"large","disabled":false}],"subFields":[],"contentTypeField":null}]"""),
                ("imagetext", """[{"developerName":"imageUrl","label":"Image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageAlt","label":"Image alt text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imagePosition","label":"Image position","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"left","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"primary","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]"""),
                ("card", """[{"developerName":"title","label":"Title","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"description","label":"Description","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageUrl","label":"Image","fieldType":"image","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"imageAlt","label":"Image alt text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"primary","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]"""),
                ("faq", """[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"expandFirst","label":"Expand first item","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"items","label":"Questions","fieldType":"repeater","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[{"developerName":"question","label":"Question","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"answer","label":"Answer","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}],"contentTypeField":null}]"""),
                ("cta", """[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"content","label":"Content","fieldType":"wysiwyg","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonText","label":"Button text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonUrl","label":"Button URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"buttonStyle","label":"Button style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"light","choices":[{"label":"Primary","developerName":"primary","disabled":false},{"label":"Secondary","developerName":"secondary","disabled":false},{"label":"Outline primary","developerName":"outline-primary","disabled":false},{"label":"Outline light","developerName":"outline-light","disabled":false},{"label":"Light","developerName":"light","disabled":false},{"label":"Link","developerName":"link","disabled":false},{"label":"Success","developerName":"success","disabled":false},{"label":"Danger","developerName":"danger","disabled":false},{"label":"Warning","developerName":"warning","disabled":false},{"label":"Info","developerName":"info","disabled":false},{"label":"Dark","developerName":"dark","disabled":false},{"label":"Outline dark","developerName":"outline-dark","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#0d6efd","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"textColor","label":"Text color","fieldType":"color","description":null,"isRequired":false,"defaultValue":"#ffffff","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"alignment","label":"Alignment","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"center","choices":[{"label":"Left","developerName":"left","disabled":false},{"label":"Center","developerName":"center","disabled":false},{"label":"Right","developerName":"right","disabled":false}],"subFields":[],"contentTypeField":null}]"""),
                ("embed", """[{"developerName":"embedType","label":"Embed type","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"iframe","choices":[{"label":"Iframe","developerName":"iframe","disabled":false},{"label":"HTML","developerName":"html","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"iframeUrl","label":"Iframe URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"htmlContent","label":"Embed HTML","fieldType":"long_text","description":"Rendered as raw HTML, scripts included.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"aspectRatio","label":"Aspect ratio","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"16x9","choices":[{"label":"16:9","developerName":"16x9","disabled":false},{"label":"4:3","developerName":"4x3","disabled":false},{"label":"1:1","developerName":"1x1","disabled":false},{"label":"21:9","developerName":"21x9","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"maxWidth","label":"Max width","fieldType":"number","description":"In pixels. Leave empty for full width.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"caption","label":"Caption","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]"""),
                ("contentlist", """[{"developerName":"headline","label":"Headline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"subheadline","label":"Subheadline","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"contentType","label":"Content type","fieldType":"content_type","description":null,"isRequired":true,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"viewId","label":"View","fieldType":"view","description":"Leave empty for the default view.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":"contentType"},{"developerName":"filter","label":"Filter","fieldType":"single_line_text","description":"OData filter expression.","isRequired":false,"defaultValue":"IsPublished eq \u0027true\u0027","choices":[],"subFields":[],"contentTypeField":null},{"developerName":"orderBy","label":"Order by","fieldType":"single_line_text","description":"OData order by expression. Overrides the view\u0027s sort.","isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"pageSize","label":"Page size","fieldType":"number","description":null,"isRequired":false,"defaultValue":3,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"displayStyle","label":"Display style","fieldType":"dropdown","description":null,"isRequired":false,"defaultValue":"cards","choices":[{"label":"Cards","developerName":"cards","disabled":false},{"label":"List","developerName":"list","disabled":false},{"label":"Compact","developerName":"compact","disabled":false}],"subFields":[],"contentTypeField":null},{"developerName":"showImage","label":"Show image","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"showDate","label":"Show date","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"showExcerpt","label":"Show excerpt","fieldType":"checkbox","description":null,"isRequired":false,"defaultValue":true,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"linkText","label":"View all text","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"linkUrl","label":"View all URL","fieldType":"single_line_text","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null},{"developerName":"backgroundColor","label":"Background color","fieldType":"color","description":null,"isRequired":false,"defaultValue":null,"choices":[],"subFields":[],"contentTypeField":null}]"""),
            ];
            foreach (var (developerName, fieldsJson) in builtInWidgetFields)
            {
                migrationBuilder.Sql(
                    $$"""
                    UPDATE "WidgetTemplates" SET "_FieldsJson" = '{{fieldsJson}}'::jsonb
                    WHERE "DeveloperName" = '{{developerName}}' AND "_FieldsJson" = '[]'::jsonb;
                    UPDATE "WidgetTemplateRevisions" AS r SET "_FieldsJson" = '{{fieldsJson}}'::jsonb
                    FROM "WidgetTemplates" AS t
                    WHERE r."WidgetTemplateId" = t."Id" AND t."DeveloperName" = '{{developerName}}' AND r."_FieldsJson" = '[]'::jsonb;
                    """
                );
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RaythaFunctions_Routes_RouteId",
                table: "RaythaFunctions");

            migrationBuilder.DropTable(
                name: "UserWebTemplate");

            migrationBuilder.DropIndex(
                name: "IX_RaythaFunctions_RouteId",
                table: "RaythaFunctions");

            migrationBuilder.DropColumn(
                name: "_FieldsJson",
                table: "WidgetTemplates");

            migrationBuilder.DropColumn(
                name: "_FieldsJson",
                table: "WidgetTemplateRevisions");

            migrationBuilder.DropColumn(
                name: "RaythaFunctionId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "RaythaFunctions");

            migrationBuilder.DropColumn(
                name: "_SubFieldsJson",
                table: "ContentTypeFields");
        }
    }
}

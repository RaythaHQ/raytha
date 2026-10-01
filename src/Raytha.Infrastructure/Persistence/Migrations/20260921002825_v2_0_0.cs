using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class v2_0_0 : Migration
    {
        // Frozen copies of the 2.0 magic-link templates; the default template files may change later,
        // this migration must not.
        private const string MagicLinkEmailContent = """
            <p>Hello {{ Target.FirstName }},</p>

            <p>Use this one-time code to login to the {{ CurrentOrganization.OrganizationName }} website.</p>

            <p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">{{ Target.Code }}</p>

            <p>The code expires after {{ Target.MagicLinkExpiresInSeconds }} seconds. If you did not request it, you can ignore this email.</p>

            <p>Thank you,<br/>
              {{ CurrentOrganization.OrganizationName }}</p>
            """;

        private const string MagicLinkSentPageContent = """
            <h3>Enter your code</h3>
            <p>We emailed you a one-time code. Enter it below to login.</p>
            {% if Target.ValidationFailures["__ValidationSummary"] %}
              <div class="alert alert-danger">
                {{ Target.ValidationFailures["__ValidationSummary"] }}
              </div>
            {% endif %}
            <form
              action="{{ PathBase }}/account/login/magic-link/complete?returnUrl={{ Target.ReturnUrl }}"
              method="post">
              <div class="mb-3">
                <label for="email" class="form-label">Email address</label>
                <input
                  id="email"
                  type="email"
                  name="EmailAddress"
                  value="{{ Target.EmailAddress }}"
                  class="form-control">
              </div>
              <div class="mb-3">
                <label for="code" class="form-label">One-time code</label>
                <input
                  id="code"
                  type="text"
                  name="Code"
                  inputmode="numeric"
                  autocomplete="one-time-code"
                  class="form-control">
              </div>
              <div>
                <button type="submit" class="btn btn-primary w-100">Login</button>
              </div>
              <input
                name="__RequestVerificationToken"
                type="hidden"
                value="{{ Target.RequestVerificationToken }}" />
            </form>
            """;

        // 1.x stored date fields as the picker's m/d/yyyy or as DateTime.ToString() in the server's
        // culture, so the day/month order is only knowable per field: a first number above 12 means
        // day-first, a second number above 12 means month-first, neither means month-first, and both
        // means the field is left alone and reported.
        private const string ConvertLegacyDates = """
            CREATE TEMP TABLE raytha_legacy_dates AS
            WITH stored AS (
                SELECT 'published' AS source, ci."Id" AS row_id, f."Id" AS field_id, f."DeveloperName" AS field,
                       ci."_PublishedContent" ->> f."DeveloperName" AS raw
                FROM "ContentItems" ci
                JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
                UNION ALL
                SELECT 'draft', ci."Id", f."Id", f."DeveloperName",
                       NULLIF(ci."_DraftContent", '')::jsonb ->> f."DeveloperName"
                FROM "ContentItems" ci
                JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
                UNION ALL
                SELECT 'revision', r."Id", f."Id", f."DeveloperName",
                       NULLIF(r."_PublishedContent", '')::jsonb ->> f."DeveloperName"
                FROM "ContentItemRevisions" r
                JOIN "ContentItems" ci ON ci."Id" = r."ContentItemId"
                JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
            )
            SELECT source, row_id, field_id, field, raw,
                   regexp_match(raw, '^[[:space:]\u00A0\u202F]*([0-9]{1,2})[/.-]([0-9]{1,2})[/.-]([0-9]{4})(?:[[:space:]\u00A0\u202F]+([0-9]{1,2}):([0-9]{2})(?::([0-9]{2}))?(?:[[:space:]\u00A0\u202F]*([AaPp])[Mm])?)?[[:space:]\u00A0\u202F]*$') AS parts
            FROM stored
            WHERE raw IS NOT NULL
              AND btrim(raw) <> ''
              AND raw !~ '^[0-9]{4}-[0-9]{2}-[0-9]{2}';

            CREATE TEMP TABLE raytha_legacy_date_order AS
            SELECT field_id,
                   bool_or(parts[1]::int > 12) AS day_first,
                   bool_or(parts[2]::int > 12) AS month_first
            FROM raytha_legacy_dates
            WHERE parts IS NOT NULL
            GROUP BY field_id;

            CREATE TEMP TABLE raytha_legacy_date_iso AS
            WITH split AS (
                SELECT d.source, d.row_id, d.field_id, d.field,
                       parts[3]::int AS y,
                       CASE WHEN o.day_first THEN parts[2]::int ELSE parts[1]::int END AS m,
                       CASE WHEN o.day_first THEN parts[1]::int ELSE parts[2]::int END AS d,
                       CASE
                           WHEN parts[4] IS NULL THEN 0
                           WHEN parts[7] IS NULL THEN parts[4]::int
                           WHEN upper(parts[7]) = 'A' THEN parts[4]::int % 12
                           ELSE parts[4]::int % 12 + 12
                       END AS h,
                       COALESCE(parts[5]::int, 0) AS mi,
                       COALESCE(parts[6]::int, 0) AS s,
                       parts[7] IS NOT NULL AND parts[4]::int NOT BETWEEN 1 AND 12 AS bad_clock
                FROM raytha_legacy_dates d
                JOIN raytha_legacy_date_order o ON o.field_id = d.field_id
                WHERE d.parts IS NOT NULL
                  AND NOT (o.day_first AND o.month_first)
            ),
            valid AS (
                SELECT * FROM split
                WHERE y > 0 AND m BETWEEN 1 AND 12 AND h BETWEEN 0 AND 23 AND mi BETWEEN 0 AND 59
                  AND s BETWEEN 0 AND 59 AND NOT bad_clock
                  AND d BETWEEN 1 AND CASE WHEN y > 0 AND m BETWEEN 1 AND 12
                      THEN extract(day FROM make_date(y, m, 1) + interval '1 month - 1 day')::int END
            )
            SELECT source, row_id, field_id, field,
                   to_char(make_date(y, m, d), 'YYYY-MM-DD')
                   || CASE WHEN h = 0 AND mi = 0 AND s = 0 THEN ''
                           ELSE 'T' || to_char(make_time(h, mi, s), 'HH24:MI:SS') END AS iso
            FROM valid;

            CREATE TEMP TABLE raytha_legacy_date_patch AS
            SELECT source, row_id, jsonb_object_agg(field, iso) AS patch
            FROM raytha_legacy_date_iso
            GROUP BY source, row_id;

            UPDATE "ContentItems" ci
            SET "_PublishedContent" = ci."_PublishedContent" || p.patch
            FROM raytha_legacy_date_patch p
            WHERE p.source = 'published' AND p.row_id = ci."Id";

            UPDATE "ContentItems" ci
            SET "_DraftContent" = (ci."_DraftContent"::jsonb || p.patch)::text
            FROM raytha_legacy_date_patch p
            WHERE p.source = 'draft' AND p.row_id = ci."Id";

            UPDATE "ContentItemRevisions" r
            SET "_PublishedContent" = (r."_PublishedContent"::jsonb || p.patch)::text
            FROM raytha_legacy_date_patch p
            WHERE p.source = 'revision' AND p.row_id = r."Id";

            DO $$
            DECLARE
                skipped record;
            BEGIN
                FOR skipped IN
                    SELECT t."DeveloperName" AS content_type, f."DeveloperName" AS field,
                           CASE WHEN d.parts IS NULL THEN 'unrecognized format'
                                WHEN o.day_first AND o.month_first THEN 'mixed day/month order'
                                ELSE 'not a valid date' END AS reason,
                           count(*) AS value_count
                    FROM raytha_legacy_dates d
                    JOIN "ContentTypeFields" f ON f."Id" = d.field_id
                    JOIN "ContentTypes" t ON t."Id" = f."ContentTypeId"
                    LEFT JOIN raytha_legacy_date_order o ON o.field_id = d.field_id
                    WHERE NOT EXISTS (
                        SELECT 1 FROM raytha_legacy_date_iso i
                        WHERE i.source = d.source AND i.row_id = d.row_id AND i.field_id = d.field_id
                    )
                    GROUP BY 1, 2, 3
                LOOP
                    RAISE NOTICE 'Date values left unchanged in %.%: % (% values)',
                        skipped.content_type, skipped.field, skipped.reason, skipped.value_count;
                END LOOP;
            END
            $$;

            DROP TABLE raytha_legacy_date_patch;
            DROP TABLE raytha_legacy_date_iso;
            DROP TABLE raytha_legacy_date_order;
            DROP TABLE raytha_legacy_dates;
            """;

        // Editor uploads used to return absolute URLs built from the request host, so rich text and
        // widget settings saved http://localhost:5200/raytha/media-items/objectkey/... and broke when
        // the site moved. Only URLs whose object key belongs to a media item in this database are
        // rewritten, which leaves links to another Raytha site alone; any path base is kept. The
        // result is the same root-relative path the upload endpoint returns now, so running this
        // again changes nothing. Templates are not touched: email templates need absolute URLs.
        private const string RelativizeMediaUrls = """
            CREATE FUNCTION pg_temp.raytha_relative_media_urls(input text) RETURNS text
            LANGUAGE plpgsql AS $fn$
            DECLARE
                found text[];
                result text := input;
            BEGIN
                FOR found IN
                    SELECT regexp_matches(
                        input,
                        '(https?://[A-Za-z0-9.-]+(?::[0-9]+)?)((?:/[A-Za-z0-9._~-]+)*/(?:raytha/media-items/objectkey|_static-files)/([A-Za-z0-9._~-]+))',
                        'g')
                LOOP
                    IF EXISTS (SELECT 1 FROM "MediaItems" WHERE "ObjectKey" = found[3]) THEN
                        result := replace(result, found[1] || found[2], found[2]);
                    END IF;
                END LOOP;
                RETURN result;
            END
            $fn$;

            UPDATE "ContentItems"
            SET "_PublishedContent" = pg_temp.raytha_relative_media_urls("_PublishedContent"::text)::jsonb
            WHERE "_PublishedContent"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            UPDATE "ContentItems"
            SET "_DraftContent" = pg_temp.raytha_relative_media_urls("_DraftContent")
            WHERE "_DraftContent" ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            UPDATE "ContentItemRevisions"
            SET "_PublishedContent" = pg_temp.raytha_relative_media_urls("_PublishedContent")
            WHERE "_PublishedContent" ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            UPDATE "DeletedContentItems"
            SET "_PublishedContent" = pg_temp.raytha_relative_media_urls("_PublishedContent")
            WHERE "_PublishedContent" ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            UPDATE "SitePages"
            SET "_PublishedWidgetsJson" = pg_temp.raytha_relative_media_urls("_PublishedWidgetsJson"::text)::jsonb
            WHERE "_PublishedWidgetsJson"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            UPDATE "SitePages"
            SET "_DraftWidgetsJson" = pg_temp.raytha_relative_media_urls("_DraftWidgetsJson"::text)::jsonb
            WHERE "_DraftWidgetsJson"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            UPDATE "SitePageRevisions"
            SET "_PublishedWidgetsJson" = pg_temp.raytha_relative_media_urls("_PublishedWidgetsJson"::text)::jsonb
            WHERE "_PublishedWidgetsJson"::text ~ 'https?://[^"\\]*/(raytha/media-items/objectkey|_static-files)/';

            DROP FUNCTION pg_temp.raytha_relative_media_urls(text);
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToAddress = table.Column<string>(type: "text", nullable: false),
                    FromAddress = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsHtml = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Secret = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscribedEvents = table.Column<string>(type: "jsonb", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifierUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Webhooks_Users_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Webhooks_Users_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookDeliveries_Webhooks_WebhookId",
                        column: x => x.WebhookId,
                        principalTable: "Webhooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CreationTime",
                table: "EmailLogs",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_IsSuccess",
                table: "EmailLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_ToAddress",
                table: "EmailLogs",
                column: "ToAddress");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_CreationTime",
                table: "WebhookDeliveries",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_EventName",
                table: "WebhookDeliveries",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_WebhookId",
                table: "WebhookDeliveries",
                column: "WebhookId");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_CreatorUserId",
                table: "Webhooks",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_IsActive",
                table: "Webhooks",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_LastModifierUserId",
                table: "Webhooks",
                column: "LastModifierUserId");

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

            // Frozen copies of the built-in widget fields as of 2.0.0; the registry may change later,
            // this migration must not.
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

            // 2.0 replaced the magic-link URL with a one-time code, but 1.5 databases still hold the
            // old email (which renders an empty link) and a sent page with nowhere to enter the code.
            // Any copy without the code is broken, customized or not, so replace it and keep the old
            // content as a revision the admin can restore from.
            migrationBuilder.Sql(
                """
                INSERT INTO "EmailTemplateRevisions" ("Id", "Subject", "Content", "Cc", "Bcc", "EmailTemplateId", "CreationTime")
                SELECT gen_random_uuid(), "Subject", "Content", "Cc", "Bcc", "Id", now()
                FROM "EmailTemplates"
                WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
                  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;

                UPDATE "EmailTemplates" SET "Content" = @content, "LastModificationTime" = now()
                WHERE "DeveloperName" = 'raytha_email_login_beginloginwithmagiclink'
                  AND strpos(coalesce("Content", ''), 'Target.Code') = 0;
                """.Replace("@content", SqlLiteral(MagicLinkEmailContent))
            );

            migrationBuilder.Sql(
                """
                INSERT INTO "WebTemplateRevisions" ("Id", "Label", "Content", "WebTemplateId", "AllowAccessForNewContentTypes", "CreationTime")
                SELECT gen_random_uuid(), "Label", "Content", "Id", "AllowAccessForNewContentTypes", now()
                FROM "WebTemplates"
                WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
                  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;

                UPDATE "WebTemplates" SET "Content" = @content, "LastModificationTime" = now()
                WHERE "DeveloperName" = 'raytha_html_login_magiclinksent'
                  AND strpos(coalesce("Content", ''), 'magic-link/complete') = 0;
                """.Replace("@content", SqlLiteral(MagicLinkSentPageContent))
            );

            migrationBuilder.Sql(ConvertLegacyDates);

            migrationBuilder.AddColumn<string>(
                name: "ImpersonatorEmail",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(RelativizeMediaUrls);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImpersonatorEmail",
                table: "AuditLogs");

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

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "Webhooks");
        }

        private static string SqlLiteral(string value) => "'" + value.Replace("'", "''") + "'";
    }
}

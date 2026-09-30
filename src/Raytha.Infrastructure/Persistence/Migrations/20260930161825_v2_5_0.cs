using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Raytha.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class v2_5_0 : Migration
    {
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ConvertLegacyDates);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}

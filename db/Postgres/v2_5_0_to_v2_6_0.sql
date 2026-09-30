START TRANSACTION;
ALTER TABLE "AuditLogs" ADD "ImpersonatorEmail" text;

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

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260930172707_v2_6_0', '10.0.11');

COMMIT;


BEGIN;

UPDATE "ContentTypeFields"
SET "FieldType" = 'wysiwyg'
WHERE "FieldType" = 'long_text';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20241212094439_v1_4_1', '8.0.10');

COMMIT;
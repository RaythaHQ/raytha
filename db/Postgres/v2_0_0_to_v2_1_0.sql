START TRANSACTION;
DROP TABLE "FeatureFlags";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260921025511_v2_1_0', '10.0.0');

COMMIT;


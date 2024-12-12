BEGIN TRANSACTION;
GO


                UPDATE ContentTypeFields
                SET FieldType = 'wysiwyg'
                WHERE FieldType = 'long_text';
            
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20241212094439_v1_4_1', N'8.0.10');
GO

COMMIT;
GO


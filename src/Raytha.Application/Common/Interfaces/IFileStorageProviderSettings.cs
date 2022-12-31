namespace Raytha.Application.Common.Interfaces;

public interface IFileStorageProviderSettings
{
    string FileStorageProvider { get; }

    long MaxTotalDiskSpace { get; }
    long MaxFileSize { get; }
    string AllowedMimeTypes { get; }
    string LocalDirectory { get; }
    string AzureBlobConnectionString { get; }
    string AzureBlobContainer { get; }
    string S3AccessKey { get; }
    string S3SecretKey { get; }
    string S3ServiceUrl { get; }
    string S3Bucket { get; }

    bool UseDirectUploadToCloud { get; }
    bool UseCloudStorage { get; }
    bool UseLocal { get; }
    bool UseS3 { get; }
    bool UseAzureBlob { get; }
}
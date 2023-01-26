using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using System;

namespace Raytha.Web.Services;

public class FileStorageProviderSettings : IFileStorageProviderSettings
{
    private readonly IConfiguration _configuration;
    public FileStorageProviderSettings(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string FileStorageProvider => _configuration[FileStorageUtility.CONFIG_NAME].ToLower().IfNullOrEmpty(FileStorageUtility.LOCAL);

    public long MaxTotalDiskSpace
    {
        get
        {
            if (string.IsNullOrEmpty(_configuration[FileStorageUtility.MAX_TOTAL_DISK_SPACE_CONFIG_NAME]))
            {
                return FileStorageUtility.DEFAULT_MAX_TOTAL_DISK_SPACE;
            }
            return Convert.ToInt64(_configuration[FileStorageUtility.MAX_TOTAL_DISK_SPACE_CONFIG_NAME]);
        }
    }

    public long MaxFileSize
    {
        get
        {
            if (string.IsNullOrEmpty(_configuration[FileStorageUtility.MAX_FILE_SIZE_CONFIG_NAME]))
            {
                return FileStorageUtility.DEFAULT_MAX_FILE_SIZE;
            }
            return Convert.ToInt64(_configuration[FileStorageUtility.MAX_FILE_SIZE_CONFIG_NAME]);
        }
    }

    public string AllowedMimeTypes => _configuration[FileStorageUtility.ALLOWED_MIMETYPES_CONFIG_NAME].ToLower().IfNullOrEmpty(FileStorageUtility.DEFAULT_ALLOWED_MIMETYPES);

    public string LocalDirectory => _configuration[FileStorageUtility.LOCAL_DIRECTORY_CONFIG_NAME].ToLower().IfNullOrEmpty(FileStorageUtility.DEFAULT_LOCAL_DIRECTORY);

    public string AzureBlobConnectionString => _configuration[FileStorageUtility.AZUREBLOB_CONNECTION_STRING_CONFIG_NAME];

    public string AzureBlobContainer => _configuration[FileStorageUtility.AZUREBLOB_CONTAINER_CONFIG_NAME];

    public string AzureBlobCustomDomain => _configuration[FileStorageUtility.AZUREBLOB_CUSTOM_DOMAIN];

    public string S3AccessKey => _configuration[FileStorageUtility.S3_ACCESS_KEY_CONFIG_NAME];

    public string S3SecretKey => _configuration[FileStorageUtility.S3_SECRET_KEY_CONFIG_NAME];

    public string S3ServiceUrl => _configuration[FileStorageUtility.S3_SERVICE_URL_CONFIG_NAME];

    public string S3Bucket => _configuration[FileStorageUtility.S3_BUCKET_CONFIG_NAME];

    public bool UseCloudStorage => !UseLocal;

    public bool UseDirectUploadToCloud
    {
        get
        {
            if (UseLocal)
                return false;

            if (string.IsNullOrEmpty(_configuration[FileStorageUtility.DIRECT_UPLOAD_TO_CLOUD_CONFIG_NAME]))
            {
                return FileStorageUtility.DEFAULT_DIRECT_UPLOAD_TO_CLOUD;
            }
            return Convert.ToBoolean(_configuration[FileStorageUtility.DIRECT_UPLOAD_TO_CLOUD_CONFIG_NAME]);
        }
    }

    public bool UseLocal => FileStorageProvider == FileStorageUtility.LOCAL;

    public bool UseS3 => FileStorageProvider == FileStorageUtility.S3;

    public bool UseAzureBlob => FileStorageProvider == FileStorageUtility.AZUREBLOB;
}

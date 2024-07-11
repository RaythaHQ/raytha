using System.Net.Mime;

namespace Raytha.Application.Common.Utils;

public static class FileStorageUtility
{
    private const string FILE_STORAGE_PREFIX = "FILE_STORAGE";
    public const string CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_PROVIDER";
    public const string MAX_FILE_SIZE_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_MAX_FILE_SIZE";
    public const string MAX_TOTAL_DISK_SPACE_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_MAX_TOTAL_DISK_SPACE";
    public const string ALLOWED_MIMETYPES_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_ALLOWED_MIMETYPES";
    public const string LOCAL_DIRECTORY_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_LOCAL_DIRECTORY";
    public const string AZUREBLOB_CONNECTION_STRING_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_AZUREBLOB_CONNECTION_STRING";
    public const string AZUREBLOB_CONTAINER_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_AZUREBLOB_CONTAINER";
    public const string AZUREBLOB_CUSTOM_DOMAIN = $"{FILE_STORAGE_PREFIX}_AZUREBLOB_CUSTOM_DOMAIN";
    public const string S3_ACCESS_KEY_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_S3_ACCESS_KEY";
    public const string S3_SECRET_KEY_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_S3_SECRET_KEY";
    public const string S3_SERVICE_URL_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_S3_SERVICE_URL";
    public const string S3_BUCKET_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_S3_BUCKET";
    public const string DIRECT_UPLOAD_TO_CLOUD_CONFIG_NAME = $"{FILE_STORAGE_PREFIX}_DIRECT_UPLOAD_TO_CLOUD";
    public const string DATABASE_MAX_SIZE_CONFIG_NAME = $"DATABASE_MAX_SIZE";

    public const string LOCAL = "local";
    public const string AZUREBLOB = "azureblob";
    public const string S3 = "s3";

    public const string DEFAULT_LOCAL_DIRECTORY = "user-uploads";
    public const long DEFAULT_MAX_FILE_SIZE = 20000000; //20 mb
    public const long DEFAULT_MAX_TOTAL_DISK_SPACE = 1000000000; //1 gb 
    public const long DEFAULT_MAX_TOTAL_DB_SIZE = 1000000000; //1 gb 
    public const string DEFAULT_ALLOWED_MIMETYPES = "text/*,image/*,video/*,audio/*,application/pdf";
    public const bool DEFAULT_DIRECT_UPLOAD_TO_CLOUD = true;

    public static string[] GetAllowedFileExtensionsFromConfig(string csvFileExt)
    {
        return csvFileExt.ToLower().Split(',', StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries);
    }

    public static string CreateObjectKeyFromIdAndFileName(string id, string fileName)
    {
        var cleanFileName = Path.GetFileNameWithoutExtension(fileName).ToDeveloperName();
        cleanFileName = $"{cleanFileName}{Path.GetExtension(fileName)}";
        return $"{id}_{cleanFileName}";
    }

    public static DateTime GetDefaultExpiry()
    {
        return DateTime.UtcNow.AddDays(1);
    }

    public static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".txt" => MediaTypeNames.Text.Plain,
            ".html" => MediaTypeNames.Text.Html,
            ".xml" => MediaTypeNames.Text.Xml,
            ".rtf" => MediaTypeNames.Text.RichText,
            ".js" => MediaTypeNames.Text.JavaScript,
            ".css" => MediaTypeNames.Text.Css,
            ".png" => MediaTypeNames.Image.Png,
            ".jpg" => MediaTypeNames.Image.Jpeg,
            ".jpeg" => MediaTypeNames.Image.Jpeg,
            ".gif" => MediaTypeNames.Image.Gif,
            ".webp" => MediaTypeNames.Image.Webp,
            ".ico" => MediaTypeNames.Image.Icon,
            ".svg" => MediaTypeNames.Image.Svg,
            ".pdf" => MediaTypeNames.Application.Pdf,
            ".zip" => MediaTypeNames.Application.Zip,
            ".json" => MediaTypeNames.Application.Json,
            _ => MediaTypeNames.Application.Octet,
        };
    }
}

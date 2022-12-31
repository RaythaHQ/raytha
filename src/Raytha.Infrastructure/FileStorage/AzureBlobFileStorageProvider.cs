using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.FileStorage;

public class AzureBlobFileStorageProvider : IFileStorageProvider
{
    private static BlobContainerClient _client;

    public AzureBlobFileStorageProvider(IFileStorageProviderSettings configuration)
    {
        string connectionString = configuration.AzureBlobConnectionString;
        string container = configuration.AzureBlobContainer;

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(container))
            throw new InvalidOperationException("Azure Environment Variables were not found");

        if (_client == null)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            _client = blobServiceClient.GetBlobContainerClient(container);
        }
    }

    public async Task DeleteAsync(string key)
    {
        await _client.DeleteBlobAsync(key);
    }

    public async Task<string> GetDownloadUrlAsync(string key, DateTime expiresAt, bool inline = true)
    {
        BlobClient blobClient = _client.GetBlobClient(key);

        string downloadUrl;

        BlobSasBuilder sasPermissions = new BlobSasBuilder(BlobSasPermissions.Read, expiresAt)
        {
            ContentDisposition = inline ? $"inline" : $"attachment; filename={key}",
            Protocol = SasProtocol.Https
        };

        downloadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;
        return downloadUrl;
    }

    public async Task<string> GetDownloadUrlAsync(string key, string fileName, string contentType, DateTime expiresAt, bool inline = true)
    {
        BlobClient blobClient = _client.GetBlobClient(key);
        
        string downloadUrl;

        BlobSasBuilder sasPermissions = new BlobSasBuilder(BlobSasPermissions.Read, expiresAt)
        {
            ContentDisposition = inline ? $"inline" : $"attachment; filename={key}",
            ContentType = contentType,
            Protocol = SasProtocol.Https
        };
        
        downloadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;
        return downloadUrl;
    }

    public async Task<string> GetUploadUrlAsync(string key, string fileName, string contentType, DateTime expiresAt, bool inline = true)
    {
        BlobClient blobClient = _client.GetBlobClient(key);
        BlobSasBuilder sasPermissions = new BlobSasBuilder(BlobSasPermissions.Write, expiresAt)
        {
            ContentDisposition = inline ? $"inline" : $"attachment; filename={fileName}",
            ContentType = contentType,
            Protocol = SasProtocol.Https
        };
        string uploadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;
        return uploadUrl;
    }

    public async Task<string> SaveAndGetDownloadUrlAsync(byte[] data, string key, string fileName, string contentType, DateTime expiresAt, bool inline = true)
    {
        BlobClient blobClient = _client.GetBlobClient(key);

        string downloadUrl;
 
        BlobSasBuilder sasPermissions = new BlobSasBuilder(BlobSasPermissions.Read, expiresAt)
        {
            ContentDisposition = inline ? $"inline" : $"attachment; filename={fileName}",
            ContentType = contentType,
            Protocol = SasProtocol.Https
        };
        downloadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;

        using (var ms = new MemoryStream())
        {
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            await blobClient.UploadAsync(ms);
        }

        return downloadUrl;
    }

    public string GetName()
    {
        return FileStorageUtility.AZUREBLOB;
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.FileStorage;

public class AzureBlobFileStorageProvider : IFileStorageProvider
{
    private static BlobContainerClient _client;
    private string _customDomain = string.Empty;

    public AzureBlobFileStorageProvider(IFileStorageProviderSettings configuration)
    {
        string connectionString = configuration.AzureBlobConnectionString;
        string container = configuration.AzureBlobContainer;
        _customDomain = configuration.AzureBlobCustomDomain;

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
            Protocol = SasProtocol.HttpsAndHttp
        };


        downloadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;

        downloadUrl = UseCustomDomain(downloadUrl);
        return downloadUrl;
    }

    public async Task<string> GetDownloadUrlAsync(string key)
    {
        BlobClient blobClient = _client.GetBlobClient(key);
        
        string downloadUrl = blobClient.Uri.AbsoluteUri;
        downloadUrl = UseCustomDomain(downloadUrl);

        return downloadUrl;
    }

    public async Task<string> GetUploadUrlAsync(string key, string fileName, string contentType, DateTime expiresAt, bool inline = true)
    {
        BlobClient blobClient = _client.GetBlobClient(key);
        BlobSasBuilder sasPermissions = new BlobSasBuilder(BlobSasPermissions.Write, expiresAt)
        {
            ContentDisposition = inline ? $"inline" : $"attachment; filename={fileName}",
            ContentType = contentType,
            Protocol = SasProtocol.HttpsAndHttp
        };
        string uploadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;
        uploadUrl = UseCustomDomain(uploadUrl);
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
            Protocol = SasProtocol.HttpsAndHttp
        };

        downloadUrl = blobClient.GenerateSasUri(sasPermissions).AbsoluteUri;

        using (var ms = new MemoryStream())
        {
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            await blobClient.UploadAsync(ms, new BlobHttpHeaders { ContentType = contentType, });
        }

        downloadUrl = UseCustomDomain(downloadUrl);
        return downloadUrl;
    }

    public string GetName()
    {
        return FileStorageUtility.AZUREBLOB;
    }

    private string UseCustomDomain(string downloadUrl)
    {
        if (string.IsNullOrEmpty(_customDomain))
            return downloadUrl;

        var builder = new UriBuilder(downloadUrl);
        builder.Host = _customDomain;
        return builder.Uri.ToString();
    }
}

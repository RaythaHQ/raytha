using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.FileStorage;

public class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private string _localStorageDirectory;

    private IFileStorageProviderSettings _configuration;

    public LocalFileStorageProvider(
        IFileStorageProviderSettings configuration,
        IRelativeUrlBuilder relativeUrlBuilder
    )
    {
        _relativeUrlBuilder = relativeUrlBuilder;
        _localStorageDirectory = configuration.LocalDirectory;
        _configuration = configuration;
    }

    public async Task DeleteAsync(string key)
    {
        var filePath = Path.Combine(_localStorageDirectory, key);
        File.Delete(filePath);
    }

    public async Task<string> GetDownloadUrlAsync(
        string key,
        DateTime expiresAt,
        bool inline = true
    )
    {
        return _relativeUrlBuilder.MediaFileLocalStorageUrl(key);
    }

    public async Task<string> GetDownloadUrlAsync(string key)
    {
        return _relativeUrlBuilder.MediaFileLocalStorageUrl(key);
    }

    public string GetName()
    {
        return FileStorageUtility.LOCAL;
    }

    public async Task<string> GetUploadUrlAsync(
        string key,
        string fileName,
        string contentType,
        DateTime expiresAt,
        bool inline = true
    )
    {
        throw new NotImplementedException();
    }

    public async Task<string> SaveAndGetDownloadUrlAsync(
        byte[] data,
        string key,
        string fileName,
        string contentType,
        DateTime expiresAt,
        bool inline = true
    )
    {
        if (data.Length > _configuration.MaxFileSize)
        {
            throw new InvalidDataException(
                $"File size {data.Length} exceeds max file size of {_configuration.MaxFileSize}"
            );
        }
        var filePath = Path.Combine(_localStorageDirectory, key);
        await File.WriteAllBytesAsync(filePath, data);
        return _relativeUrlBuilder.MediaFileLocalStorageUrl(key);
    }
}

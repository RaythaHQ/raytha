namespace Raytha.Application.Common.Interfaces;

public interface IFileStorageProvider
{
    string GetName();

    Task<string> GetUploadUrlAsync(string key, string fileName, string contentType, DateTime expiresAt, bool inline = true);

    Task<string> GetDownloadUrlAsync(string key, DateTime expiresAt, bool inline = true);

    Task<string> GetDownloadUrlAsync(string key, string fileName, string contentType, DateTime expiresAt, bool inline = true);

    Task DeleteAsync(string key);

    Task<string> SaveAndGetDownloadUrlAsync(byte[] data, string key, string fileName, string contentType, DateTime expiresAt, bool inline = true);
}

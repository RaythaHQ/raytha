using Amazon.S3;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Amazon.S3.Model;
using System.Net;
using Amazon.Runtime;

namespace Raytha.Infrastructure.FileStorage;

public class S3FileStorageProvider : IFileStorageProvider
{
    private static AmazonS3Client _client;
    private string _bucket;

    public S3FileStorageProvider(IFileStorageProviderSettings configuration)
    {
        string accessKey = configuration.S3AccessKey;
        string secretKey = configuration.S3SecretKey;
        string serviceUrl = configuration.S3ServiceUrl;
        string bucket = configuration.S3Bucket;

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(serviceUrl) || string.IsNullOrEmpty(bucket))
            throw new InvalidOperationException("S3 Environment Variables were not found");

        _bucket = bucket;
        if (_client == null)
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
                SignatureMethod = SigningAlgorithm.HmacSHA256
            };

            _client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }
    }

    public async Task DeleteAsync(string key)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = key,
        };

        var response = await _client.DeleteObjectAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.NoContent)
        {
            throw new Exception($"Failed to delete object with key '{key}'. Status code: {response.HttpStatusCode}");
        }
    }

    public async Task<string> GetDownloadUrlAsync(string key, DateTime expiresAt, bool inline = true)
    {
        GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = expiresAt,
            Protocol = Protocol.HTTPS,
            Verb = HttpVerb.GET
        };
        request1.ResponseHeaderOverrides.ContentDisposition = inline ? $"inline; filename={key}" : $"attachment; filename={key}";
        return _client.GetPreSignedURL(request1);
    }

    public async Task<string> GetDownloadUrlAsync(string key)
    {
        string url = $"https://{_bucket}.s3.amazonaws.com/{key}";
        return url;
    }

    public async Task<string> GetUploadUrlAsync(string key, string fileName, string contentType, DateTime expiresAt, bool inline = true)
    {
        GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = expiresAt,
            Protocol = Protocol.HTTPS,
            Verb = HttpVerb.PUT
        };
        request.ResponseHeaderOverrides.ContentType = contentType;
        request.ResponseHeaderOverrides.ContentDisposition = inline ? $"inline; filename={key}" : $"attachment; filename={key}";
        return _client.GetPreSignedURL(request);
    }

    public async Task<string> SaveAndGetDownloadUrlAsync(byte[] data, string key, string fileName, string contentType, DateTime expiresAt, bool inline = true)
    {
        GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = expiresAt,
            Protocol = Protocol.HTTPS,
            Verb = HttpVerb.GET
        };
        using (var ms = new MemoryStream())
        {
            ms.Write(data, 0, data.Length);
            ms.Position = 0;
            var putObjectRequest = new PutObjectRequest
            {
                Key = key,
                ContentType = contentType,
                BucketName = _bucket,
                InputStream = ms,
                DisablePayloadSigning = true
            };
            var response = await _client.PutObjectAsync(putObjectRequest);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response.HttpStatusCode.ToString());
            }
        }
        request.ResponseHeaderOverrides.ContentType = contentType;
        request.ResponseHeaderOverrides.ContentDisposition = inline ? $"inline; filename={key}" : $"attachment; filename={key}";
        return _client.GetPreSignedURL(request);
    }

    public string GetName()
    {
        return FileStorageUtility.S3;
    }
}

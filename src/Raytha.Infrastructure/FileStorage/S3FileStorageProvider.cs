using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;

namespace Raytha.Infrastructure.FileStorage;

public class S3FileStorageProvider : IFileStorageProvider
{
    private static AmazonS3Client _client;
    private string _bucket;
    private bool _useHttps = true;

    public S3FileStorageProvider(IFileStorageProviderSettings configuration)
    {
        string accessKey = configuration.S3AccessKey;
        string secretKey = configuration.S3SecretKey;
        string serviceUrl = configuration.S3ServiceUrl;
        string bucket = configuration.S3Bucket;
        string region = configuration.S3Region ?? "us-east-1";

        if (
            string.IsNullOrEmpty(accessKey)
            || string.IsNullOrEmpty(secretKey)
            || string.IsNullOrEmpty(serviceUrl)
            || string.IsNullOrEmpty(bucket)
        )
            throw new InvalidOperationException("S3 Environment Variables were not found");

        _bucket = bucket;

        var s3Config = new AmazonS3Config
        {
            ForcePathStyle = true,
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region),
            RequestChecksumCalculation = Amazon.Runtime.RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = Amazon.Runtime.ResponseChecksumValidation.WHEN_REQUIRED,
        };

        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            s3Config.ServiceURL = serviceUrl;
            _useHttps = serviceUrl.StartsWith("https://");
            if (!_useHttps && !configuration.UseDirectUploadToCloud)
            {
                throw new Exception(
                    "USE_DIRECT_UPLOAD_TO_CLOUD = false, require HTTPS service URL. Otherwise set env var to true"
                );
            }
        }

        _client = new AmazonS3Client(accessKey, secretKey, s3Config);
    }

    public async Task DeleteAsync(string key)
    {
        var request = new DeleteObjectRequest { BucketName = _bucket, Key = key };

        var response = await _client.DeleteObjectAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.NoContent)
        {
            throw new Exception(
                $"Failed to delete object with key '{key}'. Status code: {response.HttpStatusCode}"
            );
        }
    }

    public async Task<string> GetDownloadUrlAsync(
        string key,
        DateTime expiresAt,
        bool inline = true
    )
    {
        AWSConfigsS3.UseSignatureVersion4 = true;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = expiresAt,
            Verb = HttpVerb.GET,
            Protocol = _useHttps ? Protocol.HTTPS : Protocol.HTTP,
        };

        var url = _client.GetPreSignedURL(request);
        return url;
    }

    public async Task<string> GetDownloadUrlAsync(string key)
    {
        string url = $"https://{_bucket}.s3.amazonaws.com/{key}";
        return url;
    }

    public async Task<string> GetUploadUrlAsync(
        string key,
        string fileName,
        string contentType,
        DateTime expiresAt,
        bool inline = true
    )
    {
        AWSConfigsS3.UseSignatureVersion4 = true;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = expiresAt,
            Verb = HttpVerb.PUT,
            Protocol = _useHttps ? Protocol.HTTPS : Protocol.HTTP,
        };
        request.Headers["Content-Type"] = contentType;
        return _client.GetPreSignedURL(request);
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
        using var stream = new MemoryStream(data);

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true,
        };

        var response = await _client.PutObjectAsync(request);
        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception($"Failed to upload file to S3: {response.HttpStatusCode}");
        }

        var downloadUrl = await GetDownloadUrlAsync(key, expiresAt, inline);
        return downloadUrl;
    }

    public string GetName()
    {
        return FileStorageUtility.S3;
    }
}

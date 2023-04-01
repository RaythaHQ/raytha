# Configure advanced file storage for Raytha

By default, Raytha uses the local file system and the directory specified in `/src/Raytha.Web/appsettings.json`. This is a convenient way to get up and running quickly on your local development environment. However, in production, it is best practice to use Azure Blob or an S3-compatible storage provider.

## Common settings

Regardless of your storage provider, some environment variables are set. Here are the default settings:

```
  "FILE_STORAGE_PROVIDER": "Local",
  "FILE_STORAGE_MAX_FILE_SIZE": 20000000,
  "FILE_STORAGE_MAX_TOTAL_DISK_SPACE": 1000000000,
  "FILE_STORAGE_ALLOWED_MIMETYPES": "text/*,image/*,video/*,audio/*,application/pdf",
  "FILE_STORAGE_USE_DIRECT_UPLOAD_TO_CLOUD": true,
```

The variables are described as follows:

| Variable                                | Description                                           |
| -----------------------------------     | ----------------------------------------------------- |
| FILE_STORAGE_PROVIDER                   | Options can be `Local`, `AzureBlob`, or `S3`          |
| FILE_STORAGE_MAX_FILE_SIZE              | Specified as the number of bytes                      |
| FILE_STORAGE_MAX_TOTAL_DISK_SPACE       | Specified as the number of bytes                      |
| FILE_STORAGE_ALLOWED_MIMETYPES          | Can give a specific mimetype, or use wildcard matches |
| FILE_STORAGE_USE_DIRECT_UPLOAD_TO_CLOUD | Recommended set to true if using `AzureBlob` or `S3`. |

> Note: If you set `FILE_STORAGE_USE_DIRECT_UPLOAD_TO_CLOUD` to `true`, you should properly configure CORS on your buckets for this to work.

## Local file system

To use the local file system, make sure the `FILE_STORAGE_PROVIDER` environment variable is set to `Local`.

Also set the following Environment Variable whose value will be your local file system directory:

```
 "FILE_STORAGE_LOCAL_DIRECTORY": "user-uploads"
```

> Note: If you use local file storage in production, you may not be able to accept file uploads larger than 25 MB due to Kestrel server limits and other possible issues.

## Azure blob storage

To use azure blob storage, make sure the `FILE_STORAGE_PROVIDER` environment variable is set to `AzureBlob`.

You are required to set your Azure Blob connection string and provide the container name. Setting a custom domain is optional. If you plan to [have your files at a custom domain](https://raytha.com/blog/Setup-custom-domain-on-Azure-Blob-Storage-behind-reverse-proxy) such as files.mydomain.com, provide the domain in the environment variable.

```
  "FILE_STORAGE_AZUREBLOB_CONNECTION_STRING": "",
  "FILE_STORAGE_AZUREBLOB_CONTAINER": "",
  "FILE_STORAGE_AZUREBLOB_CUSTOM_DOMAIN": "",
```

## S3-compatible storage
To use an s3-compatible storage provider, make sure the `FILE_STORAGE_PROVIDER` environment variable is set to `S3`.

You must set all four of the following environment variables:

```
  "FILE_STORAGE_S3_ACCESS_KEY": "",
  "FILE_STORAGE_S3_SECRET_KEY": "",
  "FILE_STORAGE_S3_SERVICE_URL": "",
  "FILE_STORAGE_S3_BUCKET": "",
```

For AWS, their service URL format follows the format of <strong>s3.[region].amazonaws.com</strong>.

Replace [region] with the code for the specific region:

* US East (N. Virginia): us-east-1
* US East (Ohio): us-east-2
* US West (N. California): us-west-1
* US West (Oregon): us-west-2

Therefore, for example, the service URL for Amazon S3 in the US West (Oregon) region would be `s3.us-west-2.amazonaws.com`.


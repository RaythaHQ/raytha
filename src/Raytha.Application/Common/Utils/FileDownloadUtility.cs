namespace Raytha.Application.Common.Utils;

public static class FileDownloadUtility
{
    public static async Task<FileInfo> DownloadFile(string fileUrl)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(fileUrl);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to retrieve file from {fileUrl}: {response.StatusCode} - {response.ReasonPhrase}");

        string contentType = response.Content.Headers.ContentType.ToString() ?? "application/octet-stream";
        bool isValidMimeType = FileStorageUtility.GetAllowedFileExtensionsFromConfig(FileStorageUtility.DEFAULT_ALLOWED_MIMETYPES).Any(s => s.Contains(contentType.Split('/')[0]));
        if (isValidMimeType && response.Content.Headers.ContentLength <= FileStorageUtility.DEFAULT_MAX_FILE_SIZE)
        {
            string fileExt = Path.GetExtension(response.Content.Headers.ContentDisposition?.FileName?.Replace("\"", "")) ?? ".bin";

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            var fileInfo = new FileInfo()
            {
                FileMemoryStream = memoryStream,
                FileExt = fileExt,
                ContentType = contentType
            };
            return fileInfo;
        }
        else
            return null;
    }
    public class FileInfo
    {
        public MemoryStream FileMemoryStream { get; set; }
        public string FileExt { get; set; }
        public string ContentType { get; set; }
    }
}

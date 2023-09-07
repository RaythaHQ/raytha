using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raytha.Application.Common.Utils
{
    public static class FileHelper
    {
        public static async Task<FileInfo> DownloadFile(string fileUrl)
        {
            var client = new HttpClient();
            var response = await client.GetAsync(fileUrl);
            string contentType = response.Content.Headers.ContentType.ToString();
            bool isValidMimeType = FileStorageUtility.GetAllowedFileExtensionsFromConfig(FileStorageUtility.DEFAULT_ALLOWED_MIMETYPES).Any(s => s.Contains(contentType.Split('/')[0]));
            if (isValidMimeType && response.Content.Headers.ContentLength <= FileStorageUtility.DEFAULT_MAX_FILE_SIZE)
            {
                string fileExt = Path.GetExtension(response.Content.Headers.ContentDisposition.FileName.ToString().Replace("\"", ""));

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

}

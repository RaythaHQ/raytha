using System.Linq.Expressions;

namespace Raytha.Application.MediaItems;

public class MediaItemJson
{
    public required string FileName { get; init; }
    public required string DownloadUrl { get; set; }

    public static Expression<Func<string, string, MediaItemJson>> GetProjection()
    {
        return (fileName, downloadUrl) => GetProjection(fileName, downloadUrl);
    }

    public static MediaItemJson GetProjection(string fileName, string downloadUrl)
    {
        return new MediaItemJson
        {
            FileName = fileName,
            DownloadUrl = downloadUrl,
        };
    }
}
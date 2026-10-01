namespace Raytha.Application.Common.Utils;

/// <summary>
/// Local theme and media files are served from this site, so a template should reference them
/// with a root-relative path. A host-absolute URL breaks when the public host is not the host
/// the request arrived on. Presigned cloud URLs stay absolute.
/// </summary>
public static class PublicAssetUrl
{
    public static string PreferRootRelative(string downloadUrl)
    {
        if (
            Uri.TryCreate(downloadUrl, UriKind.Absolute, out var absolute)
            && absolute.AbsolutePath.Contains("/_static-files/", StringComparison.Ordinal)
        )
        {
            return absolute.PathAndQuery;
        }

        return downloadUrl;
    }
}

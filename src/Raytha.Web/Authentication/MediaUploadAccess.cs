using System.Collections.Generic;
using System.Linq;
using Raytha.Domain.Entities;

namespace Raytha.Web.Authentication;

/// <summary>
/// Who may attach a file from an editor. Deliberately wider than the media library
/// (<see cref="BuiltInSystemPermission.MANAGE_MEDIA_ITEMS_PERMISSION"/>): content authors and
/// template authors upload without being able to browse or delete other people's files.
/// </summary>
public static class MediaUploadAccess
{
    private static readonly string[] UploadingSystemPermissions =
    {
        BuiltInSystemPermission.MANAGE_MEDIA_ITEMS_PERMISSION,
        BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION,
        BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
    };

    public static bool IsAllowed(
        IEnumerable<string> systemPermissions,
        IEnumerable<string> contentTypePermissions
    ) =>
        systemPermissions.Any(UploadingSystemPermissions.Contains)
        || contentTypePermissions.Any(p =>
            p.EndsWith($"_{BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION}")
        );
}

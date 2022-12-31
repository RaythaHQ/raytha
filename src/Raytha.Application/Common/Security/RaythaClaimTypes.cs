namespace Raytha.Application.Common.Security;

public static class RaythaClaimTypes
{
    public const string LastModificationTime = "LastModificationTime";
    public const string IsAdmin = "IsAdmin";
    public const string SsoId = "SsoId";
    public const string SystemPermissions = "SystemPermissions";
    public const string ContentTypePermissions = "ContentTypePermissions";
    public const string AuthenticationScheme = "AuthenticationScheme";
    public const string UserGroups = "groups"; //https://www.rfc-editor.org/rfc/rfc9068.html
}

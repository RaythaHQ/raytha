namespace Raytha.Domain.Entities;

public class ContentTypeRolePermission : BaseAuditableEntity
{
    public Guid ContentTypeId { get; set; }
    public ContentTypePermissions ContentTypePermissions { get; set; }
    public Guid RoleId { get; set; }
    public virtual Role? Role { get; set; }
    public virtual ContentType? ContentType { get; set; }
}

[Flags]
public enum ContentTypePermissions
{
    None = 0,
    Read = 1,
    Edit = 2,
    Config = 4,
}

public class BuiltInContentTypePermission : ValueObject
{
    public const string CONTENT_TYPE_READ_PERMISSION = "read";
    public const string CONTENT_TYPE_EDIT_PERMISSION = "edit";
    public const string CONTENT_TYPE_CONFIG_PERMISSION = "config";

    static BuiltInContentTypePermission() { }

    private BuiltInContentTypePermission() { }

    private BuiltInContentTypePermission(
        string label,
        string developerName,
        ContentTypePermissions permission
    )
    {
        Label = label;
        DeveloperName = developerName;
        Permission = permission;
    }

    public static BuiltInContentTypePermission From(string developerName)
    {
        var type = Permissions.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedContentTypePermissionException(developerName);
        }

        return type;
    }

    public static IEnumerable<BuiltInContentTypePermission> From(ContentTypePermissions permission)
    {
        var permissions = new List<BuiltInContentTypePermission>();

        if (permission.HasFlag(ContentTypePermissions.Read))
            permissions.Add(Read);
        if (permission.HasFlag(ContentTypePermissions.Edit))
            permissions.Add(Edit);
        if (permission.HasFlag(ContentTypePermissions.Config))
            permissions.Add(Config);

        return permissions;
    }

    public static ContentTypePermissions From(params string[] developerNames)
    {
        ContentTypePermissions builtPermission = ContentTypePermissions.None;
        foreach (string developerName in developerNames)
        {
            var type = Permissions.FirstOrDefault(p => p.DeveloperName == developerName);

            if (type == null)
            {
                throw new UnsupportedContentTypePermissionException(developerName);
            }

            builtPermission = builtPermission | type.Permission;
        }
        return builtPermission;
    }

    public static BuiltInContentTypePermission Read =>
        new("Read", CONTENT_TYPE_READ_PERMISSION, ContentTypePermissions.Read);
    public static BuiltInContentTypePermission Edit =>
        new("Edit", CONTENT_TYPE_EDIT_PERMISSION, ContentTypePermissions.Edit);
    public static BuiltInContentTypePermission Config =>
        new("Config", CONTENT_TYPE_CONFIG_PERMISSION, ContentTypePermissions.Config);

    public string Label { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;
    public ContentTypePermissions Permission { get; private set; } = ContentTypePermissions.None;

    public static implicit operator ContentTypePermissions(BuiltInContentTypePermission scheme)
    {
        return scheme.Permission;
    }

    public static explicit operator BuiltInContentTypePermission(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<BuiltInContentTypePermission> Permissions
    {
        get
        {
            yield return Read;
            yield return Edit;
            yield return Config;
        }
    }

    public static ContentTypePermissions AllPermissionsAsEnum
    {
        get { return Read.Permission | Edit.Permission | Config.Permission; }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}

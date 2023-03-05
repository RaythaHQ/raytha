namespace Raytha.Domain.Exceptions;

public class UnsupportedContentTypePermissionException : Exception
{
    public UnsupportedContentTypePermissionException(string developerName)
        : base($"Template type \"{developerName}\" is unsupported.")
    {
    }
}

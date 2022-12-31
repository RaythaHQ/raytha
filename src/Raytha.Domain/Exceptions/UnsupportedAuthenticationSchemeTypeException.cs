namespace Raytha.Domain.Exceptions;

public class UnsupportedAuthenticationSchemeTypeException : Exception
{
    public UnsupportedAuthenticationSchemeTypeException(string developerName)
        : base($"Authentication scheme type \"{developerName}\" is unsupported.")
    {
    }
}

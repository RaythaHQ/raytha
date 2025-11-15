namespace Raytha.Domain.Exceptions;

public class ReservedContentTypeFieldNotFoundException : Exception
{
    public ReservedContentTypeFieldNotFoundException(string developerName)
        : base($"Reserved content type field \"{developerName}\" is unknown.") { }
}

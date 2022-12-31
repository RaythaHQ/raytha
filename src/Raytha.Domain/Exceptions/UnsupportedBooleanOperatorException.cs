namespace Raytha.Domain.Exceptions;

public class UnsupportedBooleanOperatorException : Exception
{
    public UnsupportedBooleanOperatorException(string developerName)
        : base($"Boolean operator \"{developerName}\" is unsupported.")
    {
    }
}

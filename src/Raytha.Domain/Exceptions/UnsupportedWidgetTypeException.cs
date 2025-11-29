namespace Raytha.Domain.Exceptions;

public class UnsupportedWidgetTypeException : Exception
{
    public UnsupportedWidgetTypeException(string developerName)
        : base($"Widget type \"{developerName}\" is unsupported.") { }
}


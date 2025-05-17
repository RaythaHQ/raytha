namespace Raytha.Domain.Exceptions;

public class UnsupportedTemplateTypeException : Exception
{
    public UnsupportedTemplateTypeException(string developerName)
        : base($"Template type \"{developerName}\" is unsupported.") { }
}

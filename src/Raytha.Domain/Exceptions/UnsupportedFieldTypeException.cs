namespace Raytha.Domain.Exceptions;

public class UnsupportedFieldTypeException : Exception
{
    public UnsupportedFieldTypeException(string developerName)
        : base($"Field type \"{developerName}\" is unsupported.") { }
}

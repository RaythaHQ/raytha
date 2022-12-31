namespace Raytha.Domain.Exceptions;

public class UnsupportedConditionOperatorException : Exception
{
    public UnsupportedConditionOperatorException(string developerName)
        : base($"Condition operator \"{developerName}\" is unsupported.")
    {
    }
}

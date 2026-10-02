namespace Raytha.Application.Common.Exceptions;

public class InvalidFilterException : Exception
{
    public InvalidFilterException(string message)
        : base(message) { }
}

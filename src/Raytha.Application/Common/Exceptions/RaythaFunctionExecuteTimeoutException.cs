namespace Raytha.Application.Common.Exceptions;

public class RaythaFunctionExecuteTimeoutException : OperationCanceledException
{
    public RaythaFunctionExecuteTimeoutException(string message)
        : base(message) { }
}

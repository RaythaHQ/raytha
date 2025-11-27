namespace Raytha.Infrastructure.RaythaFunctions;

/// <summary>
/// .NET representation of the result from a Raytha function execution.
/// Used to marshal the JavaScript result object before the V8 engine is disposed.
/// Property names are lowercase to match JavaScript convention used in the script.
/// </summary>
public class RaythaFunctionResult
{
    public string contentType { get; set; }
    public object body { get; set; }
    public int statusCode { get; set; }
}

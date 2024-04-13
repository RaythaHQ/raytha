namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionScriptEngine
{
    public void Initialize(string code);
    public Task<object> EvaluateGet(string query, TimeSpan executeTimeout, CancellationToken cancellationToken);
    public Task<object> EvaluatePost(string payload, string query, TimeSpan executeTimeout, CancellationToken cancellationToken);
}
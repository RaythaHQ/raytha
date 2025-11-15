namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionScriptEngine
{
    public Task<object> EvaluateGet(
        string code,
        string query,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    );
    public Task<object> EvaluatePost(
        string code,
        string payload,
        string query,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    );
    public Task EvaluateRun(
        string code,
        string payload,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    );
}

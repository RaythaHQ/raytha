using System.Text.Json;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionScriptEngine : IRaythaFunctionScriptEngine
{
    private readonly IV8EnginePool _enginePool;
    private readonly IRaythaFunctionApi_V1 _raythaFunctionApiV1;
    private readonly IEmailer _emailer;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly IRaythaFunctionsHttpClient _httpClient;

    public RaythaFunctionScriptEngine(
        IV8EnginePool enginePool,
        IRaythaFunctionApi_V1 raythaFunctionApiV1,
        IEmailer emailer,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        IRaythaFunctionsHttpClient httpClient
    )
    {
        _enginePool = enginePool;
        _raythaFunctionApiV1 = raythaFunctionApiV1;
        _emailer = emailer;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _httpClient = httpClient;
    }

    public async Task<object> Evaluate(
        string code,
        string method,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        V8ScriptEngine engine = _enginePool.Rent();
        try
        {
            // Add per-request host objects
            engine.AddHostObject("API_V1", _raythaFunctionApiV1);
            engine.AddHostObject("CurrentOrganization", _currentOrganization);
            engine.AddHostObject("CurrentUser", _currentUser);
            engine.AddHostObject("Emailer", _emailer);
            engine.AddHostObject("HttpClient", _httpClient);

            engine.Execute(code);
            var scriptResult = await Task.Run(
                    async () =>
                    {
                        var result = engine.Evaluate(method);

                        // The script can be synchronous or asynchronous, so this simple solution is used to convert the result
                        // Source: https://github.com/microsoft/ClearScript/issues/366
                        try
                        {
                            return await result.ToTask();
                        }
                        catch (ArgumentException)
                        {
                            return result;
                        }
                    },
                    cancellationToken
                )
                .WaitAsync(executeTimeout, cancellationToken);

            // Marshal the result to a .NET object before disposing the engine
            return MarshalResult(engine, scriptResult);
        }
        catch (TimeoutException)
        {
            throw new RaythaFunctionExecuteTimeoutException(
                "The function execution time has exceeded the timeout"
            );
        }
        catch (ScriptEngineException exception)
        {
            throw new RaythaFunctionScriptException(exception.ErrorDetails);
        }
        finally
        {
            _enginePool.Return(engine);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    /// <summary>
    /// Converts the JavaScript result object to a .NET object so it can be safely
    /// used after the V8 engine is disposed.
    /// </summary>
    private static object MarshalResult(V8ScriptEngine engine, object scriptResult)
    {
        if (scriptResult == null || scriptResult is Undefined)
        {
            return null;
        }

        // Store the result in a temp variable so we can use JSON.stringify on it
        engine.Script.__marshalTemp = scriptResult;

        try
        {
            // Check if this is a structured result (JsonResult, HtmlResult, etc.)
            var contentType = engine.Evaluate("__marshalTemp.contentType") as string;

            if (!string.IsNullOrEmpty(contentType))
            {
                var result = new RaythaFunctionResult { contentType = contentType };

                // Get the body value
                var bodyValue = engine.Evaluate("__marshalTemp.body");

                if (bodyValue is null or Undefined)
                {
                    result.body = null;
                }
                else if (bodyValue is string bodyString)
                {
                    result.body = bodyString;
                }
                else if (bodyValue is ScriptObject)
                {
                    // Pure JavaScript object - use JS JSON.stringify
                    var bodyJson = engine.Evaluate("JSON.stringify(__marshalTemp.body)") as string;
                    if (!string.IsNullOrEmpty(bodyJson))
                    {
                        result.body = JsonSerializer.Deserialize<JsonElement>(bodyJson);
                    }
                }
                else
                {
                    // .NET object (e.g., from API_V1 calls) - use .NET serializer
                    // This properly handles IEnumerable, complex DTOs, etc.
                    var bodyJson = JsonSerializer.Serialize(bodyValue, JsonOptions);
                    result.body = JsonSerializer.Deserialize<JsonElement>(bodyJson);
                }

                // Get statusCode if present
                var statusCodeValue = engine.Evaluate("__marshalTemp.statusCode");
                if (statusCodeValue is int sc)
                {
                    result.statusCode = sc;
                }
                else if (statusCodeValue is double scd)
                {
                    result.statusCode = (int)scd;
                }

                return result;
            }
        }
        catch
        {
            // Not a structured result, fall through
        }

        // For non-structured results, return primitives directly or serialize objects
        if (scriptResult is string or int or double or bool)
        {
            return scriptResult;
        }

        // Try to serialize unknown objects
        try
        {
            if (scriptResult is ScriptObject)
            {
                var json = engine.Evaluate("JSON.stringify(__marshalTemp)") as string;
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<JsonElement>(json);
                }
            }
            else
            {
                // .NET object
                var json = JsonSerializer.Serialize(scriptResult, JsonOptions);
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
        }
        catch
        {
            // Fall back to string representation
        }

        return scriptResult?.ToString();
    }

    public async Task<object> EvaluateGet(
        string code,
        string query,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        var result = await Evaluate(code, $"get({query})", executeTimeout, cancellationToken);
        return result;
    }

    public async Task<object> EvaluatePost(
        string code,
        string payload,
        string query,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        return await Evaluate(code, $"post({payload}, {query})", executeTimeout, cancellationToken);
    }

    public async Task EvaluateRun(
        string code,
        string payload,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        await Evaluate(code, $"run({payload})", executeTimeout, cancellationToken);
    }
}

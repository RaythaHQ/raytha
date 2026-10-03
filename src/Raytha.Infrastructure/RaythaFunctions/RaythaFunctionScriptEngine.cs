using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionScriptEngine : IRaythaFunctionScriptEngine
{
    private static readonly AsyncLocal<CancellationToken> CurrentExecutionAborted = new();

    // Host objects pass this on so work a function started (outbound requests, nested functions)
    // stops with it; otherwise a timed-out function that called itself keeps its chain alive.
    internal static CancellationToken ExecutionAborted => CurrentExecutionAborted.Value;

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

    // Parses request data that arrived as text. Empty is null; JSON becomes a value; anything else
    // stays a string. The text is a host property, never a piece of the script, so a body cannot
    // become code that runs with API_V1.
    private const string BindRequestData = """
        function __raythaParse(raw) {
            if (raw === null || raw === undefined) return null;
            var text = String(raw);
            if (text.trim() === '') return null;
            try { return JSON.parse(text); } catch (e) { return text; }
        }
        var __raytha_payload = __raythaParse(__raytha_payload_json);
        var __raytha_query = __raythaParse(__raytha_query_json);
        var __raytha_args = __raythaParse(__raytha_args_json);
        """;

    private static readonly Regex MethodName = new(
        @"^[A-Za-z_$][A-Za-z0-9_$]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public async Task<object> Evaluate(
        string code,
        string method,
        string? payloadJson,
        string? queryJson,
        string? argsJson,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        V8ScriptEngine engine = _enginePool.Rent();
        engine.AddHostObject("API_V1", _raythaFunctionApiV1);
        engine.AddHostObject("CurrentOrganization", FunctionCurrentOrganization.From(_currentOrganization));
        engine.AddHostObject("CurrentUser", _currentUser);
        engine.AddHostObject("Emailer", _emailer);
        engine.AddHostObject("HttpClient", _httpClient);

        var abort = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        abort.CancelAfter(executeTimeout);
        var interruptOnAbort = abort.Token.Register(engine.Interrupt);
        var execution = Task.Run(
            () => Run(engine, code, method, payloadJson, queryJson, argsJson, abort.Token)
        );
        try
        {
            return await execution.WaitAsync(abort.Token);
        }
        catch (Exception exception)
            when (abort.IsCancellationRequested
                && exception is OperationCanceledException or ScriptInterruptedException
            )
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            _ = ReturnWhenFinished(engine, execution, abort, interruptOnAbort);
        }
    }

    private static async Task<object> Run(
        V8ScriptEngine engine,
        string code,
        string method,
        string? payloadJson,
        string? queryJson,
        string? argsJson,
        CancellationToken abort
    )
    {
        CurrentExecutionAborted.Value = abort;
        // Interrupt only stops a script that is already running, so an abort that fired before
        // this task was scheduled has to be honored here.
        abort.ThrowIfCancellationRequested();
        engine.Script.__raytha_payload_json = payloadJson ?? "";
        engine.Script.__raytha_query_json = queryJson ?? "";
        engine.Script.__raytha_args_json = argsJson ?? "";
        engine.Execute(BindRequestData);
        engine.Execute(code);
        var result = engine.Evaluate(method);

        // The script can be synchronous or asynchronous, so this simple solution is used to convert the result
        // Source: https://github.com/microsoft/ClearScript/issues/366
        try
        {
            result = await result.ToTask().WaitAsync(abort);
        }
        catch (ArgumentException) { }

        return MarshalResult(engine, result);
    }

    // An aborted script can still be inside a blocking host call, where Interrupt has no effect until
    // it re-enters JavaScript, so the engine is released only once the script has actually stopped.
    private async Task ReturnWhenFinished(
        V8ScriptEngine engine,
        Task execution,
        CancellationTokenSource abort,
        CancellationTokenRegistration interruptOnAbort
    )
    {
        await execution.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        interruptOnAbort.Dispose();
        abort.Dispose();
        _enginePool.Return(engine);
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
            var kind = engine.Evaluate("__marshalTemp.kind") as string;

            if (!string.IsNullOrEmpty(contentType) || kind == "content")
            {
                var result = new RaythaFunctionResult { contentType = contentType, kind = kind };

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
        return await Evaluate(
            code,
            "get(__raytha_query)",
            payloadJson: null,
            query,
            argsJson: null,
            executeTimeout,
            cancellationToken
        );
    }

    public async Task<object> EvaluatePost(
        string code,
        string payload,
        string query,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        return await Evaluate(
            code,
            "post(__raytha_payload, __raytha_query)",
            payload,
            query,
            argsJson: null,
            executeTimeout,
            cancellationToken
        );
    }

    public async Task EvaluateRun(
        string code,
        string payload,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        await Evaluate(
            code,
            "run(__raytha_payload)",
            payload,
            queryJson: null,
            argsJson: null,
            executeTimeout,
            cancellationToken
        );
    }

    public async Task<object> EvaluateInternal(
        string code,
        string methodName,
        string argsJson,
        TimeSpan executeTimeout,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(methodName) || !MethodName.IsMatch(methodName))
        {
            throw new RaythaFunctionScriptException($"'{methodName}' is not a function name.");
        }

        return await Evaluate(
            code,
            $"{methodName}(__raytha_args)",
            payloadJson: null,
            queryJson: null,
            argsJson,
            executeTimeout,
            cancellationToken
        );
    }
}

using CSharpVitamins;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;

namespace Raytha.Infrastructure.RaythaFunctions;

public class RaythaFunctionScriptEngine : IRaythaFunctionScriptEngine
{
    private readonly IRaythaFunctionApi_V1 _raythaFunctionApiV1;
    private readonly IEmailer _emailer;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly IRaythaFunctionsHttpClient _httpClient;

    public RaythaFunctionScriptEngine(IRaythaFunctionApi_V1 raythaFunctionApiV1,
        IEmailer emailer,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        IRaythaFunctionsHttpClient httpClient)
    {
        _raythaFunctionApiV1 = raythaFunctionApiV1;
        _emailer = emailer;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _httpClient = httpClient;
    }

    public async Task<object> Evaluate(string code, string method, TimeSpan executeTimeout, CancellationToken cancellationToken)
    {
        using (var _engine = new V8ScriptEngine())
        {
            _engine.AddHostObject("API_V1", _raythaFunctionApiV1);
            _engine.AddHostObject("CurrentOrganization", _currentOrganization);
            _engine.AddHostObject("CurrentUser", _currentUser);
            _engine.AddHostObject("Emailer", _emailer);
            _engine.AddHostObject("HttpClient", _httpClient);
            _engine.AddHostObject("host", new HostFunctions());
            _engine.AddHostObject("clr", new HostTypeCollection("mscorlib", "System", "System.Core", "System.Linq", "System.Collections"));
            _engine.AddHostType(typeof(JavaScriptExtensions));
            _engine.AddHostType(typeof(Enumerable));
            _engine.AddHostType(typeof(ShortGuid));
            _engine.AddHostType(typeof(Guid));
            _engine.AddHostType(typeof(Convert));
            _engine.AddHostType(typeof(EmailMessage));
            _engine.Execute("var System = clr.System;");
            _engine.Execute(@"
            class JsonResult {
              constructor(obj) {
                this.body = obj;
                this.contentType = 'application/json';
              }
            }

            class HtmlResult {
              constructor(html) {
                this.body = html;
                this.contentType = 'text/html';
              }
            }

            class RedirectResult {
              constructor(url) {
                this.body = url;
                this.contentType = 'redirectToUrl';
              }
            }

            class StatusCodeResult {
              constructor(statusCode, error) {
                this.statusCode = statusCode;
                this.body = error;
                this.contentType = 'statusCode';
              }
            }");

            try
            {
                _engine.Execute(code);
                return await Task.Run(async () =>
                {
                    var result = _engine.Evaluate(method);

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
                }, cancellationToken).WaitAsync(executeTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new RaythaFunctionExecuteTimeoutException("The function execution time has exceeded the timeout");
            }
            catch (ScriptEngineException exception)
            {
                throw new RaythaFunctionScriptException(exception.ErrorDetails);
            }
        }
    }

    public async Task<object> EvaluateGet(string code,string query, TimeSpan executeTimeout, CancellationToken cancellationToken)
    {
        return await Evaluate(code, $"get({query})", executeTimeout, cancellationToken);
    }

    public async Task<object> EvaluatePost(string code, string payload, string query, TimeSpan executeTimeout, CancellationToken cancellationToken)
    {
        return await Evaluate(code, $"post({payload}, {query})", executeTimeout, cancellationToken);
    }

    public async Task EvaluateRun(string code, string payload, TimeSpan executeTimeout, CancellationToken cancellationToken)
    {
        await Evaluate(code, $"run({payload})", executeTimeout, cancellationToken);
    }
}
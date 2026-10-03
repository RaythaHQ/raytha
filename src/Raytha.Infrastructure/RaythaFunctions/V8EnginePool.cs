using System.Collections.Concurrent;
using CSharpVitamins;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;

namespace Raytha.Infrastructure.RaythaFunctions;

public class V8EnginePool : IV8EnginePool
{
    private readonly ConcurrentBag<V8ScriptEngine> _engines;
    private readonly int _maxPoolSize;
    private volatile bool _disposed;

    public V8EnginePool(int maxPoolSize = 10)
    {
        _maxPoolSize = maxPoolSize;
        _engines = new ConcurrentBag<V8ScriptEngine>();

        // Pre-warm the pool with a few engines
        for (int i = 0; i < Math.Min(2, maxPoolSize); i++)
        {
            _engines.Add(CreateConfiguredEngine());
        }
    }

    public V8ScriptEngine Rent()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(V8EnginePool));

        if (_engines.TryTake(out var engine))
            return engine;

        return CreateConfiguredEngine();
    }

    public void Return(V8ScriptEngine engine)
    {
        if (_disposed)
        {
            engine.Dispose();
            return;
        }

        // Dispose the used engine (it has per-request host objects attached)
        // and create a fresh one for the pool
        engine.Dispose();

        if (_engines.Count < _maxPoolSize)
        {
            _engines.Add(CreateConfiguredEngine());
        }
    }

    private V8ScriptEngine CreateConfiguredEngine()
    {
        // Engines on a shared V8Runtime share one isolate and run one at a time, so a function
        // blocked in a host call (HttpClient, API_V1.ExecuteRaythaFunction) would stall every other.
        var engine = new V8ScriptEngine();
        LoadHostTypes(engine);
        LoadResultClasses(engine);
        return engine;
    }

    private static void LoadHostTypes(V8ScriptEngine engine)
    {
        engine.AddHostType(typeof(JavaScriptExtensions));
        engine.AddHostType(typeof(Enumerable));
        engine.AddHostType(typeof(ShortGuid));
        engine.AddHostType(typeof(Guid));
        engine.AddHostType(typeof(Convert));
        engine.AddHostType(typeof(EmailMessage));
        engine.AddHostType(typeof(List<>));
        engine.AddHostType(typeof(Dictionary<,>));
        engine.AddHostType(typeof(KeyValuePair<,>));
        engine.AddHostType(typeof(HashSet<>));
        engine.AddHostType(typeof(Queue<>));
        engine.AddHostType(typeof(Stack<>));
        engine.AddHostType(typeof(DateTime));
        engine.AddHostType(typeof(DateTimeOffset));
        engine.AddHostType(typeof(DateOnly));
        engine.AddHostType(typeof(TimeOnly));
        engine.AddHostType(typeof(TimeSpan));
        // System.Math must not occupy the JavaScript Math global (Math.floor, Math.random).
        engine.AddHostType("DotNetMath", typeof(Math));
        engine.AddHostType(typeof(decimal));
        engine.AddHostType(typeof(char));
        engine.AddHostType(typeof(Random));
        engine.AddHostType(typeof(Uri));
        engine.AddHostType(typeof(UriBuilder));
        engine.AddHostType(typeof(System.Text.RegularExpressions.Regex));
        engine.AddHostType(typeof(System.Text.StringBuilder));
        engine.AddHostType(typeof(System.Text.Encoding));
        engine.AddHostType(typeof(StringComparison));
        engine.AddHostType(typeof(System.Diagnostics.Stopwatch));
        engine.AddHostType(typeof(BitConverter));
        engine.AddHostType(typeof(Tuple));
        engine.AddHostType(typeof(ValueTuple));
    }

    private static void LoadResultClasses(V8ScriptEngine engine)
    {
        engine.Execute(
            @"
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

        class XmlResult {
          constructor(xml) {
            this.body = xml;
            this.contentType = 'application/xml';
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
        }

        class ContentResult {
          constructor(body, contentType = 'text/plain; charset=utf-8', statusCode = 200) {
            this.body = body;
            this.contentType = contentType;
            this.statusCode = statusCode;
            this.kind = 'content';
          }
        }

        class TextResult extends ContentResult {
          constructor(text) {
            super(text);
          }
        }"
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        while (_engines.TryTake(out var engine))
        {
            engine.Dispose();
        }
    }
}

using System.Diagnostics;
using FluentAssertions;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Infrastructure.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionScriptEngineTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Generous = TimeSpan.FromSeconds(10);
    private const string Plain = "function get(query) { return 'ok'; }";

    private V8EnginePool _pool = null!;
    private FakeHttpClient _httpClient = null!;
    private RaythaFunctionScriptEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _pool = new V8EnginePool(maxPoolSize: 4);
        _httpClient = new FakeHttpClient();
        _engine = new RaythaFunctionScriptEngine(
            _pool,
            Mock.Of<IRaythaFunctionApi_V1>(),
            Mock.Of<IEmailer>(),
            Mock.Of<ICurrentOrganization>(),
            Mock.Of<ICurrentUser>(),
            _httpClient
        );
    }

    [TearDown]
    public void TearDown() => _pool.Dispose();

    [Test]
    public async Task A_synchronous_infinite_loop_times_out_and_the_next_evaluation_succeeds()
    {
        var watch = Stopwatch.StartNew();
        var loop = () =>
            _engine.EvaluateGet(
                "function get(query) { while (true) {} }",
                "{}",
                ShortTimeout,
                CancellationToken.None
            );

        await loop.Should()
            .ThrowAsync<RaythaFunctionExecuteTimeoutException>()
            .WaitAsync(Generous);
        watch.Elapsed.Should().BeLessThan(ShortTimeout + Margin);

        (await EvaluatePlain()).Should().Be("ok");
    }

    [Test]
    public async Task A_top_level_infinite_loop_times_out()
    {
        var watch = Stopwatch.StartNew();
        var loop = () =>
            _engine.EvaluateGet(
                "while (true) {}\n" + Plain,
                "{}",
                ShortTimeout,
                CancellationToken.None
            );

        await loop.Should()
            .ThrowAsync<RaythaFunctionExecuteTimeoutException>()
            .WaitAsync(Generous);
        watch.Elapsed.Should().BeLessThan(ShortTimeout + Margin);

        (await EvaluatePlain()).Should().Be("ok");
    }

    [Test]
    public async Task A_promise_that_never_settles_times_out()
    {
        var pending = () =>
            _engine.EvaluateGet(
                "async function get(query) { await new Promise(() => {}); }",
                "{}",
                ShortTimeout,
                CancellationToken.None
            );

        await pending.Should()
            .ThrowAsync<RaythaFunctionExecuteTimeoutException>()
            .WaitAsync(Generous);
    }

    [Test]
    public async Task Cancellation_interrupts_a_running_script()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var watch = Stopwatch.StartNew();
        var loop = () =>
            _engine.EvaluateGet(
                "function get(query) { while (true) {} }",
                "{}",
                TimeSpan.FromMinutes(1),
                cancellation.Token
            );

        var thrown = await loop.Should().ThrowAsync<OperationCanceledException>().WaitAsync(Generous);
        thrown.Which.Should().NotBeOfType<RaythaFunctionExecuteTimeoutException>();
        watch.Elapsed.Should().BeLessThan(Margin);

        (await EvaluatePlain()).Should().Be("ok");
    }

    [Test]
    public async Task Two_evaluations_run_at_the_same_time()
    {
        using var bothEntered = new Barrier(2);
        _httpClient.OnGet = _ => bothEntered.SignalAndWait(TimeSpan.FromSeconds(5)) ? "together" : "alone";
        const string code = "function get(query) { return HttpClient.Get('barrier'); }";

        var results = await Task.WhenAll(
                Task.Run(() => _engine.EvaluateGet(code, "{}", Generous, CancellationToken.None)),
                Task.Run(() => _engine.EvaluateGet(code, "{}", Generous, CancellationToken.None))
            )
            .WaitAsync(TimeSpan.FromSeconds(20));

        results.Should().Equal("together", "together");
    }

    [Test]
    public async Task A_function_can_evaluate_another_function_from_a_host_call()
    {
        _httpClient.OnGet = _ =>
            _engine
                .EvaluateGet(
                    "function get(query) { return 'inner ' + query.n; }",
                    "{ n: 1 }",
                    Generous,
                    CancellationToken.None
                )
                .GetAwaiter()
                .GetResult();

        var result = await Task.Run(() =>
                _engine.EvaluateGet(
                    "function get(query) { return 'outer got ' + HttpClient.Get('inner'); }",
                    "{}",
                    Generous,
                    CancellationToken.None
                )
            )
            .WaitAsync(TimeSpan.FromSeconds(20));

        result.Should().Be("outer got inner 1");
    }

    [Test]
    public async Task A_timed_out_script_blocked_in_a_host_call_does_not_stall_other_functions()
    {
        using var release = new ManualResetEventSlim();
        _httpClient.OnGet = _ =>
        {
            release.Wait(TimeSpan.FromSeconds(30));
            return "late";
        };
        var blocked = () =>
            _engine.EvaluateGet(
                "function get(query) { return HttpClient.Get('slow'); }",
                "{}",
                ShortTimeout,
                CancellationToken.None
            );

        var watch = Stopwatch.StartNew();
        await blocked.Should()
            .ThrowAsync<RaythaFunctionExecuteTimeoutException>()
            .WaitAsync(Generous);
        watch.Elapsed.Should().BeLessThan(ShortTimeout + Margin);

        (await EvaluatePlain()).Should().Be("ok");
        release.Set();
    }

    [Test]
    public async Task A_timed_out_function_cancels_its_outbound_request()
    {
        using var handler = new HangingHandler();
        var engine = new RaythaFunctionScriptEngine(
            _pool,
            Mock.Of<IRaythaFunctionApi_V1>(),
            Mock.Of<IEmailer>(),
            Mock.Of<ICurrentOrganization>(),
            Mock.Of<ICurrentUser>(),
            new RaythaFunctionsHttpClient(new HttpClient(handler))
        );
        var call = () =>
            engine.EvaluateGet(
                "function get(query) { return HttpClient.Get('http://nested.invalid/'); }",
                "{}",
                ShortTimeout,
                CancellationToken.None
            );

        await call.Should()
            .ThrowAsync<RaythaFunctionExecuteTimeoutException>()
            .WaitAsync(Generous);

        (await handler.Cancelled.Task.WaitAsync(Generous)).Should().BeTrue();
    }

    private Task<object> EvaluatePlain() =>
        _engine.EvaluateGet(Plain, "{}", Generous, CancellationToken.None).WaitAsync(Generous);

    public class FakeHttpClient : IRaythaFunctionsHttpClient
    {
        public Func<string, object> OnGet { get; set; } = _ => "";

        public dynamic Get(string url, IDictionary<string, object>? headers = null) => OnGet(url);

        public dynamic Post(
            string url,
            IDictionary<string, object>? headers = null,
            IDictionary<string, object>? body = null,
            bool json = true
        ) => throw new NotSupportedException();

        public dynamic Put(
            string url,
            IDictionary<string, object>? headers = null,
            IDictionary<string, object>? body = null,
            bool json = true
        ) => throw new NotSupportedException();

        public dynamic Delete(string url, IDictionary<string, object>? headers = null) =>
            throw new NotSupportedException();
    }

    private class HangingHandler : HttpMessageHandler
    {
        public TaskCompletionSource<bool> Cancelled { get; } = new();

        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
            Cancelled.TrySetResult(cancellationToken.IsCancellationRequested);
            cancellationToken.ThrowIfCancellationRequested();
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(Send(request, cancellationToken));
    }
}

using System.Diagnostics;
using FluentAssertions;
using Moq;
using Raytha.Application.AuthenticationSchemes;
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
                    """{"n":1}""",
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

    [Test]
    public async Task A_post_body_is_data_even_when_it_reads_as_javascript()
    {
        const string crafted = "); return 'pwned'; //";

        var result = await _engine.EvaluatePost(
            "function post(payload, query) { return payload; }",
            crafted,
            "[]",
            Generous,
            CancellationToken.None
        );

        result.Should().Be(crafted);
    }

    [Test]
    public async Task An_empty_post_body_arrives_as_null_and_json_arrives_parsed()
    {
        var empty = await _engine.EvaluatePost(
            "function post(payload, query) { return payload === null ? 'empty' : 'present'; }",
            "  ",
            "[]",
            Generous,
            CancellationToken.None
        );
        var parsed = await _engine.EvaluatePost(
            "function post(payload, query) { return payload.name; }",
            """{"name":"ada"}""",
            "[]",
            Generous,
            CancellationToken.None
        );

        empty.Should().Be("empty");
        parsed.Should().Be("ada");
    }

    [Test]
    public async Task An_internal_call_rejects_a_method_name_that_is_not_an_identifier()
    {
        var call = () =>
            _engine.EvaluateInternal(
                "function get(args) { return args; }",
                "get(); return",
                "{}",
                Generous,
                CancellationToken.None
            );

        await call.Should().ThrowAsync<RaythaFunctionScriptException>();
    }

    [Test]
    public async Task A_function_cannot_read_the_jwt_secret_or_the_saml_certificate()
    {
        var organization = new Mock<ICurrentOrganization>();
        organization.Setup(x => x.OrganizationName).Returns("Example");
        organization
            .Setup(x => x.AuthenticationSchemes)
            .Returns(
                [
                    new AuthenticationSchemeDto
                    {
                        Label = "Customer SSO",
                        DeveloperName = "customer_sso",
                        JwtSecretKey = "super-secret",
                        SamlCertificate = "cert-body",
                        SignOutUrl = "https://idp.example/logout",
                    },
                ]
            );
        var engine = new RaythaFunctionScriptEngine(
            _pool,
            Mock.Of<IRaythaFunctionApi_V1>(),
            Mock.Of<IEmailer>(),
            organization.Object,
            Mock.Of<ICurrentUser>(),
            _httpClient
        );
        const string probe = """
            function get(query) {
                var scheme = CurrentOrganization.AuthenticationSchemes[0];
                var leaked = false;
                try { leaked = scheme.JwtSecretKey === 'super-secret'; } catch (e) {}
                try { leaked = leaked || scheme.SamlCertificate === 'cert-body'; } catch (e) {}
                return leaked ? 'leaked' : scheme.DeveloperName;
            }
            """;

        (await engine.EvaluateGet(probe, "[]", Generous, CancellationToken.None))
            .Should()
            .Be("customer_sso");

        var dumped = await engine.EvaluateGet(
            "function get(query) { return CurrentOrganization; }",
            "[]",
            Generous,
            CancellationToken.None
        );
        dumped.ToString().Should().NotContain("super-secret").And.NotContain("cert-body");
        dumped.ToString().Should().Contain("https://idp.example/logout");
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

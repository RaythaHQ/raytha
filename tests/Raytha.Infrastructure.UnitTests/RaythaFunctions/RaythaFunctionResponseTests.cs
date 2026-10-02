using System.Text.Json;
using FluentAssertions;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Infrastructure.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionResponseTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private V8EnginePool _pool = null!;
    private RaythaFunctionScriptEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _pool = new V8EnginePool(maxPoolSize: 2);
        _engine = new RaythaFunctionScriptEngine(
            _pool,
            Mock.Of<IRaythaFunctionApi_V1>(),
            Mock.Of<IEmailer>(),
            Mock.Of<ICurrentOrganization>(),
            Mock.Of<ICurrentUser>(),
            Mock.Of<IRaythaFunctionsHttpClient>()
        );
    }

    [TearDown]
    public void TearDown() => _pool.Dispose();

    [Test]
    public async Task TextResult_is_utf8_plain_text_with_status_200()
    {
        var response = await Run("return new TextResult('# Site\\n> Summary');");

        response.Should().Be(new RaythaFunctionResponse.Content("# Site\n> Summary", "text/plain; charset=utf-8", 200));
    }

    [Test]
    public async Task ContentResult_carries_its_content_type_and_status()
    {
        var response = await Run("return new ContentResult('a,b', 'text/csv; charset=utf-8', 201);");

        response.Should().Be(new RaythaFunctionResponse.Content("a,b", "text/csv; charset=utf-8", 201));
    }

    [Test]
    public async Task ContentResult_with_a_json_content_type_sends_the_body_verbatim()
    {
        var response = await Run("return new ContentResult('{\"a\":1}', 'application/json');");

        response.Should().Be(new RaythaFunctionResponse.Content("{\"a\":1}", "application/json", 200));
    }

    [Test]
    public async Task JsonResult_keeps_its_object_body()
    {
        var response = await Run("return new JsonResult({ ok: true });");

        var json = response.Should().BeOfType<RaythaFunctionResponse.Json>().Subject;
        ((JsonElement)json.Body!).GetProperty("ok").GetBoolean().Should().BeTrue();
    }

    [TestCase("return new HtmlResult('<p>hi</p>');", "text/html", "<p>hi</p>")]
    [TestCase("return new XmlResult('<a/>');", "application/xml", "<a/>")]
    public async Task Html_and_xml_results_are_content_with_status_200(string script, string contentType, string body)
    {
        var response = await Run(script);

        response.Should().Be(new RaythaFunctionResponse.Content(body, contentType, 200));
    }

    [Test]
    public async Task RedirectResult_is_a_redirect()
    {
        (await Run("return new RedirectResult('/thanks');"))
            .Should()
            .Be(new RaythaFunctionResponse.Redirect("/thanks"));
    }

    [Test]
    public async Task StatusCodeResult_keeps_its_status_and_body()
    {
        (await Run("return new StatusCodeResult(404, 'Not Found');"))
            .Should()
            .Be(new RaythaFunctionResponse.Status(404, "Not Found"));
    }

    [TestCase("'text/plain\\r\\nSet-Cookie: a=b'")]
    [TestCase("'text/plain\\n'")]
    [TestCase("'not a media type'")]
    [TestCase("'text/'")]
    [TestCase("''")]
    public async Task ContentResult_with_a_bad_content_type_is_invalid_and_names_it(string contentType)
    {
        var response = await Run($"return new ContentResult('x', {contentType});");

        response.Should().BeOfType<RaythaFunctionResponse.Invalid>()
            .Which.Reason.Should().StartWith("The function returned")
            .And.Contain("content type");
    }

    [TestCase(99)]
    [TestCase(101)]
    [TestCase(199)]
    [TestCase(600)]
    [TestCase(0)]
    public async Task ContentResult_with_a_status_outside_200_to_599_is_invalid_and_names_it(int status)
    {
        var response = await Run($"return new ContentResult('x', 'text/plain', {status});");

        response.Should().BeOfType<RaythaFunctionResponse.Invalid>()
            .Which.Reason.Should().Contain($"status code {status}");
    }

    [Test]
    public async Task StatusCodeResult_with_a_status_outside_200_to_599_is_invalid()
    {
        (await Run("return new StatusCodeResult(1000, 'x');"))
            .Should()
            .BeOfType<RaythaFunctionResponse.Invalid>();
    }

    [Test]
    public async Task An_unknown_content_type_token_is_invalid_and_names_it()
    {
        var response = await Run("return { contentType: 'text/csv', body: 'a' };");

        response.Should().BeOfType<RaythaFunctionResponse.Invalid>()
            .Which.Reason.Should().Contain("\"text/csv\"");
    }

    [TestCase("return 'plain';")]
    [TestCase("return;")]
    public async Task A_value_that_is_not_a_result_helper_is_invalid(string script)
    {
        (await Run(script)).Should().BeOfType<RaythaFunctionResponse.Invalid>();
    }

    private async Task<RaythaFunctionResponse> Run(string body)
    {
        var result = await _engine
            .EvaluateGet($"function get(query) {{ {body} }}", "{}", Timeout, CancellationToken.None)
            .WaitAsync(Timeout);
        return RaythaFunctionResponse.From(result);
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Web.Areas.Api.Controllers.V1;

namespace Raytha.Architecture.Tests;

[TestFixture]
public class AbsoluteMediaUrlsTests
{
    private const string Origin = "https://cms.example.com";

    [TestCase(
        "/raytha/media-items/objectkey/abc_photo.png",
        "https://cms.example.com/raytha/media-items/objectkey/abc_photo.png"
    )]
    [TestCase(
        "<p><img src=\"/raytha/media-items/objectkey/abc_photo.png\" alt=\"\"></p>",
        "<p><img src=\"https://cms.example.com/raytha/media-items/objectkey/abc_photo.png\" alt=\"\"></p>"
    )]
    [TestCase(
        "<img src='/site/raytha/media-items/id/ReeNGRISE0CaDkCMXTft9w'>",
        "<img src='https://cms.example.com/site/raytha/media-items/id/ReeNGRISE0CaDkCMXTft9w'>"
    )]
    [TestCase(
        "{\"content\":\"\\u003Cimg src=\\u0022/raytha/media-items/objectkey/k.webp\\u0022\\u003E\"}",
        "{\"content\":\"\\u003Cimg src=\\u0022https://cms.example.com/raytha/media-items/objectkey/k.webp\\u0022\\u003E\"}"
    )]
    [TestCase(
        "background: url(/_static-files/k.png); srcset=\"/_static-files/a.png 1x, /_static-files/b.png 2x\"",
        "background: url(https://cms.example.com/_static-files/k.png); srcset=\"https://cms.example.com/_static-files/a.png 1x, https://cms.example.com/_static-files/b.png 2x\""
    )]
    [TestCase(
        "<a href=&quot;/raytha/media-items/objectkey/k.pdf&quot;>",
        "<a href=&quot;https://cms.example.com/raytha/media-items/objectkey/k.pdf&quot;>"
    )]
    public void Resolves_root_relative_media_urls_against_the_origin(string stored, string expected)
    {
        AbsoluteMediaUrlsAttribute.Absolutize(stored, Origin).Should().Be(expected);
    }

    [TestCase("http://localhost:5200/raytha/media-items/objectkey/k.png")]
    [TestCase("https://other.example/site/_static-files/k.png")]
    [TestCase("//cdn.example/raytha/media-items/objectkey/k.png")]
    [TestCase("<a href=\"/about\">About</a> <a href=\"/raytha/users\">Users</a>")]
    [TestCase("see docs/raytha/media-items/objectkey/k.png")]
    [TestCase("")]
    public void Leaves_everything_else_alone(string stored)
    {
        AbsoluteMediaUrlsAttribute.Absolutize(stored, Origin).Should().Be(stored);
    }

    [Test]
    public void Resolving_twice_is_the_same_as_once()
    {
        var once = AbsoluteMediaUrlsAttribute.Absolutize(
            "<img src=\"/raytha/media-items/objectkey/k.png\">",
            Origin
        );
        AbsoluteMediaUrlsAttribute.Absolutize(once, Origin).Should().Be(once);
    }

    [Test]
    public void Rewrites_every_string_in_a_json_tree_and_nothing_else()
    {
        var node = JsonNode.Parse(
            """
            {
              "published": { "hero": "/raytha/media-items/objectkey/k.png", "count": 3, "flag": true, "none": null },
              "widgets": [ { "settingsJson": "{\"imageUrl\":\"/raytha/media-items/objectkey/w.png\"}" }, "/about" ]
            }
            """
        );

        AbsoluteMediaUrlsAttribute.Absolutize(node, Origin);

        node!["published"]!["hero"]!.GetValue<string>()
            .Should()
            .Be("https://cms.example.com/raytha/media-items/objectkey/k.png");
        node["published"]!["count"]!.GetValue<int>().Should().Be(3);
        node["published"]!["flag"]!.GetValue<bool>().Should().BeTrue();
        node["published"]!["none"].Should().BeNull();
        node["widgets"]![0]!["settingsJson"]!.GetValue<string>()
            .Should()
            .Be("{\"imageUrl\":\"https://cms.example.com/raytha/media-items/objectkey/w.png\"}");
        node["widgets"]![1]!.GetValue<string>().Should().Be("/about");
    }

    [Test]
    public async Task The_filter_rewrites_the_response_for_the_requests_origin()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<JsonOptions>(o =>
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        );
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.com:8443");
        var result = new ObjectResult(
            new { Result = new { Content = "<img src=\"/raytha/media-items/objectkey/k.png\">" } }
        );
        var context = new ResultExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [],
            result,
            controller: new object()
        );

        await new AbsoluteMediaUrlsAttribute().OnResultExecutionAsync(
            context,
            () =>
                Task.FromResult(
                    new ResultExecutedContext(context, [], context.Result, context.Controller)
                )
        );

        result.DeclaredType.Should().Be(typeof(JsonNode));
        ((JsonNode)result.Value!)["result"]!["content"]!.GetValue<string>()
            .Should()
            .Be("<img src=\"https://api.example.com:8443/raytha/media-items/objectkey/k.png\">");
    }
}

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Interfaces;
using Raytha.Web.Areas.Api.Controllers.V1;
using Raytha.Web.Areas.Public.DbViewEngine;
using Raytha.Web.Services;
using Raytha.Web.Utils;

namespace Raytha.Architecture.Tests;

public class LiquidTemplateParserTests
{
    [Test]
    public void A_broken_if_tag_reports_line_and_column()
    {
        var error = new LiquidTemplateParser().GetSyntaxError("{% if %}");

        error.Should().NotBeNull();
        error!.Line.Should().Be(1);
        error.Column.Should().Be(6);
        error.Message.Should().Contain("if");
    }

    [TestCase("{% renderbody %}")]
    [TestCase("{%renderbody%}")]
    [TestCase("{% RenderBody %}")]
    public void A_base_layout_with_the_renderbody_slot_has_no_syntax_error(string slot)
    {
        var layout = $"<html>\n<body>{slot}</body>\n</html>";

        new LiquidTemplateParser().GetSyntaxError(layout).Should().BeNull();
    }

    [Test]
    public void An_error_after_the_renderbody_slot_keeps_its_line_and_column()
    {
        var error = new LiquidTemplateParser().GetSyntaxError("{% renderbody %} {% if %}");

        error.Should().NotBeNull();
        error!.Line.Should().Be(1);
        error.Column.Should().Be(23);
    }

    [Test]
    public void A_render_failure_names_the_template()
    {
        var act = () =>
            WebTemplateRenderer.Render(
                new ThrowingRenderer(),
                "raytha_html_base_layout",
                "{% endfor %}",
                new object()
            );

        act.Should()
            .Throw<TemplateRenderException>()
            .Which.Message.Should()
            .StartWith("raytha_html_base_layout:");
    }

    [Test]
    public void A_preview_failure_names_the_template_and_the_position()
    {
        var error = new TemplateRenderException(
            "raytha_html_base_layout",
            new InvalidOperationException("Unknown tag 'endfor' at (1:10)")
        );

        var problem = TemplateRenderProblem.Create(error, "/raytha/api/v1/webtemplates/x/render-preview");

        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Template render failed");
        problem.Detail.Should().StartWith("raytha_html_base_layout:");
        problem.Detail.Should().Contain("endfor");
        problem.Instance.Should().Be("/raytha/api/v1/webtemplates/x/render-preview");
        problem.Extensions["line"].Should().Be(1);
        problem.Extensions["column"].Should().Be(10);
    }

    [Test]
    public void Template_preview_does_not_require_a_content_type_in_the_route()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/raytha/api/v1/webtemplates/abc/render-preview";
        var service = new ContentTypeInRoutePath(new HttpContextAccessor { HttpContext = http });

        service.ValidateContentTypeInRoutePathMatchesValue("posts").Should().BeTrue();

        http.Request.RouteValues[RouteConstants.CONTENT_TYPE_DEVELOPER_NAME] = "pages";
        var mismatch = () => service.ValidateContentTypeInRoutePathMatchesValue("posts");
        mismatch.Should().Throw<UnauthorizedAccessException>();
    }

    private sealed class ThrowingRenderer : IRenderEngine
    {
        public string RenderAsHtml(string template, object entity) =>
            throw new InvalidOperationException("Unknown tag 'endfor' at (1:10)");

        public string RenderAsHtml(
            string template,
            object entity,
            Guid themeId,
            Dictionary<string, List<SitePageWidgetRenderData>>? widgets
        ) => throw new InvalidOperationException("widgets");
    }
}

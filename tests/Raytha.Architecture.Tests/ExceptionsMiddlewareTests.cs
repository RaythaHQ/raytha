using System.Net;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Exceptions;
using Raytha.Domain.Exceptions;
using Raytha.Web.Areas.Public.DbViewEngine;
using Raytha.Web.Middlewares;

namespace Raytha.Architecture.Tests;

public class ExceptionsMiddlewareTests
{
    [Test]
    public void An_unknown_filter_condition_type_is_a_400_that_names_the_valid_types()
    {
        var problem = ExceptionsMiddleware.ToProblemDetails(
            new FilterConditionTypeNotFoundException("condition"),
            "/raytha/api/v1/contenttypes/posts/views/x/filter",
            new TestHostEnvironment(Environments.Production)
        );

        problem.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Detail.Should().Contain("filter_condition");
        problem.Detail.Should().Contain("filter_condition_group");
    }

    [Test]
    public void An_invalid_filter_returns_the_reason_instead_of_a_generic_detail()
    {
        var problem = ExceptionsMiddleware.ToProblemDetails(
            new InvalidFilterException("Field 'seasons' cannot be used with this operator."),
            "/posts",
            new TestHostEnvironment(Environments.Production)
        );

        problem.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Detail.Should().Contain("seasons");
    }

    [Test]
    public void Development_public_render_failures_name_the_template()
    {
        var error = new TemplateRenderException(
            "raytha_html_base_layout",
            new InvalidOperationException("Unknown tag 'endfor' at (1:10)")
        );

        ExceptionsMiddleware
            .PublicErrorMessage(error, new TestHostEnvironment(Environments.Development))
            .Should()
            .Be("raytha_html_base_layout: Unknown tag 'endfor' at (1:10)");
        ExceptionsMiddleware
            .PublicErrorMessage(error, new TestHostEnvironment(Environments.Production))
            .Should()
            .BeNull();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Raytha";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

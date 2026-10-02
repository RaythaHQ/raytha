using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Web.Areas.Public.DbViewEngine;

namespace Raytha.Web.Areas.Api.Controllers.V1;

public static class TemplateRenderProblem
{
    public const string Title = "Template render failed";

    public static ProblemDetails Create(TemplateRenderException error, string? instance)
    {
        var located =
            LiquidSyntaxError.FromParserMessage(error.InnerException?.Message)
            ?? new LiquidSyntaxError(error.InnerException?.Message ?? error.Message, null, null);
        var problem = new ProblemDetails
        {
            Type = "https://httpstatuses.io/400",
            Title = Title,
            Status = StatusCodes.Status400BadRequest,
            Detail = $"{error.DeveloperName}: {located.Message}",
            Instance = instance,
        };
        if (located.Line is int line)
        {
            problem.Extensions["line"] = line;
        }

        if (located.Column is int column)
        {
            problem.Extensions["column"] = column;
        }

        return problem;
    }

    public static IActionResult ToResult(ProblemDetails problem)
    {
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status ?? StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }
}

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Raytha.Application.Common.Models;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Web.Services;

/// <summary>
/// Runs an HTTP-trigger function for a request and turns its result into the response. Shared by
/// /raytha/functions/execute/{developerName} and public paths routed to a function.
/// </summary>
public static class RaythaFunctionHttp
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
    };

    /// <summary>Null when the request method cannot run a function.</summary>
    public static async Task<ExecuteRaythaFunction.Command?> ReadCommand(
        HttpRequest request,
        string developerName
    )
    {
        var method =
            HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) ? "GET"
            : HttpMethods.IsPost(request.Method) ? "POST"
            : null;
        if (method is null)
        {
            return null;
        }

        string payloadJson;
        if (request.HasFormContentType)
        {
            payloadJson = System.Text.Json.JsonSerializer.Serialize(await request.ReadFormAsync());
        }
        else
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            payloadJson = await reader.ReadToEndAsync();
        }

        return new ExecuteRaythaFunction.Command
        {
            DeveloperName = developerName,
            RequestMethod = method,
            QueryJson = JsonConvert.SerializeObject(request.Query),
            PayloadJson = payloadJson,
        };
    }

    public static IActionResult MethodNotAllowed(HttpResponse response)
    {
        response.Headers.Allow = "GET, HEAD, POST";
        return new ContentResult
        {
            Content = "Functions answer GET, HEAD, and POST.",
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status405MethodNotAllowed,
        };
    }

    public static IActionResult ToActionResult(
        HttpRequest request,
        CommandResponseDto<object> response,
        IActionResult? notFound = null
    )
    {
        if (!response.Success)
        {
            return response.GetErrors().First().PropertyName switch
            {
                "IsActive" => notFound ?? new ObjectResult(response.Error) { StatusCode = 404 },
                "Functions" => new ContentResult
                {
                    Content = "Functions are disabled",
                    ContentType = "text/plain; charset=utf-8",
                    StatusCode = StatusCodes.Status403Forbidden,
                },
                "Queue of functions" => new ObjectResult(response.Error) { StatusCode = 503 },
                _ => new ObjectResult(response.Error) { StatusCode = 500 },
            };
        }

        return RaythaFunctionResponse.From(response.Result) switch
        {
            RaythaFunctionResponse.Json json => new JsonResult(json.Body, JsonOptions),
            RaythaFunctionResponse.Content content => new ContentResult
            {
                Content = content.Body,
                ContentType = content.ContentType,
                StatusCode = content.StatusCode,
            },
            RaythaFunctionResponse.Status status => new ObjectResult(status.Body)
            {
                StatusCode = status.StatusCode,
            },
            RaythaFunctionResponse.Redirect redirect => new RedirectResult(redirect.Url),
            RaythaFunctionResponse.Invalid invalid => new ObjectResult(
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Invalid function result",
                    Detail = invalid.Reason,
                    Instance = request.Path,
                }
            )
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                ContentTypes = { "application/problem+json" },
            },
            _ => throw new UnreachableException(),
        };
    }
}

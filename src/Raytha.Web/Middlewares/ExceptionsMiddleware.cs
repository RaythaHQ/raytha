using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Web.Middlewares;

public class ExceptionsMiddleware
{
    public const string ERROR_DETAILS_KEY = "Raytha.ErrorDetails";
    public const string ProblemJsonContentType = "application/problem+json";

    private static readonly JsonSerializerOptions ProblemJsonOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static bool IsApiPath(PathString path, string pathBase)
    {
        var lowerPath = path.Value?.ToLower() ?? string.Empty;
        return lowerPath.StartsWith($"{pathBase}/raytha/api");
    }

    /// <summary>
    /// Maps an exception to RFC 7807 problem details. Validation failures become
    /// <see cref="ValidationProblemDetails"/> with a per-field <c>errors</c> map. The legacy
    /// <c>success</c>/<c>error</c> members are kept as extensions so v1 API clients that
    /// read them keep working.
    /// </summary>
    public static ProblemDetails ToProblemDetails(
        Exception exception,
        string? instance,
        IHostEnvironment env
    )
    {
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException validation:
                problem = new ValidationProblemDetails(validation.Errors)
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "One or more validation errors occurred.",
                    Detail = validation.Message,
                };
                break;
            case NotFoundException:
                problem = Create(
                    HttpStatusCode.NotFound,
                    "Not found",
                    "The resource you requested was not found."
                );
                break;
            case FormatException:
                problem = Create(
                    HttpStatusCode.UnprocessableEntity,
                    "Invalid identifier",
                    "Invalid format of identifier."
                );
                break;
            case InvalidApiKeyException:
                // Security: never distinguish missing from invalid keys.
                problem = Create(HttpStatusCode.Unauthorized, "Unauthorized", "Invalid API key.");
                break;
            case ForbiddenAccessException:
                problem = Create(
                    HttpStatusCode.Forbidden,
                    "Forbidden",
                    "You do not have permission to perform this action."
                );
                break;
            case UnauthorizedAccessException:
                problem = Create(HttpStatusCode.Unauthorized, "Unauthorized", "Unauthorized access.");
                break;
            case BusinessException business:
                problem = Create(HttpStatusCode.BadRequest, "Request failed", business.Message);
                break;
            case BadHttpRequestException bad:
                problem = Create(
                    (HttpStatusCode)bad.StatusCode,
                    "Bad request",
                    env.IsDevelopment() ? bad.Message : "The request body could not be read."
                );
                break;
            default:
                problem = Create(
                    HttpStatusCode.InternalServerError,
                    "Server error",
                    env.IsDevelopment() ? exception.Message : "An unknown error has occurred."
                );
                if (env.IsDevelopment() && exception.StackTrace is not null)
                {
                    problem.Extensions["stackTrace"] = exception.StackTrace;
                }
                break;
        }

        problem.Type ??= $"https://httpstatuses.io/{problem.Status}";
        problem.Instance = instance;
        problem.Extensions["success"] = false;
        problem.Extensions["error"] = problem.Detail;
        return problem;
    }

    private static ProblemDetails Create(HttpStatusCode status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = detail,
        };
    }

    public static RequestDelegate ErrorHandlerDelegate(string pathBase, IWebHostEnvironment env)
    {
        return async (HttpContext context) =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>();
            if (error == null || error.Error == null)
            {
                return;
            }

            var path = context.Request.Path;

            if (IsApiPath(path, pathBase))
            {
                // Every /raytha/api/* path answers with RFC 7807 problem details so the admin SPA
                // and API clients share one machine-readable error shape. Public/Razor paths keep
                // the HTML flow below.
                var problem = ToProblemDetails(error.Error, path.Value, env);
                context.Response.StatusCode =
                    problem.Status ?? (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = ProblemJsonContentType;
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(problem, problem.GetType(), ProblemJsonOptions)
                );
                await context.Response.CompleteAsync();
            }
            else
            {
                // Store error details for retrieval by error handler
                int statusCode;

                if (error.Error is NotFoundException)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                }
                else if (error.Error is FormatException)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                }
                else if (error.Error is UnauthorizedAccessException)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                }
                else
                {
                    statusCode = (int)HttpStatusCode.InternalServerError;
                }

                var errorDetails = new ErrorDetails
                {
                    Route = path.Value ?? string.Empty,
                    Exception = error.Error,
                    ErrorMessage = error.Error?.Message ?? "An unknown error has occurred",
                    StackTrace = error.Error?.StackTrace,
                    IsDevelopmentMode = env.IsDevelopment(),
                    StatusCode = statusCode,
                };

                context.Items[ERROR_DETAILS_KEY] = errorDetails;
                if (
                    path.Value?.ToLower().StartsWith($"{pathBase}/raytha") == true
                    && !path.Value.ToLower().StartsWith($"{pathBase}/raytha/api")
                    && !path.Value.ToLower().StartsWith($"{pathBase}/raytha/error")
                )
                {
                    // Store error details in TempData factory for redirect (HttpContext.Items doesn't survive redirects)
                    var tempDataProvider =
                        context.RequestServices.GetService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>();
                    var tempDataDictionaryFactory =
                        context.RequestServices.GetService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();

                    if (tempDataProvider != null && tempDataDictionaryFactory != null)
                    {
                        var tempData = tempDataDictionaryFactory.GetTempData(context);
                        tempData[$"{ERROR_DETAILS_KEY}_Message"] = errorDetails.ErrorMessage;
                        tempData[$"{ERROR_DETAILS_KEY}_StackTrace"] =
                            errorDetails.StackTrace ?? string.Empty;
                        tempData[$"{ERROR_DETAILS_KEY}_IsDevelopment"] =
                            errorDetails.IsDevelopmentMode;
                        tempData[$"{ERROR_DETAILS_KEY}_Route"] = errorDetails.Route;
                        tempData.Save();
                    }

                    context.Response.Redirect($"{pathBase}/raytha/error/{statusCode}");
                }
                else
                {
                    context.Response.StatusCode = statusCode;
                }
            }
        };
    }

    public class ErrorDetails
    {
        public string Route { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public bool IsDevelopmentMode { get; set; }
        public int StatusCode { get; set; }
    }
}

using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Web.Middlewares;

public class ExceptionsMiddleware
{
    public const string ERROR_DETAILS_KEY = "Raytha.ErrorDetails";

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

            if (path.Value?.ToLower().StartsWith($"{pathBase}/raytha/api") == true)
            {
                byte[] errorBytes;
                if (error.Error is NotFoundException)
                {
                    errorBytes = GetErrorMessageAsByteArray(
                        "The resource you requested was not found."
                    );
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                }
                else if (error.Error is FormatException)
                {
                    errorBytes = GetErrorMessageAsByteArray("Invalid format of identifier.");
                    context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                }
                else if (error.Error is UnauthorizedAccessException)
                {
                    errorBytes = GetErrorMessageAsByteArray("Unauthorized access.");
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
                else if (error.Error is InvalidApiKeyException)
                {
                    // Security: Avoid leaking detailed API key validation reasons and return a proper 401,
                    // so callers cannot distinguish between missing/invalid keys while still receiving a clear,
                    // generic error message that does not expose internal implementation details.
                    errorBytes = GetErrorMessageAsByteArray("Invalid API key.");
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
                else
                {
                    errorBytes = GetErrorMessageAsByteArray("An unknown error has occured.");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }

                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
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

    private static byte[] GetErrorMessageAsByteArray(string message)
    {
        string json = JsonSerializer.Serialize(new { success = false, error = message });
        return Encoding.UTF8.GetBytes(json);
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

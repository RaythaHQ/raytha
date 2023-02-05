using Microsoft.AspNetCore.Builder;
using System.Text;
using System;
using Raytha.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Raytha.Web.Middlewares;

public class ExceptionsMiddleware
{
    public static Action<IApplicationBuilder> ErrorHandler()
    {
        return errorApp =>
        {
            errorApp.Run(async context =>
            {
                var error = context.Features.Get<IExceptionHandlerFeature>();
                var path = context.Request.Path;
                byte[] errorBytes;
                if (path.Value.ToLower().StartsWith("/raytha/api"))
                {
                    if (error.Error is NotFoundException)
                    {
                        errorBytes = GetErrorMessageAsByteArray("The resource you requested was not found.");
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
                        errorBytes = GetErrorMessageAsByteArray($"Api key error: {error.Error.Message}");
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
                    if (error.Error is NotFoundException)
                    {
                        context.Response.Redirect("/raytha/404");
                    }
                    else if (error.Error is FormatException)
                    {
                        context.Response.Redirect("/raytha/404");
                    }
                    else if (error.Error is UnauthorizedAccessException)
                    {
                        context.Response.Redirect("/raytha/403");
                    }
                    else
                    {
                        context.Response.Redirect("/raytha/500");
                    }
                }
            });
        };
    }

    private static byte[] GetErrorMessageAsByteArray(string message)
    {
        string json = JsonSerializer.Serialize(new { success = false, error = message });
        return Encoding.UTF8.GetBytes(json);
    }
}

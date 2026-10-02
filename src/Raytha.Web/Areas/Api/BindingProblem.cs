using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Raytha.Web.Areas.Api;

/// <summary>
/// The 400 an [ApiController] answers when the request does not bind. When the JSON body is rejected,
/// MVC reports both the real cause (<c>$.filter[0].id</c> is not a GUID) and a second "The request field
/// is required." for the parameter that stayed null. Clients that print the first error show only the
/// second, so drop it and put the real cause in <c>detail</c>.
/// </summary>
public static partial class BindingProblem
{
    private const string JsonPathPrefix = "$.";

    public static ValidationProblemDetails Create(ModelStateDictionary modelState, string? instance)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
                continue;

            var path = key.StartsWith(JsonPathPrefix, StringComparison.Ordinal) ? key[JsonPathPrefix.Length..] : key;
            errors[key] = entry.Errors.Select(e => Clean(path, e.ErrorMessage)).ToArray();
        }

        if (errors.Keys.Any(k => k.StartsWith(JsonPathPrefix, StringComparison.Ordinal)))
        {
            foreach (var key in errors.Keys.ToList())
            {
                if (
                    !key.StartsWith(JsonPathPrefix, StringComparison.Ordinal)
                    && errors[key].All(m => RequiredField().IsMatch(m))
                )
                    errors.Remove(key);
            }
        }

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = errors.Values.SelectMany(v => v).FirstOrDefault(),
            Instance = instance,
        };
    }

    private static string Clean(string path, string message)
    {
        if (!message.StartsWith("The JSON value could not be converted to ", StringComparison.Ordinal))
            return message;

        var pathMarker = message.IndexOf(". Path: ", StringComparison.Ordinal);
        var reason = pathMarker < 0 ? message : message[..(pathMarker + 1)];
        return reason.Contains("System.Guid", StringComparison.Ordinal)
            ? $"{path} must be a GUID."
            : $"{path}: {reason}";
    }

    [GeneratedRegex(@"^The \w+ field is required\.$")]
    private static partial Regex RequiredField();
}

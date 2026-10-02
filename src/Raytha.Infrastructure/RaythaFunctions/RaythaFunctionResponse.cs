using System.Net.Http.Headers;
using System.Text.Json;

namespace Raytha.Infrastructure.RaythaFunctions;

/// <summary>
/// The HTTP response an HTTP-trigger function asked for, parsed once from what its script returned.
/// </summary>
public abstract record RaythaFunctionResponse
{
    private RaythaFunctionResponse() { }

    public sealed record Json(object? Body) : RaythaFunctionResponse;

    public sealed record Content(string Body, string ContentType, int StatusCode) : RaythaFunctionResponse;

    public sealed record Status(int StatusCode, object? Body) : RaythaFunctionResponse;

    public sealed record Redirect(string Url) : RaythaFunctionResponse;

    public sealed record Invalid(string Reason) : RaythaFunctionResponse;

    private const string ResultHelpers = "JsonResult, TextResult, or ContentResult";

    private static readonly Dictionary<string, Func<RaythaFunctionResult, RaythaFunctionResponse>> ByContentType =
        new(StringComparer.Ordinal)
        {
            ["application/json"] = result => new Json(result.body),
            ["text/html"] = result => new Content(BodyText(result.body), "text/html", 200),
            ["application/xml"] = result => new Content(BodyText(result.body), "application/xml", 200),
            ["text/xml"] = result => new Content(BodyText(result.body), "text/xml", 200),
            ["redirectToUrl"] = result => FromRedirect(BodyText(result.body)),
            ["statusCode"] = result =>
                InvalidStatus(result.statusCode) ?? new Status(result.statusCode, result.body),
        };

    public static RaythaFunctionResponse From(object? scriptResult) =>
        scriptResult switch
        {
            RaythaFunctionResult { kind: "content" } result => FromContent(result),
            RaythaFunctionResult result when ByContentType.TryGetValue(result.contentType ?? "", out var map) =>
                map(result),
            RaythaFunctionResult result => new Invalid(
                $"The function returned an unknown content type \"{result.contentType}\". Return ContentResult(body, contentType) for a custom content type."
            ),
            null => new Invalid($"The function returned nothing. Return a result such as {ResultHelpers}."),
            _ => new Invalid($"The function returned a bare value. Wrap it in a result such as {ResultHelpers}."),
        };

    private static RaythaFunctionResponse FromContent(RaythaFunctionResult result)
    {
        var contentType = result.contentType ?? "";
        if (contentType.IndexOfAny(['\r', '\n']) >= 0 || !MediaTypeHeaderValue.TryParse(contentType, out _))
        {
            return new Invalid($"The function returned an invalid content type \"{contentType}\".");
        }
        return InvalidStatus(result.statusCode)
            ?? new Content(BodyText(result.body), contentType, result.statusCode);
    }

    private static RaythaFunctionResponse FromRedirect(string url) =>
        string.IsNullOrWhiteSpace(url) || url.IndexOfAny(['\r', '\n']) >= 0
            ? new Invalid($"The function returned an invalid redirect URL \"{url}\".")
            : new Redirect(url);

    private static RaythaFunctionResponse? InvalidStatus(int statusCode) =>
        statusCode is >= 200 and <= 599
            ? null
            : new Invalid($"The function returned status code {statusCode}. Use a status code from 200 to 599.");

    private static string BodyText(object? body) =>
        body switch
        {
            null => "",
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString() ?? "",
            JsonElement element => element.GetRawText(),
            _ => body.ToString() ?? "",
        };
}

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Areas.Api.Controllers.V1;

/// <summary>
/// Stored media URLs are root-relative, and a headless client has no page to resolve them
/// against, so v1 responses carry them resolved against the site root from WebsiteUrl. The path
/// base is already part of the stored path. Nothing stored changes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed partial class AbsoluteMediaUrlsAttribute : Attribute, IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next
    )
    {
        var services = context.HttpContext.RequestServices;
        var siteRoot = services.GetRequiredService<IRelativeUrlBuilder>().GetSiteRoot();
        if (context.Result is ObjectResult { Value: { } value } result && siteRoot.Length > 0)
        {
            var options = services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            var node = JsonSerializer.SerializeToNode(value, value.GetType(), options);
            Absolutize(node, siteRoot);
            result.Value = node;
            result.DeclaredType = typeof(JsonNode);
        }
        await next();
    }

    public static string Absolutize(string text, string origin) =>
        RootRelativeMediaUrl().Replace(text, match => origin + match.Value);

    public static void Absolutize(JsonNode? node, string origin)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (TryAbsolutize(obj[key], origin, out var text))
                        obj[key] = text;
                    else
                        Absolutize(obj[key], origin);
                }
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (TryAbsolutize(array[i], origin, out var text))
                        array[i] = text;
                    else
                        Absolutize(array[i], origin);
                }
                break;
        }
    }

    private static bool TryAbsolutize(JsonNode? node, string origin, out string result)
    {
        result = string.Empty;
        if (node is not JsonValue value || !value.TryGetValue(out string? text))
            return false;
        result = Absolutize(text, origin);
        return result != text;
    }

    // A path only counts as root-relative where a URL can start: the start of the value, after a
    // quote (plain, JSON-escaped, or HTML-encoded), an opening parenthesis, '=', ',', '>' or
    // whitespace. After a host or a second slash it is already absolute or protocol-relative.
    [GeneratedRegex(
        """(?<=^|[\s"'(=,>]|\\u0022|\\u0027|&quot;|&#39;)(?:/[A-Za-z0-9._~-]+)*/(?:raytha/media-items/(?:objectkey|id)|_static-files)/[A-Za-z0-9._~-]+"""
    )]
    private static partial Regex RootRelativeMediaUrl();
}

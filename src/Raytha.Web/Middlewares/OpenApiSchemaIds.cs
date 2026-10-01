using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace Raytha.Web.Middlewares;

/// <summary>
/// Schema ids in the OpenAPI document. Nested request types are all named
/// <c>Command</c> or <c>Query</c>, and the default id is that short name, so every
/// body collapsed onto one schema. The id here keeps the namespace and the declaring type.
/// </summary>
public static class OpenApiSchemaIds
{
    public static string? Create(JsonTypeInfo jsonTypeInfo)
    {
        var id = OpenApiOptions.CreateDefaultSchemaReferenceId(jsonTypeInfo);
        if (id is null)
            return null;

        var type = Nullable.GetUnderlyingType(jsonTypeInfo.Type) ?? jsonTypeInfo.Type;
        if (!type.IsNested || type.DeclaringType is null || type.Name.Contains('<'))
            return id;

        var owners = new Stack<string>();
        for (var current = type.DeclaringType; current is not null; current = current.DeclaringType)
            owners.Push(current.Name);

        return $"{Scope(type.DeclaringType)}.{string.Concat(owners)}{id}";
    }

    private static string Scope(Type declaringType)
    {
        var ns = declaringType.Namespace ?? "";
        foreach (var root in new[] { "Raytha.Application.", "Raytha.Domain.", "Raytha.Infrastructure.", "Raytha.Web." })
        {
            if (ns.StartsWith(root, StringComparison.Ordinal))
                return ns[root.Length..];
        }

        return ns.Length == 0 ? declaringType.Name : ns;
    }
}

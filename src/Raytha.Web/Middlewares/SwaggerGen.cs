using System.Linq;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Raytha.Application.Common.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Raytha.Web.Middlewares;

public class LowercaseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = swaggerDoc.Paths.ToDictionary(
            entry => LowercaseEverythingButParameters(entry.Key),
            entry => entry.Value
        );
        swaggerDoc.Paths = new OpenApiPaths();
        foreach (var (key, value) in paths)
        {
            swaggerDoc.Paths.Add(key, value);
        }
    }

    private static string LowercaseEverythingButParameters(string key) =>
        string.Join('/', key.Split('/').Select(x => x.Contains("{") ? x : x.ToLower()));
}

public class ExcludePropertyFromOpenApiDocsFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties == null)
            return;

        var excludedProperties = context
            .Type.GetProperties()
            .Where(t => t.GetCustomAttribute<ExcludePropertyFromOpenApiDocs>() != null);

        foreach (var excludedProperty in excludedProperties)
        {
            var propertyToRemove = schema.Properties.Keys.SingleOrDefault(x =>
                x.ToLower() == excludedProperty.Name.ToLower()
            );

            if (propertyToRemove != null)
            {
                schema.Properties.Remove(propertyToRemove);
            }
        }
    }
}

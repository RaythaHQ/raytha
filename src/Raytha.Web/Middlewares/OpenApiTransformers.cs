using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

internal sealed class ApiKeySecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["ApiKey"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Scheme = "X-API-KEY", // "bearer" refers to the header name here
                In = ParameterLocation.Header,
                BearerFormat = "GUID",
                Name = "X-API-KEY",
            },
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = securitySchemes;

        // Apply it as a requirement for all operations
        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
        {
            operation.Value.Security ??= [];
            operation.Value.Security.Add(
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("ApiKey", document)] = [],
                }
            );
        }
    }
}

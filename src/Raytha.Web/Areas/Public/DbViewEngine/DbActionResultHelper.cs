using System.Collections.Generic;
using Mediator;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Areas.Public.DbViewEngine;

internal static class DbActionResultHelper
{
    public static DbActionServiceBundle ResolveServices(
        HttpContext httpContext,
        bool includeMediator = false,
        bool includeAntiforgery = true
    )
    {
        var services = httpContext.RequestServices;

        return new DbActionServiceBundle(
            services.GetRequiredService<IRenderEngine>(),
            services.GetRequiredService<ICurrentOrganization>(),
            services.GetRequiredService<ICurrentUser>(),
            includeMediator ? services.GetRequiredService<IMediator>() : null,
            includeAntiforgery ? services.GetRequiredService<IAntiforgery>() : null
        );
    }

    public static Dictionary<string, string> ToQueryDictionary(IQueryCollection query)
    {
        var dict = new Dictionary<string, string>();

        foreach (var key in query.Keys)
        {
            query.TryGetValue(key, out StringValues value);
            dict[key] = value!;
        }

        return dict;
    }
}

internal sealed record DbActionServiceBundle(
    IRenderEngine Renderer,
    ICurrentOrganization CurrentOrganization,
    ICurrentUser CurrentUser,
    IMediator? Mediator,
    IAntiforgery? Antiforgery
);


using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Application.Themes.WebTemplates;

namespace Raytha.Web.Areas.Public.DbViewEngine;

public static class WebTemplatePreview
{
    public static string Render(
        HttpContext httpContext,
        WebTemplateDto template,
        object? target,
        ContentType_RenderModel? contentType
    )
    {
        var services = DbActionResultHelper.ResolveServices(httpContext);
        var source = WebTemplateExtensions.ContentAssembledFromParents(
            template.Content,
            template.ParentTemplate
        );
        // A layout's {% renderbody %} is the child slot. An empty preview has no child to insert.
        if (WebTemplateExtensions.HasRenderBodyTag(source))
        {
            source = Regex.Replace(
                source,
                WebTemplateExtensions.RENDERBODY_REGEX,
                string.Empty,
                RegexOptions.IgnoreCase
            );
        }

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                services.CurrentOrganization
            ),
            CurrentUser = CurrentUser_RenderModel.GetProjection(services.CurrentUser),
            ContentType = contentType,
            Target = target,
            QueryParams = DbActionResultHelper.ToQueryDictionary(httpContext.Request.Query),
            RequestVerificationToken = services
                .Antiforgery?.GetAndStoreTokens(httpContext)
                .RequestToken,
            PathBase = services.CurrentOrganization.PathBase,
        };

        return WebTemplateRenderer.Render(
            services.Renderer,
            template.DeveloperName,
            source,
            renderModel
        );
    }
}

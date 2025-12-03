using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Application.Themes.WebTemplates;

namespace Raytha.Web.Areas.Public.DbViewEngine;

public class ContentItemActionViewResult : IActionResult
{
    private readonly WebTemplateDto _webTemplate;
    private readonly object _target;
    private readonly ContentType_RenderModel _contentType;
    private readonly ViewDataDictionary _viewDictionary;

    public ContentItemActionViewResult(
        WebTemplateDto webTemplate,
        object target,
        ContentType_RenderModel contentType,
        ViewDataDictionary viewDictionary
    )
    {
        _webTemplate = webTemplate;
        _target = target;
        _contentType = contentType;
        _viewDictionary = viewDictionary;
    }

    public string ContentType { get; set; } = "text/html";

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var services = DbActionResultHelper.ResolveServices(httpContext);
        var antiforgeryToken = services.Antiforgery
            ?.GetAndStoreTokens(httpContext)
            .RequestToken;

        httpContext.Response.StatusCode = 200;
        httpContext.Response.ContentType = ContentType;

        var sourceWithParents = WebTemplateExtensions.ContentAssembledFromParents(
            _webTemplate.Content,
            _webTemplate.ParentTemplate
        );

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(services.CurrentOrganization),
            CurrentUser = CurrentUser_RenderModel.GetProjection(services.CurrentUser),
            ContentType = _contentType,
            Target = _target,
            QueryParams = DbActionResultHelper.ToQueryDictionary(httpContext.Request.Query),
            RequestVerificationToken = antiforgeryToken,
            ViewData = _viewDictionary,
            PathBase = services.CurrentOrganization.PathBase,
        };

        await using (var sw = new StreamWriter(httpContext.Response.Body))
        {
            var body = services.Renderer.RenderAsHtml(sourceWithParents, renderModel);
            await sw.WriteAsync(body);
        }
    }
}

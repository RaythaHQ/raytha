using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.ContentTypes;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Raytha.Application.Common.Utils;
using Raytha.Application.Themes.WebTemplates;

namespace Raytha.Web.Areas.Public.DbViewEngine;

public class ContentItemActionViewResult : IActionResult
{
    private readonly WebTemplateDto _webTemplate;
    private readonly object _target;
    private readonly ContentType_RenderModel _contentType;
    private readonly ViewDataDictionary _viewDictionary;

    public ContentItemActionViewResult(WebTemplateDto webTemplate, object target, ContentType_RenderModel contentType, ViewDataDictionary viewDictionary)
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
        var renderer = httpContext.RequestServices.GetRequiredService<IRenderEngine>();
        var currentOrg = httpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();

        httpContext.Response.StatusCode = 200;
        httpContext.Response.ContentType = ContentType;

        var sourceWithParents = WebTemplateExtensions.ContentAssembledFromParents(_webTemplate.Content, _webTemplate.ParentTemplate);

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(currentOrg),
            CurrentUser = CurrentUser_RenderModel.GetProjection(currentUser),
            ContentType = _contentType,
            Target = _target,
            QueryParams = QueryCollectionToDictionary(httpContext.Request.Query),
            RequestVerificationToken = antiforgery.GetAndStoreTokens(httpContext).RequestToken,
            ViewData = _viewDictionary,
            PathBase = currentOrg.PathBase
        };

        await using (var sw = new StreamWriter(httpContext.Response.Body))
        {
            var body = renderer.RenderAsHtml(sourceWithParents, renderModel);
            await sw.WriteAsync(body);
        }
    }

    Dictionary<string, string> QueryCollectionToDictionary(IQueryCollection query)
    {
        var dict = new Dictionary<string, string>();
        foreach (var key in query.Keys)
        {
            StringValues value = string.Empty;
            query.TryGetValue(key, out @value);
            dict.Add(key, @value);
        }
        return dict;
    }
}
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Common.Utils;

namespace Raytha.Web.Areas.Public.DbViewEngine;

public class AccountActionViewResult : IActionResult
{
    private readonly string _view;
    private readonly object _target;
    private readonly ViewDataDictionary _viewDictionary;

    public AccountActionViewResult(string view, object target, ViewDataDictionary viewDictionary)
    {
        _view = view;
        _target = target;
        _viewDictionary = viewDictionary;
    }

    public string ContentType { get; set; } = "text/html";

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var renderer = httpContext.RequestServices.GetRequiredService<IRenderEngine>();
        var currentOrg = httpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();
        var mediator = httpContext.RequestServices.GetRequiredService<IMediator>();

        httpContext.Response.StatusCode = 200;
        httpContext.Response.ContentType = ContentType;

        var template = await mediator.Send(new GetWebTemplateByDeveloperName.Query { 
            DeveloperName = _view,
            ThemeId = currentOrg.ActiveThemeId
        });

        var source = template.Result.Content;
        var sourceWithParents = WebTemplateExtensions.ContentAssembledFromParents(source, template.Result.ParentTemplate);

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(currentOrg),
            CurrentUser = CurrentUser_RenderModel.GetProjection(currentUser),
            Target = _target,
            ViewData = _viewDictionary,
            QueryParams = QueryCollectionToDictionary(httpContext.Request.Query),
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
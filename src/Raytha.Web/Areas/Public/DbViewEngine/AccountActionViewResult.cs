using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Common.Utils;
using Raytha.Application.Themes.WebTemplates.Queries;

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
        var services = DbActionResultHelper.ResolveServices(httpContext, includeMediator: true, includeAntiforgery: false);
        var mediator = services.Mediator ?? throw new InvalidOperationException("Mediator is required.");

        httpContext.Response.StatusCode = 200;
        httpContext.Response.ContentType = ContentType;

        var template = await mediator.Send(
            new GetWebTemplateByDeveloperName.Query
            {
                DeveloperName = _view,
                ThemeId = services.CurrentOrganization.ActiveThemeId,
            }
        );

        var source = template.Result.Content;
        var sourceWithParents = WebTemplateExtensions.ContentAssembledFromParents(
            source,
            template.Result.ParentTemplate
        );

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(services.CurrentOrganization),
            CurrentUser = CurrentUser_RenderModel.GetProjection(services.CurrentUser),
            Target = _target,
            ViewData = _viewDictionary,
            QueryParams = DbActionResultHelper.ToQueryDictionary(httpContext.Request.Query),
            PathBase = services.CurrentOrganization.PathBase,
        };

        await using (var sw = new StreamWriter(httpContext.Response.Body))
        {
            var body = services.Renderer.RenderAsHtml(sourceWithParents, renderModel);
            await sw.WriteAsync(body);
        }
    }
}

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

public class ErrorActionViewResult : IActionResult
{
    private readonly string _view;
    private readonly object _target;
    private readonly int _httpStatusCode;
    private readonly ViewDataDictionary _viewDictionary;

    public ErrorActionViewResult(
        string view,
        int httpStatusCode,
        object target,
        ViewDataDictionary viewDictionary
    )
    {
        _view = view;
        _target = target;
        _httpStatusCode = httpStatusCode;
        _viewDictionary = viewDictionary;
    }

    public string ContentType { get; set; } = "text/html";

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var services = DbActionResultHelper.ResolveServices(
            httpContext,
            includeMediator: true
        );
        var mediator = services.Mediator ?? throw new InvalidOperationException("Mediator is required.");
        var antiforgeryToken = services.Antiforgery
            ?.GetAndStoreTokens(httpContext)
            .RequestToken;

        httpContext.Response.StatusCode = _httpStatusCode;
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
            RequestVerificationToken = antiforgeryToken,
            QueryParams = DbActionResultHelper.ToQueryDictionary(httpContext.Request.Query),
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

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.Templates.Web;
using Raytha.Application.Templates.Web.Queries;
using System.IO;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Public.DbViewEngine;

public class AccountActionViewResult : IActionResult
{
    private readonly string _view;
    private readonly object _target;

    public AccountActionViewResult(string view, object target)
    {
        _view = view;
        _target = target;
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

        var template = await mediator.Send(new GetWebTemplateByName.Query { DeveloperName = _view });
        var source = template.Result.Content;
        var sourceWithParents = WebTemplateExtensions.ContentAssembledFromParents(source, template.Result.ParentTemplate);

        var renderModel = new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(currentOrg),
            CurrentUser = CurrentUser_RenderModel.GetProjection(currentUser),
            Target = _target
        };

        await using (var sw = new StreamWriter(httpContext.Response.Body))
        {
            var body = renderer.RenderAsHtml(sourceWithParents, renderModel);
            await sw.WriteAsync(body);
        }
    }
}
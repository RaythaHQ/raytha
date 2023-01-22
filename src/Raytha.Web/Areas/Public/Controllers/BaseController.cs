using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Areas.Public.Controllers;

[Area("Public")]
[ApiExplorerSettings(IgnoreApi = true)]
public class BaseController : Controller
{
    private ISender _mediator;
    private ICurrentOrganization _currentOrganization;
    private ICurrentUser _currentUser;
    private IAntiforgery _antiforgery;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICurrentOrganization CurrentOrganization => _currentOrganization ??= HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
    protected ICurrentUser CurrentUser => _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    protected IAntiforgery Antiforgery => _antiforgery ??= HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!CurrentOrganization.InitialSetupComplete)
        {
            context.Result = new RedirectToActionResult("Index", "Setup", new { area = "Admin" });
        }
    }
}

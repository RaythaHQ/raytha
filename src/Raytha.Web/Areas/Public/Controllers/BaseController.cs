using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Mediator;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Web.Areas.Admin.Pages.Shared;

namespace Raytha.Web.Areas.Public.Controllers;

[Area("Public")]
[ApiExplorerSettings(IgnoreApi = true)]
public class BaseController : Controller
{
    private ISender _mediator;
    private ICurrentOrganization _currentOrganization;
    private ICurrentUser _currentUser;
    private IAntiforgery _antiforgery;

    protected ISender Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICurrentOrganization CurrentOrganization =>
        _currentOrganization ??=
            HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
    protected ICurrentUser CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    protected IAntiforgery Antiforgery =>
        _antiforgery ??= HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

    public const string ErrorMessageKey = "ErrorMessage";
    public const string SuccessMessageKey = "SuccessMessage";
    public const string WarningMessageKey = "WarningMessage";

    protected void CheckIfErrorOrSuccessMessageExist()
    {
        if (TempData[ErrorMessageKey] != null)
        {
            ViewData[ErrorMessageKey] = TempData[ErrorMessageKey];
        }

        if (TempData[SuccessMessageKey] != null)
        {
            ViewData[SuccessMessageKey] = TempData[SuccessMessageKey];
        }
    }

    protected void SetErrorMessage(string message)
    {
        ViewData[ErrorMessageKey] = message;
        TempData[ErrorMessageKey] = message;
    }

    protected void SetErrorMessage(
        string message,
        IEnumerable<ValidationFailure> errors,
        int statusCode = 303
    )
    {
        var validationSummary = errors.FirstOrDefault(p =>
            p.PropertyName == Constants.VALIDATION_SUMMARY
        );
        if (validationSummary != null)
        {
            SetErrorMessage(validationSummary.ErrorMessage);
        }
        else
        {
            SetErrorMessage(message);
        }

        ViewData["ValidationErrors"] = errors;
        this.HttpContext.Response.StatusCode = statusCode;
    }

    protected void SetSuccessMessage(string message)
    {
        ViewData[SuccessMessageKey] = message;
        TempData[SuccessMessageKey] = message;
    }

    protected void SetWarningMessage(string message)
    {
        ViewData[WarningMessageKey] = message;
        TempData[WarningMessageKey] = message;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (CurrentOrganization.RedirectWebsite.IsValidUriFormat())
        {
            context.Result = new RedirectResult(CurrentOrganization.RedirectWebsite);
            return;
        }
        if (!CurrentOrganization.InitialSetupComplete)
        {
            context.Result = new RedirectToPageResult(
                RouteNames.Setup.Index,
                new { area = "Admin" }
            );
        }
    }
}

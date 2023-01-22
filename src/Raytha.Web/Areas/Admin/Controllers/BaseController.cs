using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Web.Filters;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[ApiExplorerSettings(IgnoreApi = true)]
[ServiceFilter(typeof(SetFormValidationErrorsFilterAttribute))]
public class BaseController : Controller
{
    public const string ErrorMessageKey = "ErrorMessage";
    public const string SuccessMessageKey = "SuccessMessage";
    public const string WarningMessageKey = "WarningMessage";
    public const string RAYTHA_ROUTE_PREFIX = "raytha";

    private ISender _mediator;
    private ICurrentOrganization _currentOrganization;
    private ICurrentUser _currentUser;
    private IFileStorageProviderSettings _fileStorageSettings;
    private IFileStorageProvider _fileStorageProvider;
    private IEmailer _emailer;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected ICurrentOrganization CurrentOrganization => _currentOrganization ??= HttpContext.RequestServices.GetRequiredService<ICurrentOrganization>();
    protected ICurrentUser CurrentUser => _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
    protected IFileStorageProvider FileStorageProvider => _fileStorageProvider ??= HttpContext.RequestServices.GetRequiredService<IFileStorageProvider>();
    protected IFileStorageProviderSettings FileStorageProviderSettings => _fileStorageSettings ??= HttpContext.RequestServices.GetRequiredService<IFileStorageProviderSettings>();
    protected IEmailer Emailer => _emailer ??= HttpContext.RequestServices.GetRequiredService<IEmailer>();

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

    protected void SetErrorMessage(string message, IEnumerable<ValidationFailure> errors, int statusCode = 303)
    {
        var validationSummary = errors.FirstOrDefault(p => p.PropertyName == Constants.VALIDATION_SUMMARY);
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

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        CheckIfErrorOrSuccessMessageExist();
        base.OnActionExecuted(context);
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!CurrentOrganization.InitialSetupComplete)
        {
            context.Result = new RedirectToActionResult("Index", "Setup", null);
        }
    }

    /// <summary>
    /// Get the cookie
    /// </summary>  
    /// <param name="key">Key </param>  
    /// <returns>string value</returns>  
    public string GetCookie(string key)
    {
        return Request.Cookies[key];
    }

    /// <summary>  
    /// Set the cookie  
    /// </summary>  
    /// <param name="key">key (unique identifier)</param>  
    /// <param name="value">value to store in cookie object</param>  
    /// <param name="expireTime">expiration time</param>  
    public void AddToCookies(string key, string value, int? expireTime = null)
    {
        var option = new CookieOptions
        {
            Expires = expireTime.HasValue ? DateTime.Now.AddMinutes(expireTime.Value) : DateTime.Now.AddHours(10)
        };

        Response.Cookies.Append(key, value, option);
    }

    /// <summary>  
    /// Delete the key  
    /// </summary>  
    /// <param name="key">Key</param>  
    public void RemoveCookie(string key)
    {
        Response.Cookies.Delete(key);
    }
}
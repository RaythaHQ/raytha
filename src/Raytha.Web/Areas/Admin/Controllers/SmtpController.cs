using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Application.OrganizationSettings.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Views.Smtp;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class SmtpController : BaseController
{
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/smtp", Name = "smtpindex")]
    public async Task<IActionResult> Index()
    {
        var input = new GetOrganizationSettings.Query();
        var response = await Mediator.Send(input);
        var isMissingSmtpEnvVars = Emailer.IsMissingSmtpEnvVars();
        var viewModel = new Smtp_ViewModel
        {
            SmtpOverrideSystem = isMissingSmtpEnvVars ? true : response.Result.SmtpOverrideSystem,
            SmtpHost = response.Result.SmtpHost,
            SmtpPort = response.Result.SmtpPort,
            SmtpUsername = response.Result.SmtpUsername,
            SmtpPassword = response.Result.SmtpPassword,
            MissingSmtpEnvironmentVariables = isMissingSmtpEnvVars
        };
        
        if (isMissingSmtpEnvVars)
            SetWarningMessage("The server administrator has not set SMTP environment variables (SMTP_HOST, SMTP_PORT, SMTP_USERNAME, SMTP_PASSWORD) on this host. Therefore you must specify SMTP server details below.");
        
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/smtp", Name = "smtpindex")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Smtp_ViewModel model)
    {
        var input = new EditSmtp.Command
        {
            SmtpOverrideSystem = model.SmtpOverrideSystem,
            SmtpHost = model.SmtpHost,
            SmtpPort = model.SmtpPort,
            SmtpUsername = model.SmtpUsername,
            SmtpPassword = model.SmtpPassword
        };

        var response = await Mediator.Send(input);
        
        if (response.Success)
        {
            SetSuccessMessage("SMTP has been updated successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error attempting to edit the configuration. See the error below.");
            this.HttpContext.Response.StatusCode = 303;
            return View(model);
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "SMTP";
        ViewData["ExpandSettingsMenu"] = true;
    }
}

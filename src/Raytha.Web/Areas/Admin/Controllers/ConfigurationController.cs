using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Application.OrganizationSettings.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Views.Configuration;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class ConfigurationController : BaseController
{
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/configuration", Name = "configurationindex")]
    public async Task<IActionResult> Index()
    {
        var input = new GetOrganizationSettings.Query();
        var response = await Mediator.Send(input);
        var viewModel = new Configuration_ViewModel
        {
            OrganizationName = response.Result.OrganizationName,
            WebsiteUrl = response.Result.WebsiteUrl,
            DateFormat = response.Result.DateFormat,
            TimeZone = response.Result.TimeZone,
            SmtpDefaultFromAddress = response.Result.SmtpDefaultFromAddress,
            SmtpDefaultFromName = response.Result.SmtpDefaultFromName
        };
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/configuration", Name = "configurationindex")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Configuration_ViewModel model)
    {
        var input = new EditConfiguration.Command
        {
            OrganizationName = model.OrganizationName,
            TimeZone = model.TimeZone,
            DateFormat = model.DateFormat,
            WebsiteUrl = model.WebsiteUrl,
            SmtpDefaultFromAddress = model.SmtpDefaultFromAddress,
            SmtpDefaultFromName = model.SmtpDefaultFromName
        };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage("Configuration has been updated successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error attempting the configuration. See the error below.", response.GetErrors());
            return View(model);
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Configuration";
        ViewData["ExpandSettingsMenu"] = true;
    }
}

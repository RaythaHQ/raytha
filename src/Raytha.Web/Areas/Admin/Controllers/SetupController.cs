using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Views.Setup;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class SetupController : Controller
{
    private readonly ISender Mediator;
    private readonly IConfiguration _config;
    private readonly ICurrentOrganization CurrentOrganization;
    private readonly ICurrentUser CurrentUser;
    private const string RAYTHA_ROUTE_PREFIX = "raytha";

    public SetupController(
        ISender mediator,
        IConfiguration config, 
        ICurrentUser currentUser, 
        ICurrentOrganization currentOrganization) : base()
    {
        _config = config;
        CurrentUser = currentUser;
        CurrentOrganization = currentOrganization;
        Mediator = mediator;
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/setup", Name = "setupindex")]
    public IActionResult Index()
    {
        if (CurrentOrganization.InitialSetupComplete)
            return RedirectToAction("Index", "Dashboard");

        var viewModel = new Setup_ViewModel { MissingSmtpEnvironmentVariables = string.IsNullOrEmpty(_config["SMTP_HOST"]) };
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/setup", Name = "setupindex")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Setup_ViewModel model)
    {
        if (CurrentOrganization.InitialSetupComplete)
            return RedirectToAction("Index", "Dashboard");

        var input = new InitialSetup.Command
        {
            SmtpHost = model.SmtpHost,
            SmtpPort = model.SmtpPort,
            SmtpUsername = model.SmtpUsername,
            SmtpPassword = model.SmtpPassword,
            MissingSmtpEnvironmentVariables = string.IsNullOrEmpty(_config["SMTP_HOST"]),
            FirstName = model.FirstName,
            LastName = model.LastName,
            SuperAdminEmailAddress = model.SuperAdminEmailAddress,
            SuperAdminPassword = model.SuperAdminPassword,
            SmtpDefaultFromAddress = model.SmtpDefaultFromAddress,
            SmtpDefaultFromName = model.SmtpDefaultFromName,
            OrganizationName = model.OrganizationName,
            TimeZone = model.TimeZone,
            WebsiteUrl = model.WebsiteUrl
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        else
        {
            ViewData["ErrorMessage"] = "There was an error submitting the initial setup. See the error below.";
            this.HttpContext.Response.StatusCode = 303;
            return View(model);
        }
    }
}

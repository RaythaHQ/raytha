using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Functions",
                RouteName = RouteNames.RaythaFunctions.Index,
                IsActive = false,
                Icon = SidebarIcons.Functions,
            },
            new BreadcrumbNode
            {
                Label = "Create Raytha function",
                RouteName = RouteNames.RaythaFunctions.Create,
                IsActive = true,
            }
        );

        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateRaythaFunction.Command
        {
            Name = Form.Name,
            DeveloperName = Form.DeveloperName,
            TriggerType = Form.TriggerType,
            IsActive = Form.IsActive,
            Code = Form.Code,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Name} was created successfully.");
            return RedirectToPage(RouteNames.RaythaFunctions.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this function. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Trigger type")]
        public string TriggerType { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Code")]
        public string Code { get; set; } =
            @"
/** The following classes are available:
 * API_V1
 * CurrentOrganization
 * CurrentUser
 * Emailer
 * HttpClient
*/

/** 
 * For Trigger Type: Http trigger
 * Receives a get request at /raytha/functions/execute/{developerName}
 * @param {IQueryCollection} query passed in from .NET's Request.Query
 * @returns {object} of type JsonResult, HtmlResult, XmlResult, RedirectResult, or StatusCodeResult
 */
function get(query) {
    return new JsonResult({ success: true });
    //example 1: return new HtmlResult(""<p>Hello World</p>"");
    //example 2: return new XmlResult(""<root><message>Hello World</message></root>"");
    //example 3: return new RedirectResult(""https://raytha.com"");
    //example 4: return new StatusCodeResult(404, ""Not Found"");
}

/** 
 * For Trigger Type: Http trigger
 * Receives a post request at /raytha/functions/execute/{developerName}
 * @param {IFormCollection} payload passed in from .NET's Request.Form
 * @param {IQueryCollection} query passed in from .NET's Request.Query
 * @returns {object} of type JsonResult, HtmlResult, XmlResult, RedirectResult, or StatusCodeResult
 */
function post(payload, query) {
    return new JsonResult({ success: true });
    //example 1: return new HtmlResult(""<p>Hello World</p>"");
    //example 2: return new XmlResult(""<root><message>Hello World</message></root>"");
    //example 3: return new RedirectResult(""https://raytha.com"");
    //example 4: return new StatusCodeResult(404, ""Not Found"");
}

/**
 * For Trigger Type: Content item created, updated, deleted
 * @param {ContentItemDto} payload passed in from system
 * @returns {void}, no return type
 */
function run(payload) {
    //example: HttpClient.Post(""https://your-endpoint.com"", headers=null, body=payload);
}";
    }
}

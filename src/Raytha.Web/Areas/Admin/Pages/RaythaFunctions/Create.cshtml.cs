using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        return Page();
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
     * @returns {object} of type JsonResult, HtmlResult, RedirectResult, or StatusCodeResult
     */
    function get(query) {
        return new JsonResult({ success: true });
        //example 1: return new HtmlResult(""<p>Hello World</p>"");
        //example 2: return new RedirectResult(""https://raytha.com"");
        //example 3: return new StatusCodeResult(404, ""Not Found"");
    }

    /** 
     * For Trigger Type: Http trigger
     * Receives a post request at /raytha/functions/execute/{developerName}
     * @param {IFormCollection} payload passed in from .NET's Request.Form
     * @param {IQueryCollection} query passed in from .NET's Request.Query
     * @returns {object} of type JsonResult, HtmlResult, RedirectResult, or StatusCodeResult
     */
    function post(payload, query) {
        return new JsonResult({ success: true });
        //example 1: return new HtmlResult(""<p>Hello World</p>"");
        //example 2: return new RedirectResult(""https://raytha.com"");
        //example 3: return new StatusCodeResult(404, ""Not Found"");
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

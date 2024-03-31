using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

namespace Raytha.Web.Areas.Admin.Views.RaythaFunctions;

public class RaythaFunctionsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Name")]
    public string Name { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Trigger type")]
    public string TriggerType { get; set; }

    [Display(Name = "Active")]
    public string IsActive { get; set; }

    [Display(Name = "Last modified at")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified by")]
    public string LastModifierUser { get; init; }
}

public class RaythaFunctionsCreate_ViewModel : FormSubmit_ViewModel
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
    public string Code { get; set; } = @"
    /** The following classes are available:
     * API_V1
     * CurrentOrganization
     * CurrentUser
     * Emailer
     * HttpClient
    */

    /** 
     * Receives a get request at /raytha/functions/execute/{developerName}
     * @param {IQueryCollection} query Passed in from .NET's Request.Query
     * @returns {object} of type JsonResult, HtmlResult, RedirectResult, or StatusCodeResult
     */
    function get(query) {
        return new JsonResult({ success: true });
        //example 1: return new HtmlResult(""<p>Hello World</p>"");
        //example 2: return new RedirectResult(""https://raytha.com"");
        //example 3: return new StatusCodeResult(404, ""Not Found"");
    }

    /** 
     * Receives a post request at /raytha/functions/execute/{developerName}
     * @param {IFormCollection} payload Passed in from .NET's Request.Form
     * @param {IQueryCollection} query Passed in from .NET's Request.Query
     * @returns {object} of type JsonResult, HtmlResult, RedirectResult, or StatusCodeResult
     */
    function post(payload, query) {
        return new JsonResult({ success: true });
        //example 1: return new HtmlResult(""<p>Hello World</p>"");
        //example 2: return new RedirectResult(""https://raytha.com"");
        //example 3: return new StatusCodeResult(404, ""Not Found"");
    }";
}

public class RaythaFunctionsEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Name")]
    public string Name { get; set; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Trigger type")]
    public string TriggerType { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Code")]
    public string Code { get; set; }
}

public class RaythaFunctionsActionsMenu_ViewModel
{
    public string Id { get; set; }
    public string ActivePage { get; set; }
}

public class RaythaFunctionsRevisionsPagination_ViewModel : Pagination_ViewModel
{
    public IEnumerable<RaythaFunctionsRevisionsListItem_ViewModel> Items { get; }
    public string FunctionId { get; set; }
    public RaythaFunctionsRevisionsPagination_ViewModel(IEnumerable<RaythaFunctionsRevisionsListItem_ViewModel> items, int totalCount) : base(totalCount) => Items = items;
}

public class RaythaFunctionsRevisionsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Created at")]
    public string CreationTime { get; init; }

    [Display(Name = "Created by")]
    public string CreatorUser { get; init; }

    [Display(Name = "Code")]
    public string Code { get; init; }
}
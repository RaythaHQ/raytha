using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.SitePages.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteSitePage.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage("Site page has been deleted.");
            return RedirectToPage(RouteNames.SitePages.Index);
        }
        else
        {
            SetErrorMessage("There was a problem deleting this site page", response.GetErrors());
            return RedirectToPage(RouteNames.SitePages.Edit, new { id });
        }
    }
}


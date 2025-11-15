using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Delete : BaseHasFavoriteViewsPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteContentItem.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been deleted.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToPage(
            RouteNames.ContentItems.Index,
            new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
        );
    }
}

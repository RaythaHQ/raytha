using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Views.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Delete : BaseContentTypeContextPageModel
{
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        var response = await Mediator.Send(new DeleteView.Command { Id = CurrentView.Id });

        if (response.Success)
        {
            SetSuccessMessage("Successfully deleted view.");
            return RedirectToPage(
                RouteNames.ContentTypes.Views.Index,
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }

        SetErrorMessage("There was an error deleting this view.", response.GetErrors());
        return RedirectToPage(
            RouteNames.ContentItems.Index,
            new
            {
                contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                viewId = CurrentView.Id,
            }
        );
    }
}

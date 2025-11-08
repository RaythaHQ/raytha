using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Views.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
public class ToggleFavorite : BaseContentTypeContextPageModel
{
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostFavoriteAsync()
    {
        var response = await Mediator.Send(
            new ToggleViewAsFavoriteForAdmin.Command
            {
                UserId = CurrentUser.UserId!.Value,
                ViewId = CurrentView.Id,
                SetAsFavorite = true,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Successfully favorited view.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToContentItemsIndex();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostUnfavoriteAsync()
    {
        var response = await Mediator.Send(
            new ToggleViewAsFavoriteForAdmin.Command
            {
                UserId = CurrentUser.UserId!.Value,
                ViewId = CurrentView.Id,
                SetAsFavorite = false,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Successfully unfavorited view.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToContentItemsIndex();
    }

    private IActionResult RedirectToContentItemsIndex()
    {
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


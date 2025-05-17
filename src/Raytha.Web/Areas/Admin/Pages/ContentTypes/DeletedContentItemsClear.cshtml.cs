using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class DeletedContentItemsClear : BaseContentTypeContextPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new DeleteAlreadyDeletedContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage(
                $"{CurrentView.ContentType.LabelSingular} has been permanently removed."
            );
        }
        else
        {
            SetErrorMessage(
                $"There was an error permanently removing this {CurrentView.ContentType.LabelSingular.ToLower()}",
                response.GetErrors()
            );
        }
        return RedirectToPage(
            "/ContentTypes/DeletedContentItemsList",
            new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
        );
    }
}

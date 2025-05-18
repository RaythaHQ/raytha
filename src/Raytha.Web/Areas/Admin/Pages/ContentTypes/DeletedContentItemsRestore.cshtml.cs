using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class DeletedContentItemsRestore : BaseContentTypeContextPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new RestoreContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been restored.");
            return RedirectToPage(
                "/ContentItems/Edit",
                new
                {
                    id = response.Result.ToString(),
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                }
            );
        }
        else
        {
            SetErrorMessage(
                $"There was an error restoring this {CurrentView.ContentType.LabelSingular.ToLower()}",
                response.GetErrors()
            );
            return RedirectToPage(
                "/ContentTypes/DeletedContentItemsList",
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
    }
}

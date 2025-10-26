using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Fields;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Delete : BaseContentTypeContextPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteContentTypeField.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"Field has been deleted.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }
        return RedirectToPage(
            "/ContentTypes/Fields/Index",
            new { contentTypeByDeveloperName = CurrentView.ContentType.DeveloperName }
        );
    }
}

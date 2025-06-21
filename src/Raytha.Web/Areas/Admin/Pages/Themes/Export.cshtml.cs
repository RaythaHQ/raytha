using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Export : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetThemeById.Query { Id = id });

        Form = new FormModel
        {
            Id = id,
            IsExportable = response.Result.IsExportable,
            Url =
                $"{CurrentOrganization.WebsiteUrl}/raytha/themes/export/{response.Result.DeveloperName}",
        };
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new ToggleThemeExportability.Command
        {
            Id = id,
            IsExportable = Form.IsExportable,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
            SetSuccessMessage(
                $"The current theme export {(Form.IsExportable ? "enabled" : "disabled ")}"
            );
        else
            SetErrorMessage(
                "An error occurred while saving. See the error below.",
                response.GetErrors()
            );

        return RedirectToPage("/Themes/Export", new { id });
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Is exportable")]
        public bool IsExportable { get; set; }

        [Display(Name = "Url")]
        public string Url { get; set; }
    }
}

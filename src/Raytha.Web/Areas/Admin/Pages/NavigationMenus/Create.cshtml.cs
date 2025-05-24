using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Create : BaseAdminPageModel
{
    public string NavigationMenuId { get; set; }
    public bool IsMenuItems { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        Form = new FormModel();
        IsMenuItems = false;
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateNavigationMenu.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage("/NavigationMenus/Edit", new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this menu. See the error below.",
                response.GetErrors()
            );
        }
        IsMenuItems = false;
        return Page();
    }

    public record FormModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Is main menu")]
        public bool IsMainMenu { get; init; }
    }
}

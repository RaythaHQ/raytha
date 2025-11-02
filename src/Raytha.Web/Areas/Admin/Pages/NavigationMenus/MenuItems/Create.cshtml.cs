using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Admin.Pages.Shared;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus.MenuItems;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Create : BaseAdminPageModel, ISubActionViewModel
{
    public string NavigationMenuId { get; set; }
    public string NavigationMenuItemId { get; set; }
    public bool IsNavigationMenuItem { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string navigationMenuId)
    {
        NavigationMenuId = navigationMenuId;
        IsNavigationMenuItem = false;
        var input = new GetNavigationMenuItemsByNavigationMenuId.Query
        {
            NavigationMenuId = navigationMenuId,
        };

        var response = await Mediator.Send(input);

        Form = new FormModel
        {
            NavigationMenuId = navigationMenuId,
            NavigationMenuItems = response.Result,
        };
        return Page();
    }

    public async Task<IActionResult> OnPost(string navigationMenuId)
    {
        NavigationMenuId = navigationMenuId;

        var input = new CreateNavigationMenuItem.Command
        {
            NavigationMenuId = navigationMenuId,
            Label = Form.Label,
            Url = Form.Url,
            IsDisabled = Form.IsDisabled,
            OpenInNewTab = Form.OpenInNewTab,
            CssClassName = Form.CssClassName,
            ParentNavigationMenuItemId = !string.IsNullOrEmpty(Form.ParentNavigationMenuItemId)
                ? Form.ParentNavigationMenuItemId
                : (ShortGuid?)null,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(RouteNames.NavigationMenus.MenuItems.Index, new { navigationMenuId });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this menu item. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        public string NavigationMenuId { get; set; }

        [Display(Name = "Parent menu item")]
        public string ParentNavigationMenuItemId { get; set; }

        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Url")]
        public string Url { get; set; }

        [Display(Name = "Disabled")]
        public bool IsDisabled { get; set; }

        [Display(Name = "Open in new tab")]
        public bool OpenInNewTab { get; set; }

        [Display(Name = "Css class name")]
        public string CssClassName { get; set; }

        //helpers
        public IEnumerable<NavigationMenuItemDto> NavigationMenuItems { get; set; }
    }
}

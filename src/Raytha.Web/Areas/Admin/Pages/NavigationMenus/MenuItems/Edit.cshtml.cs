using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus.MenuItems;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    public string NavigationMenuId { get; set; }
    public string NavigationMenuItemId { get; set; }

    public bool IsNavigationMenuItem { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string navigationMenuId, string id)
    {
        NavigationMenuId = navigationMenuId;
        NavigationMenuItemId = id;
        IsNavigationMenuItem = true;

        var navigationMenuItemResponse = await Mediator.Send(
            new GetNavigationMenuItemById.Query { Id = id }
        );

        var allNavigationMenuItemsResponse = await Mediator.Send(
            new GetNavigationMenuItemsByNavigationMenuId.Query
            {
                NavigationMenuId = navigationMenuId,
            }
        );

        var navigationMenuItems = allNavigationMenuItemsResponse
            .Result.ExcludeNestedNavigationMenuItems(id)
            .Where(nmi => nmi.Id != id);
        Form = new FormModel
        {
            Id = navigationMenuItemResponse.Result.Id,
            Label = navigationMenuItemResponse.Result.Label,
            Url = navigationMenuItemResponse.Result.Url,
            IsDisabled = navigationMenuItemResponse.Result.IsDisabled,
            OpenInNewTab = navigationMenuItemResponse.Result.OpenInNewTab,
            CssClassName = navigationMenuItemResponse.Result.CssClassName,
            ParentNavigationMenuItemId = navigationMenuItemResponse
                .Result
                .ParentNavigationMenuItemId,
            NavigationMenuId = navigationMenuId,
            NavigationMenuItems = navigationMenuItems,
        };
        return Page();
    }

    public async Task<IActionResult> OnPost(string navigationMenuId, string id)
    {
        NavigationMenuId = navigationMenuId;
        NavigationMenuItemId = id;

        var input = new EditNavigationMenuItem.Command
        {
            Id = id,
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
            SetSuccessMessage($"{Form.Label} was edited successfully.");
        else
            SetErrorMessage(
                "There was an error attempting to save this menu item. See the error below.",
                response.GetErrors()
            );

        return RedirectToPage("/NavigationMenus/MenuItems/Edit", new { navigationMenuId, id });
    }

    public record FormModel
    {
        public string Id { get; set; }
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

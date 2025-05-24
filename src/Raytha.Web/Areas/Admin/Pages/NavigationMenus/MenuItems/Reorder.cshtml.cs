using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus.MenuItems;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Reorder : BaseAdminPageModel, ISubActionViewModel
{
    public string NavigationMenuId { get; set; }
    public string ParentNavigationMenuId { get; set; }
    public IEnumerable<NavigationMenuItemDto> NavigationMenuItemsForSelect { get; set; }
    public IDictionary<
        string,
        List<NavigationMenuItemsListItemViewModel>
    > NavigationMenuItemsByParentNavigationMenuItemId { get; set; }
    public string NavigationMenuItemId { get; set; }

    public async Task<IActionResult> OnGet(string navigationMenuId, string id)
    {
        NavigationMenuId = navigationMenuId;
        NavigationMenuItemId = id;

        var input = new GetNavigationMenuItemsByNavigationMenuId.Query
        {
            NavigationMenuId = navigationMenuId,
        };

        var response = await Mediator.Send(input);

        var navigationMenuItemsByParentNavigationMenuItemId = response
            .Result.GroupBy(nmi => nmi.ParentNavigationMenuItemId)
            .Where(groups => groups.Count() > 1)
            .ToDictionary(
                g => g.Key?.ToString() ?? string.Empty,
                g =>
                    g.OrderBy(nmi => nmi.Ordinal)
                        .Select(nmi => new NavigationMenuItemsListItemViewModel
                        {
                            Id = nmi.Id,
                            Label = nmi.Label,
                            Url = nmi.Url,
                        })
                        .ToList()
            );

        var navigationMenuItemsForSelect = navigationMenuItemsByParentNavigationMenuItemId
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .Select(kv => response.Result.First(nmi => nmi.Id == kv.Key));

        ParentNavigationMenuId = null;
        NavigationMenuItemsByParentNavigationMenuItemId =
            navigationMenuItemsByParentNavigationMenuItemId;
        NavigationMenuItemsForSelect = navigationMenuItemsForSelect;
        NavigationMenuId = navigationMenuId;

        return Page();
    }

    public async Task<IActionResult> Ajax([FromForm] string position, string id)
    {
        var input = new ReorderNavigationMenuItems.Command
        {
            Id = id,
            Ordinal = Convert.ToInt32(position),
        };

        var result = await Mediator.Send(input);

        if (result.Success)
            return new JsonResult(result);

        return BadRequest(result);
    }

    public class NavigationMenuItemsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Url")]
        public string Url { get; init; }

        [Display(Name = "Disabled")]
        public bool IsDisabled { get; init; }

        [Display(Name = "Open in new tab")]
        public bool OpenInNewTab { get; init; }

        [Display(Name = "Css class name")]
        public string CssClassName { get; init; }

        public IEnumerable<NavigationMenuItemsListItemViewModel> MenuItems { get; init; } = null!;
    }
}

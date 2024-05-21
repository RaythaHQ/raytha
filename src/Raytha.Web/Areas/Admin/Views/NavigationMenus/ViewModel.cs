using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Application.NavigationMenuItems;

namespace Raytha.Web.Areas.Admin.Views.NavigationMenus;

public class NavigationMenusListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Is main menu")]
    public bool IsMainMenu { get; init; }

    [Display(Name = "Last modified at")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified by")]
    public string LastModifierUser { get; init; }
}

public class NavigationMenusCreate_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer Name")]
    public string DeveloperName { get; set; }
}

public class NavigationMenusEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Is main menu")]
    public bool IsMainMenu { get; init; }
}

public class NavigationMenuRevisionsPagination_ViewModel : Pagination_ViewModel
{
    public IEnumerable<NavigationMenuRevisionsListItem_ViewModel> Items { get; }
    public string NavigationMenuId { get; set; }

    public NavigationMenuRevisionsPagination_ViewModel(IEnumerable<NavigationMenuRevisionsListItem_ViewModel> items, int totalCount, string navigationMenuId) : base(totalCount)
    {
        Items = items;
        NavigationMenuId = navigationMenuId;
    }
}

public class NavigationMenuRevisionsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Created at")]
    public string CreationTime { get; init; }

    [Display(Name = "Created by")]
    public string CreatorUser { get; init; }

    [Display(Name = "Menu items")]
    public string MenuItems { get; init; }
}

public class NavigationMenusActionsMenu_ViewModel
{
    public string NavigationMenuId { get; set; }
    public bool IsNavigationMenuItem { get; set; }
    public string NavigationMenuItemId { get; set; }
}

public class NavigationMenuItems_ViewModel
{
    public IEnumerable<NavigationMenuItemsListItem_ViewModel> Items { get; set; }
    public string NavigationMenuId { get; set; }
}

public class NavigationMenuItemsReorder_VieModel
{
    public string NavigationMenuId { get; set; }
    public string ParentNavigationMenuId { get; set; }
    public IEnumerable<NavigationMenuItemDto> NavigationMenuItemsForSelect { get; set; }
    public IDictionary<string, List<NavigationMenuItemsListItem_ViewModel>> NavigationMenuItemsByParentNavigationMenuItemId { get; set; }
}

public class NavigationMenuItemsListItem_ViewModel
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

    public IEnumerable<NavigationMenuItemsListItem_ViewModel> MenuItems { get; init; } = null!;
}

public class NavigationMenuItemsCreate_ViewModel : FormSubmit_ViewModel
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

public class NavigationMenuItemsEdit_ViewModel : FormSubmit_ViewModel
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

public class NavigationMenuItemsBackToList_ViewModel
{
    public string NavigationMenuId { get; set; }
    public bool IsMenuItems { get; set; }
}
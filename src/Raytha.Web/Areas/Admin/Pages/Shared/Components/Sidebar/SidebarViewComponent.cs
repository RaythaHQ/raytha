#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Infrastructure.Navigation;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Components.Sidebar;

/// <summary>
/// ViewComponent for rendering the admin sidebar navigation.
/// Uses NavMap to build the navigation structure, filters items by permissions,
/// and injects dynamic content type menu items.
/// </summary>
public class SidebarViewComponent : ViewComponent
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;

    public SidebarViewComponent(
        IAuthorizationService authorizationService,
        ICurrentUser currentUser,
        ICurrentOrganization currentOrganization
    )
    {
        _authorizationService = authorizationService;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var activeMenu = ViewContext.ViewData["ActiveMenu"]?.ToString();
        var menuItems = await BuildMenuAsync(activeMenu);

        return View(new SidebarViewModel { MenuItems = menuItems, ActiveMenu = activeMenu });
    }

    /// <summary>
    /// Builds the complete menu including static items and dynamic content types.
    /// </summary>
    private async Task<IEnumerable<NavMenuItem>> BuildMenuAsync(string? activeMenu)
    {
        var allItems = new List<NavMenuItem>();

        // Get static menu items from NavMap
        var staticItems = NavMap.GetMenuItems().ToList();

        // Insert dynamic content type menu items at Order 50
        var contentTypeItems = await BuildContentTypeMenuItemsAsync();
        staticItems.AddRange(contentTypeItems);

        // Add profile menu with user info
        var profileMenu = NavMap.GetProfileMenu(
            _currentUser.FullName ?? "User",
            _currentOrganization.EmailAndPasswordIsEnabledForAdmins
        );
        staticItems.Add(profileMenu);

        // Filter by permissions
        foreach (var item in staticItems.OrderBy(i => i.Order))
        {
            var processedItem = await ProcessMenuItemAsync(item);
            if (processedItem != null)
            {
                allItems.Add(processedItem);
            }
        }

        return allItems;
    }

    /// <summary>
    /// Builds menu items for each content type dynamically.
    /// </summary>
    private async Task<IEnumerable<NavMenuItem>> BuildContentTypeMenuItemsAsync()
    {
        var items = new List<NavMenuItem>();
        int order = 50; // Start at order 50

        foreach (var contentType in _currentOrganization.ContentTypes)
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                HttpContext.User,
                contentType.DeveloperName,
                ContentItemOperations.Read
            );

            if (authResult.Succeeded)
            {
                items.Add(
                    new NavMenuItem
                    {
                        Id = contentType.DeveloperName,
                        Label = contentType.LabelPlural,
                        RouteName = RouteNames.ContentItems.Index,
                        Icon = null, // Content types don't have icons in the old design
                        Permission = null, // Already checked authorization
                        Order = order++,
                    }
                );
            }
        }

        return items;
    }

    /// <summary>
    /// Processes a single menu item: checks permissions and processes children recursively.
    /// Returns null if the item should be filtered out.
    /// </summary>
    private async Task<NavMenuItem?> ProcessMenuItemAsync(NavMenuItem item)
    {
        // Check permission if required
        if (!string.IsNullOrEmpty(item.Permission))
        {
            var authResult = await _authorizationService.AuthorizeAsync(
                HttpContext.User,
                item.Permission
            );

            if (!authResult.Succeeded)
            {
                return null; // Filter out
            }
        }

        // Process children recursively
        List<NavMenuItem>? processedChildren = null;
        if (item.Children != null && item.Children.Any())
        {
            processedChildren = new List<NavMenuItem>();
            foreach (var child in item.Children.OrderBy(c => c.Order))
            {
                var processedChild = await ProcessMenuItemAsync(child);
                if (processedChild != null)
                {
                    processedChildren.Add(processedChild);
                }
            }

            // If parent has children but all were filtered out, filter out the parent too
            if (!processedChildren.Any())
            {
                return null;
            }
        }

        // Return the item with processed children
        return new NavMenuItem
        {
            Id = item.Id,
            Label = item.Label,
            RouteName = item.RouteName,
            Icon = item.Icon,
            Permission = item.Permission,
            Order = item.Order,
            Children = processedChildren,
            IsDivider = item.IsDivider,
            CssClass = item.CssClass,
            OpenInNewTab = item.OpenInNewTab,
        };
    }
}

/// <summary>
/// ViewModel for the Sidebar ViewComponent.
/// </summary>
public class SidebarViewModel
{
    public IEnumerable<NavMenuItem> MenuItems { get; set; } = Enumerable.Empty<NavMenuItem>();
    public string? ActiveMenu { get; set; }
}

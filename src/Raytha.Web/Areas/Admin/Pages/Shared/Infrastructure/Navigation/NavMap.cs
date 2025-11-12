#nullable enable
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Infrastructure.Navigation;

/// <summary>
/// Centralized navigation structure for the admin area.
/// Defines menu items, hierarchy, permissions, and icons.
/// This is the single source of truth for the sidebar navigation menu.
/// </summary>
public static class NavMap
{
    /// <summary>
    /// Gets the complete navigation menu structure.
    /// Note: Content types are added dynamically by the Sidebar ViewComponent.
    /// </summary>
    /// <returns>Collection of root-level navigation menu items.</returns>
    public static IEnumerable<NavMenuItem> GetMenuItems()
    {
        return new[]
        {
            new NavMenuItem
            {
                Id = "Dashboard",
                Label = "Dashboard",
                RouteName = RouteNames.Dashboard.Index,
                Icon = IconLibrary.Dashboard,
                Permission = null, // Available to all authenticated admins
                Order = 0,
            },
            // Users (order 10)
            new NavMenuItem
            {
                Id = "Users",
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                Icon = IconLibrary.Users,
                Permission = BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                Order = 10,
            },
            // Content Types will be inserted dynamically at order 50
            // by the Sidebar ViewComponent based on ICurrentOrganization.ContentTypes

            // Divider before system menu items
            new NavMenuItem
            {
                Id = "DividerBeforeSystem",
                Label = string.Empty,
                IsDivider = true,
                Order = 90,
            },
            // Website link
            new NavMenuItem
            {
                Id = "LiveWebsite",
                Label = "Live Website",
                RouteName = null, // Set dynamically to empty route
                Icon = IconLibrary.Website,
                OpenInNewTab = true,
                Order = 95,
            },
            // Themes
            new NavMenuItem
            {
                Id = "Themes",
                Label = "Themes",
                RouteName = RouteNames.Themes.Index,
                Icon = IconLibrary.Themes,
                Permission = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                Order = 100,
            },
            // Email Templates
            new NavMenuItem
            {
                Id = "Email Templates",
                Label = "Email Templates",
                RouteName = RouteNames.EmailTemplates.Index,
                Icon = IconLibrary.EmailTemplates,
                Permission = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                Order = 110,
            },
            // Navigation Menus
            new NavMenuItem
            {
                Id = "Menus",
                Label = "Menus",
                RouteName = RouteNames.NavigationMenus.Index,
                Icon = IconLibrary.Menus,
                Permission = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION,
                Order = 120,
            },
            // Raytha Functions
            new NavMenuItem
            {
                Id = "Functions",
                Label = "Functions",
                RouteName = RouteNames.RaythaFunctions.Index,
                Icon = IconLibrary.Functions,
                Permission = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                Order = 130,
            },
            // Audit Log
            new NavMenuItem
            {
                Id = "Audit Logs",
                Label = "Audit Log",
                RouteName = RouteNames.AuditLogs.Index,
                Icon = IconLibrary.AuditLog,
                Permission = BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION,
                Order = 140,
            },
            // Settings (with submenu)
            new NavMenuItem
            {
                Id = "Settings",
                Label = "Settings",
                Icon = IconLibrary.Settings,
                Permission = null, // Check children permissions
                Order = 200,
                Children = new[]
                {
                    new NavMenuItem
                    {
                        Id = "Admins",
                        Label = "Admins",
                        RouteName = RouteNames.Admins.Index,
                        Permission = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION,
                        Order = 0,
                    },
                    new NavMenuItem
                    {
                        Id = "Configuration",
                        Label = "Configuration",
                        RouteName = RouteNames.Configuration.Index,
                        Permission = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                        Order = 10,
                    },
                    new NavMenuItem
                    {
                        Id = "Authentication Schemes",
                        Label = "Authentication",
                        RouteName = RouteNames.AuthenticationSchemes.Index,
                        Permission = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                        Order = 20,
                    },
                    new NavMenuItem
                    {
                        Id = "SMTP",
                        Label = "SMTP",
                        RouteName = RouteNames.Smtp.Index,
                        Permission = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                        Order = 30,
                    },
                },
            },

            // Profile menu will be added separately at the bottom with user info
        };
    }

    /// <summary>
    /// Gets the profile/user menu items (typically shown at the bottom of sidebar).
    /// </summary>
    /// <param name="fullName">The current user's full name.</param>
    /// <param name="emailAndPasswordEnabled">Whether email/password authentication is enabled.</param>
    /// <returns>Profile menu items.</returns>
    public static NavMenuItem GetProfileMenu(string fullName, bool emailAndPasswordEnabled)
    {
        var children = new List<NavMenuItem>
        {
            new NavMenuItem
            {
                Id = "Profile",
                Label = "My Profile",
                RouteName = RouteNames.Profile.Index,
                Order = 0,
            },
        };

        if (emailAndPasswordEnabled)
        {
            children.Add(
                new NavMenuItem
                {
                    Id = "Change Password",
                    Label = "Change Password",
                    RouteName = RouteNames.Profile.ChangePassword,
                    Order = 10,
                }
            );
        }

        children.Add(
            new NavMenuItem
            {
                Id = "Logout",
                Label = "Log out",
                RouteName = RouteNames.Login.Logout,
                Order = 20,
            }
        );

        return new NavMenuItem
        {
            Id = "ProfileMenu",
            Label = fullName,
            Order = 1000, // Always at the bottom
            Children = children,
        };
    }
}

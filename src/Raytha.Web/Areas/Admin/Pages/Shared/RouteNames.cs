namespace Raytha.Web.Areas.Admin.Pages.Shared;

/// <summary>
/// Centralized constants for Razor Page routes throughout the admin area.
/// Eliminates magic strings and provides compile-time safety for page navigation.
/// </summary>
public static class RouteNames
{
    /// <summary>
    /// Route constants for user management pages.
    /// </summary>
    public static class Users
    {
        public const string Index = "/Users/Index";
        public const string Create = "/Users/Create";
        public const string Edit = "/Users/Edit";
        public const string Delete = "/Users/Delete";
        public const string Suspend = "/Users/Suspend";
        public const string Restore = "/Users/Restore";
        public const string ResetPassword = "/Users/ResetPassword";
    }

    /// <summary>
    /// Route constants for administrator management pages.
    /// </summary>
    public static class Admins
    {
        public const string Index = "/Admins/Index";
        public const string Create = "/Admins/Create";
        public const string Edit = "/Admins/Edit";
        public const string Delete = "/Admins/Delete";
        public const string Suspend = "/Admins/Suspend";
        public const string Restore = "/Admins/Restore";
        public const string ResetPassword = "/Admins/ResetPassword";
    }

    /// <summary>
    /// Route constants for user group management pages.
    /// </summary>
    public static class UserGroups
    {
        public const string Index = "/UserGroups/Index";
        public const string Create = "/UserGroups/Create";
        public const string Edit = "/UserGroups/Edit";
        public const string Delete = "/UserGroups/Delete";
    }

    /// <summary>
    /// Route constants for role management pages.
    /// </summary>
    public static class Roles
    {
        public const string Index = "/Roles/Index";
        public const string Create = "/Roles/Create";
        public const string Edit = "/Roles/Edit";
        public const string Delete = "/Roles/Delete";
    }

    /// <summary>
    /// Route constants for content type management pages.
    /// </summary>
    public static class ContentTypes
    {
        public const string Index = "/ContentTypes/Index";
        public const string Create = "/ContentTypes/Create";
        public const string Edit = "/ContentTypes/Edit";
        public const string Delete = "/ContentTypes/Delete";
        public const string Configuration = "/ContentTypes/Configuration";
        public const string DeletedContentItemsList = "/ContentTypes/DeletedContentItemsList";
        public const string DeletedContentItemsRestore = "/ContentTypes/DeletedContentItemsRestore";
        public const string DeletedContentItemsClear = "/ContentTypes/DeletedContentItemsClear";

        /// <summary>
        /// Route constants for content type field management pages.
        /// </summary>
        public static class Fields
        {
            public const string Index = "/ContentTypes/Fields/Index";
            public const string Create = "/ContentTypes/Fields/Create";
            public const string Edit = "/ContentTypes/Fields/Edit";
            public const string Delete = "/ContentTypes/Fields/Delete";
            public const string Reorder = "/ContentTypes/Fields/Reorder";
        }

        /// <summary>
        /// Route constants for content type view management pages.
        /// </summary>
        public static class Views
        {
            public const string Index = "/ContentTypes/Views/Index";
            public const string Create = "/ContentTypes/Views/Create";
            public const string Edit = "/ContentTypes/Views/Edit";
            public const string Delete = "/ContentTypes/Views/Delete";
        }
    }

    /// <summary>
    /// Route constants for content item management pages.
    /// </summary>
    public static class ContentItems
    {
        public const string Index = "/ContentItems/Index";
        public const string Create = "/ContentItems/Create";
        public const string Edit = "/ContentItems/Edit";
        public const string Delete = "/ContentItems/Delete";
        public const string Revisions = "/ContentItems/Revisions";
        public const string Settings = "/ContentItems/Settings";
    }

    /// <summary>
    /// Route constants for template management pages.
    /// </summary>
    public static class Templates
    {
        public const string Index = "/Templates/Index";
        public const string Create = "/Templates/Create";
        public const string Edit = "/Templates/Edit";
        public const string Delete = "/Templates/Delete";
    }

    /// <summary>
    /// Route constants for media item management pages.
    /// </summary>
    public static class MediaItems
    {
        public const string Index = "/MediaItems/Index";
    }

    /// <summary>
    /// Route constants for configuration pages.
    /// </summary>
    public static class Configuration
    {
        public const string Index = "/Configuration/Index";
        public const string EmailSettings = "/Configuration/EmailSettings";
        public const string AuthenticationSchemes = "/Configuration/AuthenticationSchemes";
    }

    /// <summary>
    /// Route constants for setup pages.
    /// </summary>
    public static class Setup
    {
        public const string Index = "/Setup/Index";
    }
}

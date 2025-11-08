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
        public const string RemoveAccess = "/Admins/RemoveAccess";
        public const string ResetPassword = "/Admins/ResetPassword";
        public const string ApiKeys = "/Admins/ApiKeys";
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
        public const string BeginImportFromCsv = "/ContentTypes/BeginImportFromCsv";

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
            public const string Sort = "/ContentTypes/Views/Sort";
            public const string Filter = "/ContentTypes/Views/Filter";
            public const string Columns = "/ContentTypes/Views/Columns";
            public const string PublicSettings = "/ContentTypes/Views/PublicSettings";
            public const string BeginExportToCsv = "/ContentTypes/Views/BeginExportToCsv";
            public const string ToggleFavorite = "/ContentTypes/Views/ToggleFavorite";
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

    /// <summary>
    /// Route constants for dashboard pages.
    /// </summary>
    public static class Dashboard
    {
        public const string Index = "/Dashboard/Index";
    }

    /// <summary>
    /// Route constants for theme management pages.
    /// </summary>
    public static class Themes
    {
        public const string Index = "/Themes/Index";
        public const string Create = "/Themes/Create";
        public const string Edit = "/Themes/Edit";
        public const string Delete = "/Themes/Delete";
        public const string Duplicate = "/Themes/Duplicate";
        public const string Import = "/Themes/Import";
        public const string Export = "/Themes/Export";
        public const string ExportAsJson = "/Themes/ExportAsJson";
        public const string MediaItems = "/Themes/MediaItems";
        public const string SetAsActive = "/Themes/SetAsActive";
        public const string BackgroundTaskStatus = "/Themes/BackgroundTaskStatus";
        public const string AccessToTemplateRedirect = "/Themes/AccessToTemplateRedirect";
        public const string ExportTheme = "/Themes/ExportTheme";

        /// <summary>
        /// Route constants for theme web templates management pages.
        /// </summary>
        public static class WebTemplates
        {
            public const string Index = "/Themes/WebTemplates/Index";
            public const string Create = "/Themes/WebTemplates/Create";
            public const string Edit = "/Themes/WebTemplates/Edit";
            public const string Delete = "/Themes/WebTemplates/Delete";
            public const string Revisions = "/Themes/WebTemplates/Revisions";
        }

        /// <summary>
        /// Route constants for theme template management pages.
        /// </summary>
        public static class Templates
        {
            public const string Index = "/Themes/Templates/Index";
            public const string Create = "/Themes/Templates/Create";
            public const string Edit = "/Themes/Templates/Edit";
            public const string Delete = "/Themes/Templates/Delete";
            public const string Revert = "/Themes/Templates/Revert";
        }
    }

    /// <summary>
    /// Route constants for email template management pages.
    /// </summary>
    public static class EmailTemplates
    {
        public const string Index = "/EmailTemplates/Index";
        public const string Edit = "/EmailTemplates/Edit";
        public const string Revisions = "/EmailTemplates/Revisions";
    }

    /// <summary>
    /// Route constants for navigation menu management pages.
    /// </summary>
    public static class NavigationMenus
    {
        public const string Index = "/NavigationMenus/Index";
        public const string Create = "/NavigationMenus/Create";
        public const string Edit = "/NavigationMenus/Edit";
        public const string Delete = "/NavigationMenus/Delete";
        public const string Revisions = "/NavigationMenus/Revisions";
        public const string SetAsMainMenu = "/NavigationMenus/SetAsMainMenu";

        /// <summary>
        /// Route constants for navigation menu item management pages.
        /// </summary>
        public static class MenuItems
        {
            public const string Index = "/NavigationMenus/MenuItems/Index";
            public const string Create = "/NavigationMenus/MenuItems/Create";
            public const string Edit = "/NavigationMenus/MenuItems/Edit";
            public const string Delete = "/NavigationMenus/MenuItems/Delete";
            public const string Reorder = "/NavigationMenus/MenuItems/Reorder";
        }
    }

    /// <summary>
    /// Route constants for Raytha Functions management pages.
    /// </summary>
    public static class RaythaFunctions
    {
        public const string Index = "/RaythaFunctions/Index";
        public const string Create = "/RaythaFunctions/Create";
        public const string Edit = "/RaythaFunctions/Edit";
        public const string Delete = "/RaythaFunctions/Delete";
        public const string Execute = "/RaythaFunctions/Execute";
        public const string Revisions = "/RaythaFunctions/Revisions";
    }

    /// <summary>
    /// Route constants for authentication scheme management pages.
    /// </summary>
    public static class AuthenticationSchemes
    {
        public const string Index = "/AuthenticationSchemes/Index";
        public const string Create = "/AuthenticationSchemes/Create";
        public const string Edit = "/AuthenticationSchemes/Edit";
        public const string Delete = "/AuthenticationSchemes/Delete";
    }

    /// <summary>
    /// Route constants for audit log pages.
    /// </summary>
    public static class AuditLogs
    {
        public const string Index = "/AuditLogs/Index";
    }

    /// <summary>
    /// Route constants for user profile pages.
    /// </summary>
    public static class Profile
    {
        public const string Index = "/Profile/Index";
        public const string ChangePassword = "/Profile/ChangePassword";
    }

    /// <summary>
    /// Route constants for SMTP configuration pages.
    /// </summary>
    public static class Smtp
    {
        public const string Index = "/Smtp/Index";
    }

    /// <summary>
    /// Route constants for login and authentication pages.
    /// </summary>
    public static class Login
    {
        public const string LoginRedirect = "/Login/LoginRedirect";
        public const string LoginWithEmailAndPassword = "/Login/LoginWithEmailAndPassword";
        public const string LoginWithMagicLink = "/Login/LoginWithMagicLink";
        public const string LoginWithMagicLinkSent = "/Login/LoginWithMagicLinkSent";
        public const string LoginWithMagicLinkComplete = "/Login/LoginWithMagicLinkComplete";
        public const string LoginWithSaml = "/Login/LoginWithSaml";
        public const string LoginWithSso = "/Login/LoginWithSso";
        public const string LoginWithJwt = "/Login/LoginWithJwt";
        public const string ForgotPassword = "/Login/ForgotPassword";
        public const string ForgotPasswordSent = "/Login/ForgotPasswordSent";
        public const string ForgotPasswordComplete = "/Login/ForgotPasswordComplete";
        public const string Logout = "/Login/Logout";
    }
}

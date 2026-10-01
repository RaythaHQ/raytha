namespace Raytha.Web.Areas.Admin.Pages.Shared;

/// <summary>
/// Razor Page names for the server-side admin pages that remain after the admin UI moved to the
/// React SPA: SSO entry and callbacks, the cookie login redirect, and sign-out. The sign-in screens
/// and everything else under /raytha are client-side routes.
/// </summary>
public static class RouteNames
{
    /// <summary>
    /// Route constants for login and authentication pages.
    /// </summary>
    public static class Login
    {
        public const string LoginRedirect = "/Login/LoginRedirect";
        public const string LoginWithSaml = "/Login/LoginWithSaml";
        public const string LoginWithSso = "/Login/LoginWithSso";
        public const string LoginWithJwt = "/Login/LoginWithJwt";
        public const string Logout = "/Login/Logout";
    }
}

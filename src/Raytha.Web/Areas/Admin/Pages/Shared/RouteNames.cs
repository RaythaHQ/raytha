namespace Raytha.Web.Areas.Admin.Pages.Shared;

/// <summary>
/// Razor Page names for the server-side admin pages that remain after the admin UI moved to the
/// React SPA: login flows, SSO callbacks and emailed token links. Everything else under /raytha is
/// a client-side route.
/// </summary>
public static class RouteNames
{
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

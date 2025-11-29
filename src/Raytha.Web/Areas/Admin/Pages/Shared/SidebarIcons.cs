namespace Raytha.Web.Areas.Admin.Pages.Shared;

/// <summary>
/// Centralized repository of sidebar navigation icons.
/// Each icon matches exactly what is displayed in the sidebar (SidebarLayout.cshtml).
/// </summary>
public static class SidebarIcons
{
    /// <summary>
    /// Dashboard icon (pie chart)
    /// </summary>
    public const string Dashboard =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M2 10a8 8 0 018-8v8h8a8 8 0 11-16 0z\"></path><path d=\"M12 2.252A8.014 8.014 0 0117.748 8H12V2.252z\"></path></svg>";

    /// <summary>
    /// Users icon (group of people)
    /// </summary>
    public const string Users =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path fill-rule=\"evenodd\" d=\"M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z\" clip-rule=\"evenodd\"></path></svg>";

    /// <summary>
    /// Themes icon (layout/grid)
    /// </summary>
    public const string Themes =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path fill-rule=\"evenodd\" d=\"M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z\" clip-rule=\"evenodd\"></path></svg>";

    /// <summary>
    /// Email Templates icon (inbox/mail)
    /// </summary>
    public const string EmailTemplates =
        "<svg class=\"icon icon-xs me-2\" xmlns=\"http://www.w3.org/2000/svg\" fill=\"none\" viewBox=\"0 0 24 24\" stroke-width=\"1.5\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"M9 3.75H6.912a2.25 2.25 0 00-2.15 1.588L2.35 13.177a2.25 2.25 0 00-.1.661V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18v-4.162c0-.224-.034-.447-.1-.661L19.24 5.338a2.25 2.25 0 00-2.15-1.588H15M2.25 13.5h3.86a2.25 2.25 0 012.012 1.244l.256.512a2.25 2.25 0 002.013 1.244h3.218a2.25 2.25 0 002.013-1.244l.256-.512a2.25 2.25 0 012.013-1.244h3.859M12 3v8.25m0 0l-3-3m3 3l3-3\" /></svg>";

    /// <summary>
    /// Menus / Navigation Menus icon (hamburger menu lines)
    /// </summary>
    public const string Menus =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\"><path clip-rule=\"evenodd\" fill-rule=\"evenodd\" d=\"M19 4a1 1 0 01-1 1H2a1 1 0 010-2h16a1 1 0 011 1zm0 6a1 1 0 01-1 1H2a1 1 0 110-2h16a1 1 0 011 1zm-1 7a1 1 0 100-2H2a1 1 0 100 2h16z\" /></svg>";

    /// <summary>
    /// Functions / Raytha Functions icon (code/function symbol)
    /// </summary>
    public const string Functions =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" version=\"1.1\" id=\"_x32_\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 512 512\" xml:space=\"preserve\"><g id=\"SVGRepo_bgCarrier\" stroke-width=\"0\"></g><g id=\"SVGRepo_tracerCarrier\" stroke-linecap=\"round\" stroke-linejoin=\"round\"></g><g id=\"SVGRepo_iconCarrier\"><style type=\"text/css\"></style><g><path class=\"st0\" d=\"M256,0C114.614,0,0,114.614,0,256s114.614,256,256,256c141.386,0,256-114.614,256-256S397.386,0,256,0z M203.969,301.371c-8.697,35.561-31.53,78.796-90.924,73.485c-10.409-1.614-20.057-7.538-20.72-18.318 c-0.666-10.78,7.534-20.053,18.315-20.712c6.159-0.379,11.834,2.136,15.685,6.379c2.879,3.182,5.743,6.44,13.406,9.371 c19.564-2.561,25.439-39.659,29.606-60.318c1.424-7.046,3.879-20.409,6.768-35.833h-36.052l5.432-21.046h34.606 c4.716-24.56,9.81-49.583,13.276-60.902c9.178-29.978,33.201-57.136,65.091-58.424c12.626-0.5,24.596,2.129,34.906,7.78 c7.303,3.901,11.878,11.886,12.246,17.879c0.739,11.977-8.371,22.28-20.345,23.022c-8.985,0.553-17.837-4.075-20.795-12.015 c-5.314-14.258-19.087-19.227-26.068-8.909c-6.231,9.227-12.731,36.151-17.205,62.954c-1.302,7.826-2.984,17.894-4.814,28.614 h30.254l-5.428,21.046h-28.455C209.34,274.886,205.965,293.204,203.969,301.371z M357.424,360 c-22.44,3.007-42.266-8.819-51.163-33.258c-0.773-2.128-1.5-4.136-2.208-6.098c-5.496,9.159-10.815,17.742-13.875,21.803 c-9.512,12.697-25.61,21.485-41,16.614c-6.099-1.932-13.376-6.463-17.315-10.917c-2.803-3.113-3.606-7.704-2.754-10.621 c1.709-5.826,7.815-9.152,13.64-7.447c4.372,1.281,7.785,5.008,7.826,9.28c0.08,7.705,5.777,12.439,10.872,8.727 c4.553-3.326,12.272-15.015,19.004-27c3.682-6.537,9.425-16.53,14.58-25.394c-2.977-8.106-5.86-15.652-9.53-24.613 c-11.121-27.144-31.883-16.288-31.883-23.758c0-6.258,14.03-26.334,39.299-17.644c11.004,3.773,17.512,16.773,22.8,32.084 c3.701-5.993,7.25-11.493,9.912-14.992c9.606-12.606,25.766-21.303,41.114-16.326c6.084,1.97,13.342,6.538,17.251,11.023 c2.78,3.136,3.553,7.727,2.682,10.636c-1.75,5.81-7.878,9.106-13.69,7.364c-4.356-1.318-7.75-5.061-7.765-9.349 c-0.046-14.841-11.099-8.576-10.811-8.788c-4.576,3.288-12.371,14.924-19.189,26.864c-2.932,5.144-7.17,12.417-11.386,19.591 c0.352,1.113,0.704,2.22,1.06,3.318c6.97,21.462,13.242,42.773,34.015,44.47c13.455,1.098,30.629-6.235,28.174-19.689 c-0.734-4.076,2.97-6.788,6.675-6.788c3.712,0,4.454,4.75,4.454,4.75C397.47,326.068,387.833,355.932,357.424,360z\"></path></g></g></svg>";

    /// <summary>
    /// Audit Logs icon (calendar/log)
    /// </summary>
    public const string AuditLogs =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path fill-rule=\"evenodd\" d=\"M6 2a1 1 0 00-1 1v1H4a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V6a2 2 0 00-2-2h-1V3a1 1 0 10-2 0v1H7V3a1 1 0 00-1-1zm0 5a1 1 0 000 2h8a1 1 0 100-2H6z\" clip-rule=\"evenodd\"></path></svg>";

    /// <summary>
    /// Settings icon (gear/cog)
    /// </summary>
    public const string Settings =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path fill-rule=\"evenodd\" d=\"M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z\" clip-rule=\"evenodd\"></path></svg>";

    /// <summary>
    /// Live Website icon (external link/open in new window)
    /// </summary>
    public const string LiveWebsite =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path fill-rule=\"evenodd\" d=\"M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14\" clip-rule=\"evenodd\"></path></svg>";

    /// <summary>
    /// Content Items icon (desktop/computer) - used for dynamic content types
    /// Note: This icon has "invisible" class in sidebar, so it may not be appropriate for breadcrumbs
    /// </summary>
    public const string ContentItems =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" d=\"M9 17.25v1.007a3 3 0 01-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0115 18.257V17.25m6-12V15a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 15V5.25m18 0A2.25 2.25 0 0018.75 3H5.25A2.25 2.25 0 003 5.25m18 0V12a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 12V5.25\" /></svg>";

    /// <summary>
    /// Site Pages icon (document with grid) - used for one-off pages with widget layouts
    /// </summary>
    public const string SitePages =
        "<svg class=\"icon icon-xs me-2\" fill=\"currentColor\" viewBox=\"0 0 20 20\" xmlns=\"http://www.w3.org/2000/svg\"><path fill-rule=\"evenodd\" d=\"M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4zm2 6a1 1 0 011-1h6a1 1 0 110 2H7a1 1 0 01-1-1zm1 3a1 1 0 100 2h6a1 1 0 100-2H7z\" clip-rule=\"evenodd\"></path></svg>";
}

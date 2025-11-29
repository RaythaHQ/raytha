using System.Text.Json;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Application.AuditLogs.Commands;
using Raytha.Application.AuditLogs.Queries;
using Raytha.Application.AuthenticationSchemes.Commands;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.Login.Commands;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.Roles.Commands;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.Themes.WidgetTemplates.Commands;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.Users.Commands;
using Raytha.Application.Views.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.AuditLogs;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Dictionary<string, string> LogCategories
    {
        get
        {
            var logCategories = new List<string>()
            {
                //Admins
                new CreateAdmin.Command().GetLogName(),
                new CreateApiKey.Command().GetLogName(),
                new DeleteAdmin.Command().GetLogName(),
                new DeleteApiKey.Command().GetLogName(),
                new EditAdmin.Command().GetLogName(),
                new RemoveAdminAccess.Command().GetLogName(),
                new Application.Admins.Commands.ResetPassword.Command().GetLogName(),
                new Application.Admins.Commands.SetIsActive.Command().GetLogName(),
                //AuditLogs
                new ClearAllAuditLogs.Command().GetLogName(),
                //AuthenticationSchemes
                new CreateAuthenticationScheme.Command().GetLogName(),
                new DeleteAuthenticationScheme.Command().GetLogName(),
                new EditAuthenticationScheme.Command().GetLogName(),
                //ContentItems
                new CreateContentItem.Command().GetLogName(),
                new DeleteContentItem.Command().GetLogName(),
                new DeleteAlreadyDeletedContentItem.Command().GetLogName(),
                new DiscardDraftContentItem.Command().GetLogName(),
                new EditContentItem.Command().GetLogName(),
                new EditContentItemSettings.Command().GetLogName(),
                new RestoreContentItem.Command().GetLogName(),
                new RevertContentItem.Command().GetLogName(),
                new Application.ContentItems.Commands.SetAsHomePage.Command().GetLogName(),
                new UnpublishContentItem.Command().GetLogName(),
                new BeginExportContentItemsToCsv.Command().GetLogName(),
                new BeginImportContentItemsFromCsv.Command().GetLogName(),
                //ContentTypes
                new CreateContentType.Command().GetLogName(),
                new CreateContentTypeField.Command().GetLogName(),
                new DeleteContentTypeField.Command().GetLogName(),
                new EditContentType.Command().GetLogName(),
                new EditContentTypeField.Command().GetLogName(),
                new ReorderContentTypeField.Command().GetLogName(),
                //Email-Templates
                new EditEmailTemplate.Command().GetLogName(),
                new RevertEmailTemplate.Command().GetLogName(),
                //Login
                new BeginForgotPassword.Command().GetLogName(),
                new BeginLoginWithMagicLink.Command().GetLogName(),
                new ChangePassword.Command().GetLogName(),
                new ChangeProfile.Command().GetLogName(),
                new CompleteForgotPassword.Command().GetLogName(),
                new CompleteLoginWithMagicLink.Command().GetLogName(),
                new Application.Login.Commands.CreateUser.Command().GetLogName(),
                new LoginWithEmailAndPassword.Command().GetLogName(),
                new LoginWithJwt.Command().GetLogName(),
                new LoginWithSaml.Command().GetLogName(),
                //MediaItems
                CreateMediaItem.Command.Empty().GetLogName(),
                new DeleteMediaItem.Command().GetLogName(),
                //NavigationMenus
                new CreateNavigationMenu.Command
                {
                    Label = string.Empty,
                    DeveloperName = string.Empty,
                }.GetLogName(),
                new CreateNavigationMenuRevision.Command
                {
                    NavigationMenuId = string.Empty,
                }.GetLogName(),
                new DeleteNavigationMenu.Command().GetLogName(),
                new EditNavigationMenu.Command { Label = string.Empty }.GetLogName(),
                new RevertNavigationMenu.Command().GetLogName(),
                new SetAsMainMenu.Command().GetLogName(),
                //NavigationMenuItems
                new CreateNavigationMenuItem.Command
                {
                    NavigationMenuId = string.Empty,
                    Label = string.Empty,
                    Url = string.Empty,
                }.GetLogName(),
                new DeleteNavigationMenuItem.Command
                {
                    NavigationMenuId = string.Empty,
                }.GetLogName(),
                new EditNavigationMenuItem.Command
                {
                    NavigationMenuId = string.Empty,
                    Label = string.Empty,
                    Url = string.Empty,
                }.GetLogName(),
                new ReorderNavigationMenuItems.Command().GetLogName(),
                //OrganizationSettings
                new EditConfiguration.Command().GetLogName(),
                new EditSmtp.Command().GetLogName(),
                new InitialSetup.Command().GetLogName(),
                //RaythaFunctions
                new CreateRaythaFunction.Command
                {
                    Name = string.Empty,
                    DeveloperName = string.Empty,
                    TriggerType = string.Empty,
                    Code = string.Empty,
                }.GetLogName(),
                new DeleteRaythaFunction.Command().GetLogName(),
                new EditRaythaFunction.Command
                {
                    Name = string.Empty,
                    TriggerType = string.Empty,
                    Code = string.Empty,
                }.GetLogName(),
                new RevertRaythaFunction.Command().GetLogName(),
                //Roles
                new CreateRole.Command().GetLogName(),
                new DeleteRole.Command().GetLogName(),
                new EditRole.Command().GetLogName(),
                //SitePages
                new CreateSitePage.Command().GetLogName(),
                new DeleteSitePage.Command().GetLogName(),
                new DeleteWidget.Command().GetLogName(),
                new DiscardDraftSitePage.Command().GetLogName(),
                new EditSitePage.Command().GetLogName(),
                new EditSitePageSettings.Command().GetLogName(),
                new EditWidget.Command().GetLogName(),
                new PublishSitePage.Command().GetLogName(),
                new RevertSitePage.Command().GetLogName(),
                new SaveWidgets.Command().GetLogName(),
                new SetSitePageAsHomePage.Command().GetLogName(),
                new UnpublishSitePage.Command().GetLogName(),
                //Themes
                new CreateTheme.Command
                {
                    Title = string.Empty,
                    DeveloperName = string.Empty,
                    Description = string.Empty,
                    InsertDefaultThemeMediaItems = false,
                }.GetLogName(),
                new DeleteTheme.Command().GetLogName(),
                new EditTheme.Command
                {
                    Title = string.Empty,
                    Description = string.Empty,
                }.GetLogName(),
                new ExportTheme.Command { DeveloperName = string.Empty }.GetLogName(),
                new SetAsActiveTheme.Command().GetLogName(),
                new ToggleThemeExportability.Command { IsExportable = false }.GetLogName(),
                new BeginDuplicateTheme.Command
                {
                    PathBase = string.Empty,
                    ThemeId = string.Empty,
                    Title = string.Empty,
                    DeveloperName = string.Empty,
                    Description = string.Empty,
                }.GetLogName(),
                new BeginImportThemeFromUrl.Command
                {
                    Title = string.Empty,
                    DeveloperName = string.Empty,
                    Description = string.Empty,
                    Url = string.Empty,
                }.GetLogName(),
                new BeginMatchWebTemplates.Command
                {
                    MatchedWebTemplateDeveloperNames = new Dictionary<string, string>(),
                }.GetLogName(),
                //UserGroups
                new CreateUserGroup.Command().GetLogName(),
                new DeleteUserGroup.Command().GetLogName(),
                new EditUserGroup.Command().GetLogName(),
                //Users
                new Application.Users.Commands.CreateUser.Command().GetLogName(),
                new DeleteUser.Command().GetLogName(),
                new EditUser.Command().GetLogName(),
                new Application.Users.Commands.ResetPassword.Command().GetLogName(),
                new Application.Users.Commands.SetIsActive.Command().GetLogName(),
                //Views
                new CreateView.Command().GetLogName(),
                new DeleteView.Command().GetLogName(),
                new EditFilter.Command().GetLogName(),
                new EditPublicSettings.Command().GetLogName(),
                new EditView.Command().GetLogName(),
                new Application.Views.Commands.SetAsHomePage.Command().GetLogName(),
                //WebTemplates
                new CreateWebTemplate.Command
                {
                    ThemeId = string.Empty,
                    DeveloperName = string.Empty,
                    Label = string.Empty,
                    Content = string.Empty,
                    TemplateAccessToModelDefinitions = Enumerable.Empty<ShortGuid>(),
                }.GetLogName(),
                new CreateWebTemplateByDeveloperName.Command
                {
                    DeveloperName = string.Empty,
                    Label = string.Empty,
                    Content = string.Empty,
                }.GetLogName(),
                new DeleteWebTemplate.Command().GetLogName(),
                new EditWebTemplate.Command
                {
                    Label = string.Empty,
                    Content = string.Empty,
                    TemplateAccessToModelDefinitions = Enumerable.Empty<ShortGuid>(),
                }.GetLogName(),
                new EditWebTemplateByDeveloperName.Command
                {
                    Label = string.Empty,
                    Content = string.Empty,
                }.GetLogName(),
                new RevertWebTemplate.Command().GetLogName(),
                //WidgetTemplates
                new EditWidgetTemplate.Command
                {
                    Label = string.Empty,
                    Content = string.Empty,
                }.GetLogName(),
                new RevertWidgetTemplate.Command().GetLogName(),
            };

            logCategories.Sort();
            var dropDownList = logCategories.ToDictionary(k => k, k => k);

            return dropDownList;
        }
    }

    public ListViewModel<AuditLogsListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string category = "",
        string startDate = "",
        string endDate = "",
        string emailAddress = "",
        string entityId = "",
        string orderBy = $"CreationTime desc",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Audit Log",
                RouteName = RouteNames.AuditLogs.Index,
                IsActive = true,
                Icon = SidebarIcons.AuditLogs,
            }
        );

        DateTime? startDateAsUtc = DateTimeExtensions.GetDateFromString(startDate);
        DateTime? endDateAsUtc = DateTimeExtensions.GetDateFromString(endDate);

        if (startDateAsUtc.HasValue)
            startDateAsUtc = CurrentOrganization.TimeZoneConverter.UtcToTimeZone(
                startDateAsUtc.Value
            );

        if (endDateAsUtc.HasValue)
            endDateAsUtc = CurrentOrganization.TimeZoneConverter.UtcToTimeZone(endDateAsUtc.Value);

        emailAddress = emailAddress?.Trim();
        entityId = entityId?.Trim();

        ShortGuid shortGuid = null;
        if (!ShortGuid.TryParse(entityId, out shortGuid) && !string.IsNullOrEmpty(entityId))
        {
            SetErrorMessage($"{entityId} is not a valid unique entity ID");
        }

        var input = new GetAuditLogs.Query
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            StartDateAsUtc = startDateAsUtc,
            EndDateAsUtc = endDateAsUtc,
            Category = category,
            EmailAddress = emailAddress,
            EntityId = shortGuid,
        };

        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            SetErrorMessage(response.Error);
            ListView = new ListViewModel<AuditLogsListItemViewModel>(
                new List<AuditLogsListItemViewModel>(),
                0
            );
        }
        else
        {
            var items = response.Result.Items.Select(p => new AuditLogsListItemViewModel
            {
                Id = p.Id,
                CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.CreationTime
                ),
                Category = p.Category,
                UserEmail = p.UserEmail,
                EntityId =
                    p.EntityId.HasValue && p.EntityId.Value != ShortGuid.Empty
                        ? p.EntityId.Value
                        : "N/A",
                Request = PrettyPrintJson(p.Request),
                IpAddress = p.IpAddress,
            });
            ListView = new ListViewModel<AuditLogsListItemViewModel>(
                items,
                response.Result.TotalCount
            );
        }
        StartDate = startDateAsUtc;
        EndDate = endDateAsUtc;
        Category = category;
        EntityId = entityId;
        UserEmail = emailAddress;
        ListView.PageNumber = pageNumber;
        ListView.PageSize = pageSize;
        ListView.PageName = "Index";

        if (!string.IsNullOrEmpty(orderBy))
        {
            ListView.OrderByPropertyName = orderBy.Split(' ')[0];
            ListView.OrderByDirection = orderBy.Split(' ')[1];
        }
        return Page();
    }

    [Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
    public async Task<IActionResult> OnPostClearAllAsync()
    {
        var input = new ClearAllAuditLogs.Command();
        var response = await Mediator.Send(input);

        if (!response.Success)
        {
            SetErrorMessage(response.Error);
        }
        else
        {
            SetSuccessMessage("All audit logs have been cleared successfully.");
        }

        return RedirectToPage("Index");
    }

    private static string PrettyPrintJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            var jsonDocument = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(
                jsonDocument,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch
        {
            // If it's not valid JSON, return as-is
            return json;
        }
    }

    public record AuditLogsListItemViewModel
    {
        public ShortGuid Id { get; set; }
        public string CreationTime { get; set; }
        public string Category { get; set; }
        public string UserEmail { get; set; }
        public string EntityId { get; set; }
        public string Request { get; set; }
        public string IpAddress { get; set; }
    }
}

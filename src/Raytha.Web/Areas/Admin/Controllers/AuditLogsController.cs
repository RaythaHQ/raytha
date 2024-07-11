using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Admins.Commands;
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
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.Users.Commands;
using Raytha.Application.Views.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.AuditLogs;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION)]
public class AuditLogsController : BaseController
{
    [Route(RAYTHA_ROUTE_PREFIX + "/audit-logs", Name = "auditlogsindex")]
    public async Task<IActionResult> Index(
        string category = "", 
        string startDate = "", 
        string endDate = "", 
        string emailAddress = "", 
        string entityId = "", 
        string orderBy = $"CreationTime {SortOrder.DESCENDING}", 
        int pageNumber = 1, 
        int pageSize = 50)
    {
        DateTime? startDateAsUtc = DateTimeExtensions.GetDateFromString(startDate);
        DateTime? endDateAsUtc = DateTimeExtensions.GetDateFromString(endDate); 

        if (startDateAsUtc.HasValue)
            startDateAsUtc = CurrentOrganization.TimeZoneConverter.UtcToTimeZone(startDateAsUtc.Value);

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
            EntityId = shortGuid
        };

        var response = await Mediator.Send(input);
        AuditLogsPagination_ViewModel viewModel;
        if (!response.Success)
        {
            SetErrorMessage(response.Error);
            viewModel = new AuditLogsPagination_ViewModel(new List<AuditLogsListItem_ViewModel>(), 0);
        }
        else
        {
            var items = response.Result.Items.Select(p => new AuditLogsListItem_ViewModel
            {
                Id = p.Id,
                CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime),
                Category = p.Category,
                UserEmail = p.UserEmail,
                EntityId = p.EntityId.HasValue && p.EntityId.Value != ShortGuid.Empty ? p.EntityId.Value : "N/A",
                Request = p.Request,
                IpAddress = p.IpAddress
            });
            viewModel = new AuditLogsPagination_ViewModel(items, response.Result.TotalCount);
        }
        viewModel.StartDate = startDateAsUtc;
        viewModel.EndDate = endDateAsUtc;
        viewModel.Category = category;
        viewModel.EntityId = entityId;
        viewModel.UserEmail = emailAddress;
        viewModel.PageNumber = pageNumber;
        viewModel.PageSize = pageSize;
        viewModel.SetOrderFromString(orderBy);
        viewModel.ActionName = "Index";

        viewModel.LogCategories = GetLogCategories();
        return View(viewModel);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Audit Log";
    }

    private Dictionary<string, string> GetLogCategories()
    {
        var logCategories = new List<string>()
        {
            //Admins
            new CreateAdmin.Command().GetLogName(),
            new DeleteAdmin.Command().GetLogName(),
            new EditAdmin.Command().GetLogName(),
            new RemoveAdminAccess.Command().GetLogName(),
            new Application.Admins.Commands.ResetPassword.Command().GetLogName(),
            new Application.Admins.Commands.SetIsActive.Command().GetLogName(),

            //AuthenticationSchemes
            new CreateAuthenticationScheme.Command().GetLogName(),
            new DeleteAuthenticationScheme.Command().GetLogName(),
            new EditAuthenticationScheme.Command().GetLogName(),

            //ContentItems
            new BeginExportContentItemsToCsv.Command().GetLogName(),
            new BeginImportContentItemsFromCsv.Command().GetLogName(),
            new CreateContentItem.Command().GetLogName(),
            new DeleteAlreadyDeletedContentItem.Command().GetLogName(),
            new DeleteContentItem.Command().GetLogName(),
            new DiscardDraftContentItem.Command().GetLogName(),
            new EditContentItem.Command().GetLogName(),
            new EditContentItemSettings.Command().GetLogName(),
            new RestoreContentItem.Command().GetLogName(),
            new RevertContentItem.Command().GetLogName(),
            new Application.ContentItems.Commands.SetAsHomePage.Command().GetLogName(),
            new UnpublishContentItem.Command().GetLogName(),

            //ContentTypes
            new CreateContentType.Command().GetLogName(),
            new CreateContentTypeField.Command().GetLogName(),
            new DeleteContentTypeField.Command().GetLogName(),
            new EditContentType.Command().GetLogName(),
            new EditContentTypeField.Command().GetLogName(),
            new ReorderContentTypeField.Command().GetLogName(),

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

            //OrganizationSettings
            new EditConfiguration.Command().GetLogName(),
            new EditSmtp.Command().GetLogName(),
            new InitialSetup.Command().GetLogName(),

            //Roles
            new CreateRole.Command().GetLogName(),
            new DeleteRole.Command().GetLogName(),
            new EditRole.Command().GetLogName(),

            //Email-Templates
            new EditEmailTemplate.Command().GetLogName(),
            new RevertEmailTemplate.Command().GetLogName(),

            //Themes
            BeginDuplicateTheme.Command.Empty().GetLogName(),
            BeginImportThemeFromUrl.Command.Empty().GetLogName(),
            BeginMatchWebTemplates.Command.Empty().GetLogName(),
            CreateTheme.Command.Empty().GetLogName(),
            new DeleteTheme.Command().GetLogName(),
            EditTheme.Command.Empty().GetLogName(),
            ExportTheme.Command.Empty().GetLogName(),
            new SetAsActiveTheme.Command().GetLogName(),
            ToggleThemeExportability.Command.Empty().GetLogName(),

            //Web-Templates
            CreateWebTemplate.Command.Empty().GetLogName(),
            new DeleteWebTemplate.Command().GetLogName(),
            EditWebTemplate.Command.Empty().GetLogName(),
            new RevertWebTemplate.Command().GetLogName(),

            //Menus
            CreateNavigationMenu.Command.Empty().GetLogName(),
            CreateNavigationMenuRevision.Command.Empty().GetLogName(),
            EditNavigationMenu.Command.Empty().GetLogName(),
            new DeleteNavigationMenu.Command().GetLogName(),
            new RevertNavigationMenu.Command().GetLogName(),
            new SetAsMainMenu.Command().GetLogName(),

            //MenuItems
            CreateNavigationMenuItem.Command.Empty().GetLogName(),
            DeleteNavigationMenuItem.Command.Empty().GetLogName(),
            EditNavigationMenuItem.Command.Empty().GetLogName(),
            new ReorderNavigationMenuItems.Command().GetLogName(),

            //Functions
            CreateRaythaFunction.Command.Empty().GetLogName(),
            EditRaythaFunction.Command.Empty().GetLogName(),
            new DeleteRaythaFunction.Command().GetLogName(),
            new RevertRaythaFunction.Command().GetLogName(),

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
        };

        logCategories.Sort();
        var dropDownList = logCategories.ToDictionary(k => k, k => k);

        return dropDownList;
    }
}

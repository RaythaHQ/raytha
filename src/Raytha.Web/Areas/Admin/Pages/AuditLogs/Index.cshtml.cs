using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Application.AuditLogs.Queries;
using Raytha.Application.AuthenticationSchemes.Commands;
using Raytha.Application.Common.Utils;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.Login.Commands;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Application.Roles.Commands;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.Users.Commands;
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
                new DeleteAdmin.Command().GetLogName(),
                new EditAdmin.Command().GetLogName(),
                new RemoveAdminAccess.Command().GetLogName(),
                new Application.Admins.Commands.ResetPassword.Command().GetLogName(),
                new Application.Admins.Commands.SetIsActive.Command().GetLogName(),
                //AuthenticationSchemes
                new CreateAuthenticationScheme.Command().GetLogName(),
                new DeleteAuthenticationScheme.Command().GetLogName(),
                new EditAuthenticationScheme.Command().GetLogName(),
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
                Label = "Audit Logs",
                RouteName = RouteNames.AuditLogs.Index,
                IsActive = true,
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
                Request = p.Request,
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

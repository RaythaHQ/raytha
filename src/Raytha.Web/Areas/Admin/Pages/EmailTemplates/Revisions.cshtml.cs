using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.EmailTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.EmailTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Revisions
    : BaseAdminPageModel,
        IHasListView<Revisions.EmailTemplatesRevisionListItemViewModel>,
        ISubActionViewModel
{
    public string Id { get; set; }
    public ListViewModel<EmailTemplatesRevisionListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string id,
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Email Templates",
                RouteName = RouteNames.EmailTemplates.Index,
                IsActive = false,
                Icon = SidebarIcons.EmailTemplates,
            },
            new BreadcrumbNode
            {
                Label = "Revisions",
                RouteName = RouteNames.EmailTemplates.Revisions,
                IsActive = true,
            }
        );

        var template = await Mediator.Send(new GetEmailTemplateById.Query { Id = id });

        var input = new GetEmailTemplateRevisionsByTemplateId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new EmailTemplatesRevisionListItemViewModel
        {
            Id = p.Id,
            Subject = p.Subject,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            Content = p.Content,
        });

        Id = template.Result.Id;

        ListView = new ListViewModel<EmailTemplatesRevisionListItemViewModel>(
            items,
            response.Result.TotalCount
        );
        return Page();
    }

    public async Task<IActionResult> OnPostRevert(string id, string revisionId)
    {
        var input = new RevertEmailTemplate.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Template has been reverted.");
        }
        else
        {
            SetErrorMessage("There was an error reverting this template", response.GetErrors());
        }
        return RedirectToPage(RouteNames.EmailTemplates.Edit, new { id });
    }

    public record EmailTemplatesRevisionListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Created at")]
        public string CreationTime { get; init; }

        [Display(Name = "Subject")]
        public string Subject { get; init; }

        [Display(Name = "Created by")]
        public string CreatorUser { get; init; }

        [Display(Name = "Content")]
        public string Content { get; init; }
    }
}

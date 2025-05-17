using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.EmailTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.EmailTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.EmailTemplatesListItemViewModel>
{
    public ListViewModel<EmailTemplatesListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Subject {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetEmailTemplates.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new EmailTemplatesListItemViewModel
        {
            Id = p.Id,
            Subject = p.Subject,
            DeveloperName = p.DeveloperName,
            LastModificationTime =
                CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.LastModificationTime
                ),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
        });

        ListView = new ListViewModel<EmailTemplatesListItemViewModel>(
            items,
            response.Result.TotalCount
        );
        return Page();
    }

    public record EmailTemplatesListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Subject")]
        public string Subject { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Last modified at")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Last modified by")]
        public string LastModifierUser { get; init; }
    }
}

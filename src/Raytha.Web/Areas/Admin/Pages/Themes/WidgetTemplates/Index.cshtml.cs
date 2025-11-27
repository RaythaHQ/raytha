using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.Themes.WidgetTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WidgetTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public WidgetTemplatesPaginationViewModel ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string themeId,
        string orderBy = $"Label {SortOrder.ASCENDING}",
        string search = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Themes",
                RouteName = RouteNames.Themes.Index,
                IsActive = false,
                Icon = SidebarIcons.Themes,
            },
            new BreadcrumbNode
            {
                Label = "Widget templates",
                RouteName = RouteNames.Themes.WidgetTemplates.Index,
                IsActive = true,
            }
        );

        var input = new GetWidgetTemplates.Query
        {
            ThemeId = themeId,
            OrderBy = orderBy,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new WidgetTemplatesListItemViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            LastModificationTime =
                CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.LastModificationTime
                ),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
            IsBuiltInTemplate = p.IsBuiltInTemplate.YesOrNo(),
        });

        ListView = new WidgetTemplatesPaginationViewModel(
            items,
            response.Result.TotalCount,
            themeId
        );
        ListView.PageNumber = pageNumber;
        ListView.PageSize = pageSize;
        ListView.Search = search;
        (ListView.OrderByPropertyName, ListView.OrderByDirection) =
            orderBy.SplitIntoColumnAndSortOrder();
        ListView.PageName = "Index";

        return Page();
    }

    public record WidgetTemplatesPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<WidgetTemplatesListItemViewModel> Items { get; }
        public string ThemeId { get; }

        public WidgetTemplatesPaginationViewModel(
            IEnumerable<WidgetTemplatesListItemViewModel> items,
            int totalCount,
            string themeId
        )
            : base(totalCount)
        {
            Items = items;
            ThemeId = themeId;
        }
    }

    public record WidgetTemplatesListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Last modified at")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Last modified by")]
        public string LastModifierUser { get; init; }

        [Display(Name = "Is built in template")]
        public string IsBuiltInTemplate { get; init; }
    }
}

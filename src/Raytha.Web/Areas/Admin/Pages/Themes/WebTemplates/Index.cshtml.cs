using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WebTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public WebTemplatesPaginationViewModel ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string themeId,
        string orderBy = $"Label {SortOrder.ASCENDING}",
        string search = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Themes", RouteName = RouteNames.Themes.Index, IsActive = false },
            new BreadcrumbNode { Label = "Web Templates", RouteName = RouteNames.Themes.WebTemplates.Index, IsActive = true }
        );

        var input = new GetWebTemplates.Query
        {
            ThemeId = themeId,
            OrderBy = orderBy,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new WebTemplatesListItemViewModel
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
            ParentTemplate =
                p.ParentTemplate != null
                    ? new WebTemplatesListItemViewModel.ParentTemplateViewModel
                    {
                        Id = p.ParentTemplate.Id,
                        Label = p.ParentTemplate.Label,
                    }
                    : null,
        });

        ListView = new WebTemplatesPaginationViewModel(items, response.Result.TotalCount, themeId);
        ListView.PageNumber = pageNumber;
        ListView.PageSize = pageSize;
        ListView.Search = search;
        (ListView.OrderByPropertyName, ListView.OrderByDirection) =
            orderBy.SplitIntoColumnAndSortOrder();
        ListView.PageName = "Index";

        return Page();
    }

    public record WebTemplatesPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<WebTemplatesListItemViewModel> Items { get; }
        public string ThemeId { get; }

        public WebTemplatesPaginationViewModel(
            IEnumerable<WebTemplatesListItemViewModel> items,
            int totalCount,
            string themeId
        )
            : base(totalCount)
        {
            Items = items;
            ThemeId = themeId;
        }
    }

    public record WebTemplatesListItemViewModel
    {
        public string Id { get; init; }
        public string ThemeId { get; set; }

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

        public ParentTemplateViewModel ParentTemplate { get; init; }

        //helpers
        public record ParentTemplateViewModel
        {
            public string Id { get; init; }
            public string Label { get; init; }
        }
    }
}

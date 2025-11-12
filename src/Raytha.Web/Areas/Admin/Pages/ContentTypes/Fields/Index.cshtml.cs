using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Fields;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class Index : BaseContentTypeContextPageModel
{
    public FieldsPaginationViewModel ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"FieldOrder {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        bool showDeletedOnly = false
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = CurrentView.ContentType.LabelPlural,
                RouteName = RouteNames.ContentItems.Index,
                RouteValues = new Dictionary<string, string>
                {
                    { "contentTypeDeveloperName", CurrentView.ContentType.DeveloperName },
                },
                IsActive = false,
                Icon = SidebarIcons.ContentItems,
            },
            new BreadcrumbNode
            {
                Label = "Fields",
                RouteName = RouteNames.ContentTypes.Fields.Index,
                IsActive = true,
            }
        );

        var input = new GetContentTypeFields.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            DeveloperName = CurrentView.ContentType.DeveloperName,
            ShowDeletedOnly = showDeletedOnly,
        };
        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new FieldsListItemViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            FieldType = p.FieldType,
            IsRequired = p.IsRequired.YesOrNo(),
        });

        ListView = new FieldsPaginationViewModel(
            items,
            response.Result.TotalCount,
            showDeletedOnly
        );
        ListView.PageNumber = pageNumber;
        ListView.PageSize = pageSize;
        (ListView.OrderByPropertyName, ListView.OrderByDirection) =
            orderBy.SplitIntoColumnAndSortOrder();
        ListView.Search = search;

        return Page();
    }

    public record FieldsPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<FieldsListItemViewModel> Items { get; }
        public bool ShowDeletedOnly { get; }

        public FieldsPaginationViewModel(
            IEnumerable<FieldsListItemViewModel> items,
            int totalCount,
            bool showDeletedOnly
        )
            : base(totalCount)
        {
            Items = items;
            ShowDeletedOnly = showDeletedOnly;
        }
    }

    public record FieldsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Field type")]
        public string FieldType { get; init; }

        [Display(Name = "Is required")]
        public string IsRequired { get; init; }
    }
}

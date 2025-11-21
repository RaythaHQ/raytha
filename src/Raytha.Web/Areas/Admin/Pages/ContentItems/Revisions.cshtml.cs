using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Revisions
    : BaseHasFavoriteViewsPageModel,
        IHasListView<Revisions.ContentItemsRevisionsListItemViewModel>,
        ISubActionViewModel
{
    public ListViewModel<ContentItemsRevisionsListItemViewModel> ListView { get; set; }
    ViewDto ISubActionViewModel.CurrentView => base.CurrentView;
    public string Id { get; set; }
    public string? RoutePath { get; set; }

    public async Task<IActionResult> OnGet(
        string id,
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
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
                Label = CurrentView.Label,
                RouteName = RouteNames.ContentItems.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "View revisions",
                RouteName = RouteNames.ContentItems.Revisions,
                IsActive = true,
            }
        );

        var contentItemResponse = await Mediator.Send(new GetContentItemById.Query { Id = id });
        RoutePath = contentItemResponse.Result.RoutePath;

        var input = new GetContentItemRevisionsByContentItemId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new ContentItemsRevisionsListItemViewModel
        {
            Id = p.Id,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            ContentAsJson = JsonSerializer.Serialize(
                p.PublishedContent,
                new JsonSerializerOptions { WriteIndented = true }
            ),
        });

        ListView = new ListViewModel<ContentItemsRevisionsListItemViewModel>(
            items,
            response.Result.TotalCount
        );

        Id = id;

        return Page();
    }

    public async Task<IActionResult> OnPostRevert(string id, string revisionId)
    {
        var input = new RevertContentItem.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been reverted.");
            return RedirectToPage(
                RouteNames.ContentItems.Edit,
                new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
        return RedirectToPage(
            RouteNames.ContentItems.Revisions,
            new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id }
        );
    }

    public record ContentItemsRevisionsListItemViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Created at")]
        public string CreationTime { get; set; }

        [Display(Name = "Created by")]
        public string CreatorUser { get; set; }

        public string ContentAsJson { get; set; }
    }
}

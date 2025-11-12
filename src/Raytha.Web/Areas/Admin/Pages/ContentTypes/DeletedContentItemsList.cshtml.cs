using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class DeletedContentItemsList
    : BaseContentTypeContextPageModel,
        IHasListView<DeletedContentItemsList.DeletedContentItemsListItemViewModel>
{
    public ListViewModel<DeletedContentItemsListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
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
                Label = "Deleted Items",
                RouteName = RouteNames.ContentTypes.DeletedContentItemsList,
                IsActive = true,
            }
        );

        var input = new GetDeletedContentItems.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            DeveloperName = CurrentView.ContentType.DeveloperName,
        };
        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new DeletedContentItemsListItemViewModel
        {
            Id = p.Id,
            PrimaryField = p.PrimaryField,
            DeletedBy = p.CreatorUser?.FullName ?? "N/A",
            DeletionTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            OriginalContentItemId = p.OriginalContentItemId,
        });

        ListView = new ListViewModel<DeletedContentItemsListItemViewModel>(
            items,
            response.Result.TotalCount
        );

        return Page();
    }

    public async Task<IActionResult> OnPostRestore(string id)
    {
        var input = new RestoreContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} has been restored.");
            return RedirectToPage(
                RouteNames.ContentItems.Edit,
                new
                {
                    id = response.Result.ToString(),
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                }
            );
        }
        else
        {
            SetErrorMessage(
                $"There was an error restoring this {CurrentView.ContentType.LabelSingular.ToLower()}",
                response.GetErrors()
            );
            return RedirectToPage(
                RouteNames.ContentTypes.DeletedContentItemsList,
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
    }

    public async Task<IActionResult> OnPostClear(string id)
    {
        var input = new DeleteAlreadyDeletedContentItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage(
                $"{CurrentView.ContentType.LabelSingular} has been permanently removed."
            );
        }
        else
        {
            SetErrorMessage(
                $"There was an error permanently removing this {CurrentView.ContentType.LabelSingular.ToLower()}",
                response.GetErrors()
            );
        }
        return RedirectToPage(
            RouteNames.ContentTypes.DeletedContentItemsList,
            new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
        );
    }

    public class DeletedContentItemsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Primary field")]
        public string PrimaryField { get; init; }

        [Display(Name = "Original id")]
        public string OriginalContentItemId { get; init; }

        [Display(Name = "Deleted by")]
        public string DeletedBy { get; init; }

        [Display(Name = "Deleted at")]
        public string DeletionTime { get; init; }
    }
}

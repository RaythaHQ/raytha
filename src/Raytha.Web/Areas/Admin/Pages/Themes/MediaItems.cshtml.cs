using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.MediaItems.Commands;
using Raytha.Application.Themes.MediaItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class MediaItems : BaseAdminPageModel, ISubActionViewModel
{
    public ListViewModel<ThemesMediaItemListItemViewModel> ListView { get; set; }
    public string ThemeId { get; set; }
    public string Id { get; set; }
    public bool IsActive { get; set; }

    public async Task<IActionResult> OnGet(string themeId)
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
                Label = "Media Items",
                RouteName = RouteNames.Themes.MediaItems,
                IsActive = true,
            }
        );

        var input = new GetMediaItemsByThemeId.Query { ThemeId = themeId };

        var response = await Mediator.Send(input);
        var items = response.Result.Select(mi => new ThemesMediaItemListItemViewModel
        {
            Id = mi.Id,
            FileName = mi.FileName,
            ContentType = mi.ContentType,
            FileStorageProvider = mi.FileStorageProvider,
            ObjectKey = mi.ObjectKey,
        });

        ListView = new ListViewModel<ThemesMediaItemListItemViewModel>(
            items,
            response.Result.Count()
        );

        ThemeId = themeId;
        Id = themeId;
        IsActive = CurrentOrganization.ActiveThemeId == themeId;

        return Page();
    }

    public async Task<IActionResult> OnPostDelete(string themeId, string id)
    {
        var input = new DeleteMediaItem.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Media item has been deleted.");
        }
        else
        {
            SetErrorMessage("There was an error deleting this media item", response.GetErrors());
        }
        return RedirectToPage(RouteNames.Themes.MediaItems, new { themeId });
    }

    public record ThemesMediaItemListItemViewModel
    {
        public string Id { get; init; }
        public string FileName { get; init; }
        public string ContentType { get; init; }
        public string FileStorageProvider { get; init; }
        public string ObjectKey { get; init; }
    }
}

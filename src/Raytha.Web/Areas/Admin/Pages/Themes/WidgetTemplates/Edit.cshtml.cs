using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.MediaItems;
using Raytha.Application.Themes.MediaItems.Queries;
using Raytha.Application.Themes.WidgetTemplates.Commands;
using Raytha.Application.Themes.WidgetTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WidgetTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string ThemeId { get; set; }
    public string Id { get; set; }

    public async Task<IActionResult> OnGet(string themeId, string id)
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
                Label = "Widget Templates",
                RouteName = RouteNames.Themes.WidgetTemplates.Index,
                RouteValues = new Dictionary<string, string> { ["themeId"] = themeId },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Edit widget template",
                RouteName = RouteNames.Themes.WidgetTemplates.Edit,
                IsActive = true,
            }
        );

        ThemeId = themeId;
        Id = id;

        var templateResponse = await Mediator.Send(new GetWidgetTemplateById.Query { Id = id });

        var mediaItemsResponse = await Mediator.Send(
            new GetMediaItemsByThemeId.Query { ThemeId = themeId }
        );

        Form = new FormModel
        {
            Id = templateResponse.Result.Id,
            Content = templateResponse.Result.Content,
            Label = templateResponse.Result.Label,
            DeveloperName = templateResponse.Result.DeveloperName,
            IsBuiltInTemplate = templateResponse.Result.IsBuiltInTemplate,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            MaxFileSize = FileStorageProviderSettings.MaxFileSize,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            PathBase = CurrentOrganization.PathBase,
            ThemeId = themeId,
            MediaItems = mediaItemsResponse.Result,
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                templateResponse.Result.LastModificationTime
            ),
            LastModifierUser = templateResponse.Result.LastModifierUser?.FullName ?? "N/A",
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(string themeId, string id)
    {
        var input = new EditWidgetTemplate.Command
        {
            Id = id,
            Label = Form.Label,
            Content = Form.Content,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was edited successfully.");
            return RedirectToPage(
                RouteNames.Themes.WidgetTemplates.Edit,
                new { themeId, id = response.Result }
            );
        }
        else
        {
            var mediaItemsResponse = await Mediator.Send(
                new GetMediaItemsByThemeId.Query { ThemeId = themeId }
            );

            Form.MediaItems = mediaItemsResponse.Result;
            Form.ThemeId = themeId;
            Form.AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes;
            Form.MaxFileSize = FileStorageProviderSettings.MaxFileSize;
            Form.UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud;
            Form.PathBase = CurrentOrganization.PathBase;

            SetErrorMessage(
                "There was an error attempting to update this template. See the error below.",
                response.GetErrors()
            );

            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }
        public string ThemeId { get; set; }

        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; }

        public IEnumerable<MediaItemDto> MediaItems { get; set; }

        // Helpers
        public long MaxFileSize { get; set; }
        public string AllowedMimeTypes { get; set; }
        public bool UseDirectUploadToCloud { get; set; }
        public string PathBase { get; set; }
        public bool IsBuiltInTemplate { get; set; }
        public string LastModificationTime { get; set; }
        public string LastModifierUser { get; set; }
    }
}


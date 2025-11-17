using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentItems;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Settings : BaseHasFavoriteViewsPageModel, ISubActionViewModel
{
    public string Id { get; set; }
    public string? RoutePath { get; set; }
    ViewDto ISubActionViewModel.CurrentView => base.CurrentView;
    private FieldValueConverter _fieldValueConverter;

    protected FieldValueConverter FieldValueConverter =>
        _fieldValueConverter ??=
            HttpContext.RequestServices.GetRequiredService<FieldValueConverter>();
    public string WebsiteUrl { get; set; }
    public Dictionary<string, string> ContentTypeFields { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
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
                Label = "Settings",
                RouteName = RouteNames.ContentItems.Settings,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetContentItemById.Query { Id = id });
        RoutePath = response.Result.RoutePath;

        var webTemplates = await Mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue,
            }
        );

        var webTemplateResponse = await Mediator.Send(
            new GetWebTemplateByContentItemId.Query
            {
                ContentItemId = id,
                ThemeId = CurrentOrganization.ActiveThemeId,
            }
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            TemplateId = webTemplateResponse.Result.Id,
            IsHomePage = CurrentOrganization.HomePageId == response.Result.Id,
            RoutePath = response.Result.RoutePath,
            WebsiteUrl =
                CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/",
            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(
                p => p.Id.ToString(),
                p => p.Label
            ),
        };

        Id = id;

        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditContentItemSettings.Command
        {
            Id = id,
            TemplateId = Form.TemplateId,
            RoutePath = Form.RoutePath,
        };

        var editResponse = await Mediator.Send(input);
        if (editResponse.Success)
        {
            SetSuccessMessage($"{CurrentView.ContentType.LabelSingular} was edited successfully.");
            return RedirectToPage(
                "/ContentItems/Settings",
                new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this page. See the error below.",
                editResponse.GetErrors()
            );

            var webTemplatesResponse = await Mediator.Send(
                new GetWebTemplates.Query
                {
                    ThemeId = CurrentOrganization.ActiveThemeId,
                    ContentTypeId = CurrentView.ContentTypeId,
                    PageSize = int.MaxValue,
                }
            );

            Form.AvailableTemplates = webTemplatesResponse.Result?.Items.ToDictionary(
                p => p.Id.ToString(),
                p => p.Label
            );
            Form.WebsiteUrl =
                CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";

            Id = id;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSetAsHomePage(string id)
    {
        var authorizationService =
            HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var authResult = await authorizationService.AuthorizeAsync(
            User,
            BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
        );

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var input = new SetAsHomePage.Command { Id = id };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage(
                $"{CurrentView.ContentType.LabelSingular} set as home page successfully."
            );
            return RedirectToPage(
                RouteNames.ContentItems.Settings,
                new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
            return RedirectToPage(
                RouteNames.ContentItems.Settings,
                new { id, contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Template")]
        public string TemplateId { get; set; }

        [Display(Name = "Route path")]
        public string RoutePath { get; set; }

        public bool IsHomePage { get; set; }

        //helpers
        public string WebsiteUrl { get; set; }
        public Dictionary<string, string> AvailableTemplates { get; set; }
    }
}

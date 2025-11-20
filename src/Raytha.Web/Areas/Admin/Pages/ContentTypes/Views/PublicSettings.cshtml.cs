using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class PublicSettings : BaseContentTypeContextPageModel
{
    private readonly IAuthorizationService _authorizationService;

    public PublicSettings(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public string BackToListUrl { get; set; } = string.Empty;

    [BindProperty]
    public string TemplateId { get; set; } = string.Empty;

    [BindProperty]
    public string RoutePath { get; set; } = string.Empty;

    [BindProperty]
    public bool IsPublished { get; set; }

    [BindProperty]
    public int DefaultNumberOfItemsPerPage { get; set; }

    [BindProperty]
    public int MaxNumberOfItemsPerPage { get; set; }

    [BindProperty]
    public bool IgnoreClientFilterAndSortQueryParams { get; set; }

    public Dictionary<string, string> AvailableTemplates { get; set; } = new();
    public string WebsiteUrl { get; set; } = string.Empty;
    public bool IsHomePage { get; set; }
    public bool CanManageSystemSettings { get; set; }

    public async Task<IActionResult> OnGet(string viewId, string backToListUrl = "")
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = CurrentView!.ContentType.LabelPlural,
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
                Label = "Views",
                RouteName = RouteNames.ContentTypes.Views.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Edit public settings",
                RouteName = RouteNames.ContentTypes.Views.PublicSettings,
                IsActive = true,
            }
        );

        await LoadPublicSettings();
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string viewId, string backToListUrl = "")
    {
        var input = new EditPublicSettings.Command
        {
            Id = viewId,
            RoutePath = RoutePath,
            IsPublished = IsPublished,
            TemplateId = TemplateId,
            DefaultNumberOfItemsPerPage = DefaultNumberOfItemsPerPage,
            MaxNumberOfItemsPerPage = MaxNumberOfItemsPerPage,
            IgnoreClientFilterAndSortQueryParams = IgnoreClientFilterAndSortQueryParams,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage("Public settings updated successfully.");
            return RedirectToPage(
                RouteNames.ContentTypes.Views.PublicSettings,
                new
                {
                    contentTypeDeveloperName = CurrentView!.ContentType.DeveloperName,
                    viewId = viewId,
                    backToListUrl = backToListUrl,
                }
            );
        }

        SetErrorMessage(
            "There were validation errors with your form submission. Please correct the fields below.",
            response.GetErrors()
        );

        await LoadPublicSettings();
        return Page();
    }

    public async Task<IActionResult> OnPostSetAsHomePageAsync(
        string viewId,
        string backToListUrl = ""
    )
    {
        // Check authorization manually since Authorize attribute doesn't work on handler methods
        var authResult = await _authorizationService.AuthorizeAsync(
            User,
            BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
        );

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var input = new SetAsHomePage.Command { Id = viewId };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Home page updated successfully.");
        }
        else
        {
            SetErrorMessage(
                "There was an error setting this as the home page.",
                response.GetErrors()
            );
        }

        return RedirectToPage(
            RouteNames.ContentTypes.Views.PublicSettings,
            new
            {
                contentTypeDeveloperName = CurrentView!.ContentType.DeveloperName,
                viewId = viewId,
                backToListUrl = backToListUrl,
            }
        );
    }

    private async Task LoadPublicSettings()
    {
        var viewResponse = await Mediator.Send(new GetViewById.Query { Id = CurrentView!.Id });

        var webTemplates = await Mediator.Send(
            new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue,
            }
        );

        var webTemplateIdResponse = await Mediator.Send(
            new GetWebTemplateByViewId.Query
            {
                ViewId = CurrentView.Id,
                ThemeId = CurrentOrganization.ActiveThemeId,
            }
        );

        RoutePath = viewResponse.Result!.RoutePath;
        IsPublished = viewResponse.Result.IsPublished;
        TemplateId = webTemplateIdResponse.Result!.Id;
        AvailableTemplates =
            webTemplates.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label) ?? new();
        WebsiteUrl =
            CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
        IgnoreClientFilterAndSortQueryParams = viewResponse
            .Result
            .IgnoreClientFilterAndSortQueryParams;
        MaxNumberOfItemsPerPage = viewResponse.Result.MaxNumberOfItemsPerPage;
        DefaultNumberOfItemsPerPage = viewResponse.Result.DefaultNumberOfItemsPerPage;
        IsHomePage = CurrentOrganization.HomePageId == CurrentView.Id;

        var auth = await _authorizationService.AuthorizeAsync(
            User,
            BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
        );
        CanManageSystemSettings = auth.Succeeded;
    }
}

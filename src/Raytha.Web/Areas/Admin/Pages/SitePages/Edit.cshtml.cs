using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public IEnumerable<SelectListItem> AvailableTemplates { get; set; }
    public string Id { get; set; }
    public string? RoutePath { get; set; }
    public string Title { get; set; }
    public string CreatedAt { get; set; }
    public string LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; }
    public string WebsiteUrl { get; set; }
    public bool IsHomePage { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetSitePageById.Query { Id = id });

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Site Pages",
                RouteName = RouteNames.SitePages.Index,
                IsActive = false,
                Icon = SidebarIcons.SitePages,
            },
            new BreadcrumbNode
            {
                Label = response.Result.Title,
                RouteName = RouteNames.SitePages.Edit,
                IsActive = true,
            }
        );

        await LoadTemplates();

        Form = new FormModel
        {
            Id = response.Result.Id,
            Title = response.Result.Title,
            TemplateId = response.Result.WebTemplateId,
            RoutePath = response.Result.RoutePath,
            SaveAsDraft = false,  // Default to publish on save
            IsPublished = response.Result.IsPublished,
            IsDraft = response.Result.IsDraft,
        };

        // For ISubActionViewModel
        Id = response.Result.Id;
        Title = response.Result.Title;
        RoutePath = response.Result.RoutePath;
        WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
        IsHomePage = CurrentOrganization.HomePageId == response.Result.Id;
        CreatedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
            response.Result.CreationTime
        );
        LastModifiedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
            response.Result.LastModificationTime
        );
        LastModifiedBy = response.Result.LastModifierUser?.FullName ?? "N/A";

        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        // First update basic info
        var input = new EditSitePage.Command
        {
            Id = Form.Id,
            Title = Form.Title,
            TemplateId = Form.TemplateId,
            SaveAsDraft = Form.SaveAsDraft,
        };

        var response = await Mediator.Send(input);

        if (!response.Success)
        {
            SetErrorMessage(
                "There was an error updating this site page. See the error below.",
                response.GetErrors()
            );
            await ReloadPageData(id);
            return Page();
        }

        // Then update route path if changed
        var currentPage = await Mediator.Send(new GetSitePageById.Query { Id = id });
        if (currentPage.Result.RoutePath != Form.RoutePath)
        {
            var settingsInput = new EditSitePageSettings.Command
            {
                Id = id,
                RoutePath = Form.RoutePath,
            };
            var settingsResponse = await Mediator.Send(settingsInput);
            
            if (!settingsResponse.Success)
            {
                SetErrorMessage(
                    "Page saved but route path could not be updated. See the error below.",
                    settingsResponse.GetErrors()
                );
                await ReloadPageData(id);
                return Page();
            }
        }

        SetSuccessMessage($"'{Form.Title}' was updated successfully.");
        return RedirectToPage(RouteNames.SitePages.Edit, new { id });
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

        var input = new SetSitePageAsHomePage.Command { Id = id };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Site page set as home page successfully.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToPage(RouteNames.SitePages.Edit, new { id });
    }

    public async Task<IActionResult> OnPostUnpublish(string id)
    {
        var input = new UnpublishSitePage.Command { Id = id };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Site page has been unpublished.");
        }
        else
        {
            SetErrorMessage("There was an error unpublishing this site page.", response.GetErrors());
        }

        return RedirectToPage(RouteNames.SitePages.Edit, new { id });
    }

    public async Task<IActionResult> OnPostDiscardDraft(string id)
    {
        var input = new DiscardDraftSitePage.Command { Id = id };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Draft changes have been discarded.");
        }
        else
        {
            SetErrorMessage("There was an error discarding the draft.", response.GetErrors());
        }

        return RedirectToPage(RouteNames.SitePages.Edit, new { id });
    }

    private async Task ReloadPageData(string id)
    {
        await LoadTemplates();
        var pageResponse = await Mediator.Send(new GetSitePageById.Query { Id = id });
        
        // For ISubActionViewModel
        Id = pageResponse.Result.Id;
        Title = pageResponse.Result.Title;
        RoutePath = pageResponse.Result.RoutePath;
        WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
        IsHomePage = CurrentOrganization.HomePageId == pageResponse.Result.Id;
        
        CreatedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
            pageResponse.Result.CreationTime
        );
        LastModifiedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
            pageResponse.Result.LastModificationTime
        );
        LastModifiedBy = pageResponse.Result.LastModifierUser?.FullName ?? "N/A";
    }

    private async Task LoadTemplates()
    {
        var templatesResponse = await Mediator.Send(
            new GetWebTemplates.Query { ThemeId = CurrentOrganization.ActiveThemeId }
        );

        AvailableTemplates = templatesResponse.Result.Items
            .Where(t => !t.IsBaseLayout)
            .Select(t => new SelectListItem { Value = t.Id, Text = t.Label })
            .ToList();
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Title")]
        [Required]
        public string Title { get; set; }

        [Display(Name = "Template")]
        [Required]
        public string TemplateId { get; set; }

        [Display(Name = "Route path")]
        [Required]
        public string RoutePath { get; set; }

        [Display(Name = "Save as draft")]
        public bool SaveAsDraft { get; set; }

        public bool IsPublished { get; set; }
        
        public bool IsDraft { get; set; }
    }
}


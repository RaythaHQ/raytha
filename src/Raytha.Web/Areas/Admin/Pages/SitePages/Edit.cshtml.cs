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
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public IEnumerable<SelectListItem> AvailableTemplates { get; set; }
    public string RoutePath { get; set; }
    public string CreatedAt { get; set; }
    public string LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; }

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
            SaveAsDraft = response.Result.IsDraft,
            IsPublished = response.Result.IsPublished,
        };

        RoutePath = response.Result.RoutePath;
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
        var input = new EditSitePage.Command
        {
            Id = Form.Id,
            Title = Form.Title,
            TemplateId = Form.TemplateId,
            SaveAsDraft = Form.SaveAsDraft,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"'{Form.Title}' was updated successfully.");
            return RedirectToPage(RouteNames.SitePages.Edit, new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error updating this site page. See the error below.",
                response.GetErrors()
            );
            await LoadTemplates();

            // Reload metadata
            var pageResponse = await Mediator.Send(new GetSitePageById.Query { Id = id });
            RoutePath = pageResponse.Result.RoutePath;
            CreatedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                pageResponse.Result.CreationTime
            );
            LastModifiedAt = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                pageResponse.Result.LastModificationTime
            );
            LastModifiedBy = pageResponse.Result.LastModifierUser?.FullName ?? "N/A";

            return Page();
        }
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

        [Display(Name = "Save as draft")]
        public bool SaveAsDraft { get; set; }

        public bool IsPublished { get; set; }
    }
}


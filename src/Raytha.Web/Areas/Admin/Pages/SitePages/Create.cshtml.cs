using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.SitePages.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class Create : SitePageTemplatePageModel
{
    public Create(ISitePageTemplateOptionsProvider templateOptionsProvider)
        : base(templateOptionsProvider)
    {
    }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
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
                Label = "Create site page",
                RouteName = RouteNames.SitePages.Create,
                IsActive = true,
            }
        );

        await LoadTemplateOptionsAsync(HttpContext.RequestAborted);

        Form = new FormModel { SaveAsDraft = false };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateSitePage.Command
        {
            Title = Form.Title,
            TemplateId = Form.TemplateId,
            SaveAsDraft = Form.SaveAsDraft,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"'{Form.Title}' was created successfully.");
            return RedirectToPage(RouteNames.SitePages.Edit, new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                "There was an error creating this site page. See the error below.",
                response.GetErrors()
            );
            await LoadTemplateOptionsAsync(HttpContext.RequestAborted);
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Title")]
        [Required]
        public string Title { get; set; }

        [Display(Name = "Template")]
        [Required]
        public string TemplateId { get; set; }

        [Display(Name = "Save as draft")]
        public bool SaveAsDraft { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Application.Roles.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Admins;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Settings",
                RouteName = RouteNames.Configuration.Index,
                IsActive = false
            },
            new BreadcrumbNode
            {
                Label = "Admins",
                RouteName = RouteNames.Admins.Index,
                IsActive = false
            },
            new BreadcrumbNode
            {
                Label = "Create",
                RouteName = RouteNames.Admins.Create,
                IsActive = true
            }
        );

        var roleChoicesResponse = await Mediator.Send(new GetRoles.Query());
        Form = new FormModel
        {
            Roles = roleChoicesResponse
                .Result.Items.Select(p => new FormModel.RoleCheckboxItemViewModel
                {
                    Id = p.Id,
                    Label = p.Label,
                    Selected = false,
                })
                .ToArray(),
        };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateAdmin.Command
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            Roles = Form.Roles.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
            SendEmail = Form.SendEmail,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.FirstName} {Form.LastName} was created successfully.");
            return RedirectToPage(RouteNames.Admins.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this admin. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        [Display(Name = "Send admin welcome email")]
        public bool SendEmail { get; set; } = true;

        public RoleCheckboxItemViewModel[] Roles { get; set; }

        //helpers
        public class RoleCheckboxItemViewModel
        {
            public string Id { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
        }
    }
}

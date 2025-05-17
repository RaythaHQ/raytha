using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Pages.Users;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var userGroupsChoicesResponse = await Mediator.Send(new GetUserGroups.Query());
        var userGroupsViewModel = userGroupsChoicesResponse
            .Result.Items.Select(p => new FormModel.UserGroupCheckboxItemViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Selected = false,
            })
            .ToArray();

        Form = new FormModel { UserGroups = userGroupsViewModel };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateUser.Command
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            UserGroups = Form.UserGroups?.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
            SendEmail = Form.SendEmail,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.FirstName} {Form.LastName} was created successfully.");
            return RedirectToPage("/Users/Index");
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this user. See the error below.",
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

        public UserGroupCheckboxItemViewModel[] UserGroups { get; set; }

        public class UserGroupCheckboxItemViewModel
        {
            public string Id { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
        }
    }
}

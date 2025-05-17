using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Pages.Users;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetUserById.Query { Id = id });

        var allUserGroups = await Mediator.Send(new GetUserGroups.Query());
        var userGroups = allUserGroups.Result.Items.Select(
            p => new FormModel.UserGroupCheckboxItemViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Selected = response.Result.UserGroups.Select(p => p.Id).Contains(p.Id),
            }
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            FirstName = response.Result.FirstName,
            LastName = response.Result.LastName,
            EmailAddress = response.Result.EmailAddress,
            UserGroups = userGroups.ToArray(),
        };

        Id = id;
        IsActive = response.Result.IsActive;
        IsAdmin = response.Result.IsAdmin;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditUser.Command
        {
            Id = id,
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            UserGroups = Form.UserGroups?.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.FirstName} {Form.LastName} was updated successfully.");
            return RedirectToPage("/Users/Edit", new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this admin. See the error below.",
                response.GetErrors()
            );
            var user = await Mediator.Send(new GetUserById.Query { Id = id });
            Id = id;
            IsAdmin = user.Result.IsAdmin;
            IsActive = user.Result.IsActive;
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        public UserGroupCheckboxItemViewModel[] UserGroups { get; set; }

        public class UserGroupCheckboxItemViewModel
        {
            public string Id { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
        }
    }
}

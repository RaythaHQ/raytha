using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string Id { get; set; }
    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetRaythaFunctionById.Query { Id = id });

        Form = new FormModel
        {
            Id = id,
            Name = response.Result.Name,
            DeveloperName = response.Result.DeveloperName,
            TriggerType = response.Result.TriggerType,
            IsActive = response.Result.IsActive,
            Code = response.Result.Code,
        };
        Id = id;
        IsActive = response.Result.IsActive;
        IsAdmin = false; // functions don't have admin flag currently
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditRaythaFunction.Command
        {
            Id = id,
            Name = Form.Name,
            TriggerType = Form.TriggerType,
            IsActive = Form.IsActive,
            Code = Form.Code,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Name} was updated successfully.");
            return RedirectToPage("/RaythaFunctions/Edit", new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this function. See the error below.",
                response.GetErrors()
            );

            Form.Id = id;
            // ensure layout's ISubActionViewModel properties are set for the layout
            Id = id;
            IsActive = Form.IsActive;
            IsAdmin = false;
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Trigger type")]
        public string TriggerType { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Code")]
        public string Code { get; set; }
    }
}

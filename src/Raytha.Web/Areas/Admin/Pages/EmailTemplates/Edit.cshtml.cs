using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.EmailTemplates.Commands;
using Raytha.Application.EmailTemplates.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Pages.EmailTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public string Id { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetEmailTemplateById.Query { Id = id });

        Form = new FormModel
        {
            Id = response.Result.Id,
            Content = response.Result.Content,
            Subject = response.Result.Subject,
            DeveloperName = response.Result.DeveloperName,
            Cc = response.Result.Cc,
            Bcc = response.Result.Bcc,
        };

        Id = response.Result.Id;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditEmailTemplate.Command
        {
            Id = id,
            Subject = Form.Subject,
            Content = Form.Content,
            Bcc = Form.Bcc,
            Cc = Form.Cc,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Subject} was updated successfully.");
            return RedirectToPage("/EmailTemplates/Edit", new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this template. See the error below.",
                response.GetErrors()
            );
            Id = Form.Id;
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; }

        [Display(Name = "Cc")]
        public string Cc { get; set; }

        [Display(Name = "Bcc")]
        public string Bcc { get; set; }
    }
}

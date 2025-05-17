using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.OrganizationSettings.Queries;

namespace Raytha.Web.Areas.Admin.Pages.Smtp;

public class Index : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public bool MissingSmtpEnvironmentVariables { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var input = new GetOrganizationSettings.Query();
        var response = await Mediator.Send(input);
        MissingSmtpEnvironmentVariables = EmailerConfiguration.IsMissingSmtpEnvVars();
        Form = new FormModel
        {
            SmtpOverrideSystem =
                MissingSmtpEnvironmentVariables || response.Result.SmtpOverrideSystem,
            SmtpHost = response.Result.SmtpHost,
            SmtpPort = response.Result.SmtpPort,
            SmtpUsername = response.Result.SmtpUsername,
            SmtpPassword = response.Result.SmtpPassword,
        };

        if (MissingSmtpEnvironmentVariables)
            SetWarningMessage(
                "The server administrator has not set SMTP environment variables (SMTP_HOST, SMTP_PORT, SMTP_USERNAME, SMTP_PASSWORD) on this host. Therefore you must specify SMTP server details below."
            );

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new Raytha.Application.OrganizationSettings.Commands.EditSmtp.Command
        {
            SmtpOverrideSystem = Form.SmtpOverrideSystem,
            SmtpHost = Form.SmtpHost,
            SmtpPort = Form.SmtpPort,
            SmtpUsername = Form.SmtpUsername,
            SmtpPassword = Form.SmtpPassword,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("SMTP has been updated successfully.");
            return RedirectToPage("/Smtp/Index");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Override default SMTP settings")]
        public bool SmtpOverrideSystem { get; set; }

        [Display(Name = "Host")]
        public string SmtpHost { get; set; }

        [Display(Name = "Port")]
        public int? SmtpPort { get; set; }

        [Display(Name = "Username")]
        public string SmtpUsername { get; set; }

        [Display(Name = "Password")]
        public string SmtpPassword { get; set; }
    }
}

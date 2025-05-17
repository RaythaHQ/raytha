using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Setup;

public class Index : BasePageModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public bool MissingSmtpEnvironmentVariables { get; set; }
    public IDictionary<string, string> TimeZones
    {
        get { return DateTimeExtensions.GetTimeZoneDisplayNames(); }
    }

    public async Task<IActionResult> OnGet()
    {
        if (CurrentOrganization.InitialSetupComplete)
            return RedirectToPage("/Dashboard/Index");

        MissingSmtpEnvironmentVariables = EmailerConfiguration.IsMissingSmtpEnvVars();
        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (CurrentOrganization.InitialSetupComplete)
            return RedirectToPage("/Dashboard/Index");

        var input = new InitialSetup.Command
        {
            SmtpHost = Form.SmtpHost,
            SmtpPort = Form.SmtpPort,
            SmtpUsername = Form.SmtpUsername,
            SmtpPassword = Form.SmtpPassword,
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            SuperAdminEmailAddress = Form.SuperAdminEmailAddress,
            SuperAdminPassword = Form.SuperAdminPassword,
            SmtpDefaultFromAddress = Form.SmtpDefaultFromAddress,
            SmtpDefaultFromName = Form.SmtpDefaultFromName,
            OrganizationName = Form.OrganizationName,
            TimeZone = Form.TimeZone,
            WebsiteUrl = Form.WebsiteUrl,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            return RedirectToPage("/Dashboard/Index");
        }

        SetErrorMessage(response.GetErrors());
        MissingSmtpEnvironmentVariables = EmailerConfiguration.IsMissingSmtpEnvVars();
        return Page();
    }

    public class FormModel
    {
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Email address")]
        public string SuperAdminEmailAddress { get; set; }

        [Display(Name = "Password")]
        public string SuperAdminPassword { get; set; }

        [Display(Name = "Organization name")]
        public string OrganizationName { get; set; }

        [Display(Name = "Website url")]
        public string WebsiteUrl { get; set; }

        [Display(Name = "Time zone")]
        public string TimeZone { get; set; }

        [Display(Name = "Default reply-to email address")]
        public string SmtpDefaultFromAddress { get; set; }

        [Display(Name = "Default reply-to name")]
        public string SmtpDefaultFromName { get; set; }

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

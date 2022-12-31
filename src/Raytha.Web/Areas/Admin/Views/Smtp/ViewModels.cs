using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Smtp;

public class Smtp_ViewModel : FormSubmit_ViewModel
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
    
    //helpers
    public bool MissingSmtpEnvironmentVariables { get; set; }
}

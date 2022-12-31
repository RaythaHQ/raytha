using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Application.Common.Utils;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Setup;

public class Setup_ViewModel : FormSubmit_ViewModel
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

    //helpers
    public bool MissingSmtpEnvironmentVariables { get; set; }
    public IDictionary<string, string> TimeZones
    {
        get { return DateTimeExtensions.GetTimeZoneDisplayNames(); }
    }
}

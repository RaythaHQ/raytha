using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Application.Common.Utils;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Configuration;

public class Configuration_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Organization name")]
    public string OrganizationName { get; set; }

    [Display(Name = "Website url")]
    public string WebsiteUrl { get; set; }

    [Display(Name = "Time zone")]
    public string TimeZone { get; set; }

    [Display(Name = "Date format")]
    public string DateFormat { get; set; }

    [Display(Name = "Default reply-to email address")]
    public string SmtpDefaultFromAddress { get; set; }

    [Display(Name = "Default reply-to name")]
    public string SmtpDefaultFromName { get; set; }

    //helpers
    public IDictionary<string, string> AvailableTimeZones
    {
        get { return DateTimeExtensions.GetTimeZoneDisplayNames(); }
    }

    public IDictionary<string, string> AvailableDateFormats
    {
        get
        {
            var dateFormats = new Dictionary<string, string>();
            foreach (var dF in DateTimeExtensions.GetDateFormats())
            {
                dateFormats.Add(dF, dF);
            }
            return dateFormats;
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Profile;

public class ChangePassword_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Your current password")]
    public string CurrentPassword { get; set; }

    [Display(Name = "Your new password")]
    public string NewPassword { get; set; }

    [Display(Name = "Re-type your new password")]
    public string ConfirmNewPassword { get; set; }
}

public class ChangeProfile_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "First name")]
    public string FirstName { get; set; }

    [Display(Name = "Last name")]
    public string LastName { get; set; }

    [Display(Name = "Email address")]
    public string EmailAddress { get; set; }
}

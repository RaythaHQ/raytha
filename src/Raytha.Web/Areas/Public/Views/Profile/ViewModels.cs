using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Public.Views.Profile;

public class ChangePassword_ViewModel : FormSubmit_ViewModel
{
    public string CurrentPassword { get; set; }

    public string NewPassword { get; set; }

    public string ConfirmNewPassword { get; set; }
}

public class ChangeProfile_ViewModel : FormSubmit_ViewModel
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailAddress { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Users;

public class UsersListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "First name")]
    public string FirstName { get; init; }

    [Display(Name = "Last name")]
    public string LastName { get; init; }

    [Display(Name = "Email address")]
    public string EmailAddress { get; init; }

    [Display(Name = "Created at")]
    public string CreationTime { get; init; }

    [Display(Name = "Last logged in")]
    public string LastLoggedInTime { get; init; }

    [Display(Name = "Is active")]
    public string IsActive { get; init; }

    [Display(Name = "User groups")]
    public string UserGroups { get; init; }
}

public class UsersCreate_ViewModel : FormSubmit_ViewModel
{        
    [Display(Name = "First name")]
    public string FirstName { get; set; }
    
    [Display(Name = "Last name")]
    public string LastName { get; set; }
    
    [Display(Name = "Email address")]
    public string EmailAddress { get; set; }

    [Display(Name = "Send admin welcome email")]
    public bool SendEmail { get; set; } = true;

    public UserGroupCheckboxItem_ViewModel[] UserGroups { get; set; }

    //helpers
    public class UserGroupCheckboxItem_ViewModel
    {
        public string Id { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
    }
}

public class UsersEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }
    
    [Display(Name = "First name")]
    public string FirstName { get; set; }
    
    [Display(Name = "Last name")]
    public string LastName { get; set; }
    
    [Display(Name = "Email address")]
    public string EmailAddress { get; set; }

    [Display(Name = "Is active")]
    public bool IsActive { get; set; }

    public UserGroupCheckboxItem_ViewModel[] UserGroups { get; set; }

    //helpers
    public string CurrentUserId { get; set; }
    public bool EditingMyself => CurrentUserId == Id;
    public bool EmailAndPasswordEnabledForUsers { get; set; }
    public bool IsAdmin { get; set; }

    public class UserGroupCheckboxItem_ViewModel
    {
        public string Id { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
    }
}

public class UsersResetPassword_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }
    public bool IsActive { get; set; }

    [Display(Name = "New password")]
    public string NewPassword { get; set; }

    [Display(Name = "Re-type the new password")]
    public string ConfirmNewPassword { get; set; }

    public bool SendEmail { get; set; } = true;

    //helpers
    public string CurrentUserId { get; set; }
    public bool EmailAndPasswordEnabledForUsers { get; set; }
    public bool IsAdmin { get; set; }
}


public class UsersActionsMenu_ViewModel
{
    public string Id { get; set; }
    public bool IsActive { get; set; }
    public string CurrentUserId { get; set; }
    public bool EmailAndPasswordEnabledForUsers { get; set; }
    public bool IsAdmin { get; set; }

    //helpers
    public bool EditingMyself => CurrentUserId == Id;

}
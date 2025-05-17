using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.UserGroups;

public class UserGroupsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "DeveloperName")]
    public string DeveloperName { get; init; }
}

public class CreateUserGroup_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }
}

public class EditUserGroup_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }
}

public class UserGroupsActionsMenu_ViewModel
{
    public string Id { get; set; }
}

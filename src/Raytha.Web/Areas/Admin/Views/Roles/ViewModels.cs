using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Roles;

public class RolesListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "DeveloperName")]
    public string DeveloperName { get; init; }
}

public class CreateRole_ViewModel : FormSubmit_ViewModel
{        
    [Display(Name = "Label")]
    public string Label { get; set; }
    
    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    public SystemPermissionCheckboxItem_ViewModel[] SystemPermissions { get; set; }
    public ContentTypePermissionCheckboxItem_ViewModel[] ContentTypePermissions { get; set; }

    //helpers
    public class SystemPermissionCheckboxItem_ViewModel
    {
        public string DeveloperName { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
    }

    public class ContentTypePermissionCheckboxItem_ViewModel
    {
        public string DeveloperName { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
        public string ContentTypeLabel { get; set; }
        public string ContentTypeId { get; set; }
    }
}

public class EditRole_ViewModel : FormSubmit_ViewModel
{        
    public string Id { get; set; }
    
    [Display(Name = "Label")]
    public string Label { get; set; }
    
    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    public SystemPermissionCheckboxItem_ViewModel[] SystemPermissions { get; set; }
    public ContentTypePermissionCheckboxItem_ViewModel[] ContentTypePermissions { get; set; }

    //helpers
    public bool IsSuperAdmin { get; set; }

    public class SystemPermissionCheckboxItem_ViewModel
    {
        public string DeveloperName { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
    }

    public class ContentTypePermissionCheckboxItem_ViewModel
    {
        public string DeveloperName { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
        public string ContentTypeLabel { get; set; }
        public string ContentTypeId { get; set; }
    }
}

public class RolesActionsMenu_ViewModel
{
    public string Id { get; set; }

    //helpers
    public bool IsSuperAdmin { get; set; }
}
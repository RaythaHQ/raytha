using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

namespace Raytha.Web.Areas.Admin.Views.EmailTemplates;

public class EmailTemplatesListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Subject")]
    public string Subject { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Last modified at")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified by")]
    public string LastModifierUser { get; init; }
}

public class EmailTemplatesEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Subject")]
    public string Subject { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Content")]
    public string Content { get; set; }

    [Display(Name = "Cc")]
    public string Cc { get; set; }

    [Display(Name = "Bcc")]
    public string Bcc { get; set; }

    //helpers
    public long MaxFileSize { get; set; }
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }
    public string PathBase { get; set; }
    public Dictionary<string, IEnumerable<EmailInsertVariableListItem_ViewModel>> TemplateVariables { get; set; }
}

public class EmailTemplatesRevisionsPagination_ViewModel : Pagination_ViewModel
{
    public IEnumerable<EmailTemplatesRevisionsListItem_ViewModel> Items { get; }

    public string EmailTemplateId { get; set; }

    public EmailTemplatesRevisionsPagination_ViewModel(
        IEnumerable<EmailTemplatesRevisionsListItem_ViewModel> items, int totalCount) : base(totalCount) => Items = items;

}

public class EmailTemplatesRevisionsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Created at")]
    public string CreationTime { get; init; }

    [Display(Name = "Subject")]
    public string Subject { get; init; }

    [Display(Name = "Created by")]
    public string CreatorUser { get; init; }

    [Display(Name = "Content")]
    public string Content { get; init; }
}

public class EmailTemplateActionsMenu_ViewModel
{
    public string Id { get; set; }
    public bool IsBuiltInTemplate { get; set; }
}

public class EmailInsertVariableListItem_ViewModel
{
    public string DeveloperName { get; set; }
    public string TemplateVariable { get; set; }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Application.MediaItems;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

namespace Raytha.Web.Areas.Admin.Views.Themes.WebTemplates;

public class WebTemplatesListItem_ViewModel
{
    public string Id { get; init; }
    public string ThemeId { get; set; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Last modified at")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified by")]
    public string LastModifierUser { get; init; }

    [Display(Name = "Is built in template")]
    public string IsBuiltInTemplate { get; init; }

    public ParentTemplate_ViewModel ParentTemplate { get; init; }

    //helpers
    public class ParentTemplate_ViewModel
    {
        public string Id { get; init; }
        public string Label { get; init; }
    }
}

public class WebTemplatesPagination_ViewModel : Pagination_ViewModel
{
    public IEnumerable<WebTemplatesListItem_ViewModel> Items { get; }
    public string ThemeId { get; }

    public WebTemplatesPagination_ViewModel(IEnumerable<WebTemplatesListItem_ViewModel> items, int totalCount, string themeId) : base(totalCount)
    {
        Items = items;
        ThemeId = themeId;
    }

}

public class WebTemplatesCreate_ViewModel : FormSubmit_ViewModel
{
    public string ThemeId { get; set; }

    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Content")]
    public string Content { get; set; }

    [Display(Name = "Parent template")]
    public string ParentTemplateId { get; set; }

    [Display(Name = "The following content types can use this template")]
    public WebTemplateAccessToModelDefinitions_ViewModel[] TemplateAccessToModelDefinitions { get; set; }

    [Display(Name = "This is a base layout that other templates can inherit from")]
    public bool IsBaseLayout { get; set; } = false;

    [Display(Name = "This template can be accessed by newly created content types")]
    public bool AllowAccessForNewContentTypes { get; set; } = true;

    public IEnumerable<MediaItemDto> MediaItems { get; set; }

    //helpers
    public long MaxFileSize { get; set; }
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }
    public string PathBase { get; set; }
    public IEnumerable<WebTemplateDto> ParentTemplates { get; set; }
    public Dictionary<string, IEnumerable<WebTemplatesInsertVariableListItem_ViewModel>> TemplateVariables { get; set; }
}

public class WebTemplatesEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }
    public string ThemeId { get; set; }

    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "This is a base layout that other templates can inherit from")]
    public bool IsBaseLayout { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Content")]
    public string Content { get; set; }

    [Display(Name = "Parent template")]
    public string ParentTemplateId { get; set; }

    [Display(Name = "The following content types can use this template")]
    public WebTemplateAccessToModelDefinitions_ViewModel[] TemplateAccessToModelDefinitions { get; set; }

    [Display(Name = "This template can be accessed by newly created content types")]
    public bool AllowAccessForNewContentTypes { get; set; }

    public IEnumerable<MediaItemDto> MediaItems { get; set; }

    //helpers
    public long MaxFileSize { get; set; }
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }
    public string PathBase { get; set; }
    public Dictionary<string, string> ParentTemplates { get; set; }
    public bool IsBuiltInTemplate { get; set; }
    public Dictionary<string, IEnumerable<WebTemplatesInsertVariableListItem_ViewModel>> TemplateVariables { get; set; }
}

public class WebTemplateAccessToModelDefinitions_ViewModel
{
    public string Id { get; set; }
    public string Key { get; set; }
    public bool Value { get; set; }
}

public class WebTemplatesRevisionsPagination_ViewModel : Pagination_ViewModel
{
    public IEnumerable<WebTemplatesRevisionsListItem_ViewModel> Items { get; }

    public string TemplateId { get; set; }
    public bool IsBuiltInTemplate { get; set; }
    public string ThemeId { get; set; }

    public WebTemplatesRevisionsPagination_ViewModel(
        IEnumerable<WebTemplatesRevisionsListItem_ViewModel> items, int totalCount) : base(totalCount) => Items = items;

}

public class WebTemplatesRevisionsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Created at")]
    public string CreationTime { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Created by")]
    public string CreatorUser { get; init; }

    [Display(Name = "Content")]
    public string Content { get; init; }
}

public class WebTemplatesActionsMenu_ViewModel
{
    public string ThemeId { get; set; }
    public string Id { get; set; }
    public bool IsBuiltInTemplate { get; set; }
}

public class WebTemplatesInsertVariableListItem_ViewModel
{
    public string DeveloperName { get; set; }
    public string TemplateVariable { get; set; }
}
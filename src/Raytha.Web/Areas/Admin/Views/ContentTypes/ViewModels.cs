using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

namespace Raytha.Web.Areas.Admin.Views.ContentTypes;

public class ContentTypesCreate_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Label (plural)")]
    public string LabelPlural { get; set; }

    [Display(Name = "Label (singular)")]
    public string LabelSingular { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Description")]
    public string Description { get; set; }

    [Display(Name = "Default route template")]
    public string DefaultRouteTemplate { get; set; }

    //helpers
    public string WebsiteUrl { get; set; }
}

public class ContentTypesEdit_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }

    [Display(Name = "Label (plural)")]
    public string LabelPlural { get; set; }

    [Display(Name = "Label (singular)")]
    public string LabelSingular { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Description")]
    public string Description { get; set; }

    [Display(Name = "Default route template")]
    public string DefaultRouteTemplate { get; set; }

    [Display(Name = "Primary field")]
    public string PrimaryFieldId { get; set; }

    //helpers
    public Dictionary<string, string> ContentTypeFields { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
    public string WebsiteUrl { get; set; }
}

public class FieldsPagination_ViewModel : Pagination_ViewModel, IMustHaveCurrentViewForList
{
    public IEnumerable<FieldsListItem_ViewModel> Items { get; }
    public bool ShowDeletedOnly { get; }

    public FieldsPagination_ViewModel(
        IEnumerable<FieldsListItem_ViewModel> items,
        int totalCount,
        bool showDeletedOnly
    )
        : base(totalCount)
    {
        Items = items;
        ShowDeletedOnly = showDeletedOnly;
    }

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class FieldsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Field type")]
    public string FieldType { get; init; }

    [Display(Name = "Is required")]
    public string IsRequired { get; init; }
}

public class FieldsCreate_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string ContentTypeId { get; set; }

    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Field type")]
    public string FieldType { get; set; }

    [Display(Name = "Content type to establish a relationship with")]
    public string RelatedContentTypeId { get; set; }

    [Display(Name = "Is a required field")]
    public bool IsRequired { get; set; } = true;

    [Display(Name = "Instructions for this field")]
    public string Description { get; set; }

    public FieldChoiceItem_ViewModel[] Choices { get; set; } =
        new FieldChoiceItem_ViewModel[]
        {
            new FieldChoiceItem_ViewModel
            {
                Label = "Choice label 1",
                DeveloperName = "developer_name_1",
                Disabled = false,
            },
            new FieldChoiceItem_ViewModel
            {
                Label = "Choice label 2",
                DeveloperName = "developer_name_2",
                Disabled = false,
            },
            new FieldChoiceItem_ViewModel
            {
                Label = "Choice label 3",
                DeveloperName = "developer_name_3",
                Disabled = false,
            },
        };

    //helpers
    public Dictionary<string, string> AvailableContentTypes { get; set; }
    public Dictionary<string, string> AvailableFieldTypes { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class EditField_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }

    public string ContentTypeId { get; set; }

    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Field type")]
    public string FieldType { get; set; }

    [Display(Name = "Content type to establish a relationship with")]
    public string RelatedContentTypeId { get; set; }

    [Display(Name = "Is a required field")]
    public bool IsRequired { get; set; } = true;

    [Display(Name = "Instructions for this field")]
    public string Description { get; set; }

    public FieldChoiceItem_ViewModel[] Choices { get; set; }

    //helpers
    public Dictionary<string, string> AvailableContentTypes { get; set; }
    public Dictionary<string, string> AvailableFieldTypes { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class FieldChoiceItem_ViewModel
{
    public string Label { get; set; }
    public string DeveloperName { get; set; }
    public string Value { get; set; }
    public bool Disabled { get; set; }
}

public class DeletedContentItemsPagination_ViewModel
    : Pagination_ViewModel,
        IMustHaveCurrentViewForList
{
    public IEnumerable<DeletedContentItemsListItem_ViewModel> Items { get; }

    public DeletedContentItemsPagination_ViewModel(
        IEnumerable<DeletedContentItemsListItem_ViewModel> items,
        int totalCount
    )
        : base(totalCount) => Items = items;

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class DeletedContentItemsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Primary field")]
    public string PrimaryField { get; init; }

    [Display(Name = "Original id")]
    public string OriginalContentItemId { get; init; }

    [Display(Name = "Deleted by")]
    public string DeletedBy { get; init; }

    [Display(Name = "Deleted at")]
    public string DeletionTime { get; init; }
}

public class FieldsActionsMenu_ViewModel
{
    public string Id { get; set; }
    public string ContentTypeDeveloperName { get; set; }
}

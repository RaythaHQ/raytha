using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Raytha.Web.Areas.Admin.Views.Views;

public class ViewsPagination_ViewModel : Pagination_ViewModel, IMustHaveCurrentViewForList
{
    public IEnumerable<ViewsListItem_ViewModel> Items { get; }

    public ViewsPagination_ViewModel(
        IEnumerable<ViewsListItem_ViewModel> items, int totalCount) : base(totalCount) => Items = items;

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ViewsListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Last modified at")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified by")]
    public string LastModifierUser { get; init; }

    [Display(Name = "Is published")]
    public string IsPublished { get; init; }

    [Display(Name = "Route path")]
    public string RoutePath { get; init; }

    //helpers
    public bool IsHomePage { get; set; }
    public bool IsFavoriteForAdmin { get; init; }
}

public class ViewsCreate_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    public string Description { get; set; }

    public string ContentTypeId { get; set; }

    public string DuplicateFromId { get; set; }

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ViewsEdit_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }

    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    public string Description { get; set; }

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ViewsPublicSettings_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }

    [Display(Name = "Template")]
    public string TemplateId { get; set; }

    [Display(Name = "Route path")]
    public string RoutePath { get; set; }

    [Display(Name = "Is published")]
    public bool IsPublished { get; set; }

    [Display(Name = "Default number of items per page")]
    public int DefaultNumberOfItemsPerPage { get; set; }

    [Display(Name = "Max number of items per page")]
    public int MaxNumberOfItemsPerPage { get; set; }

    [Display(Name = "Ignore client side filter and sort query parameters")]
    public bool IgnoreClientFilterAndSortQueryParams { get; set; }

    public bool IsHomePage { get; set; }

    //helpers
    public string WebsiteUrl { get; set; }
    public Dictionary<string, string> AvailableTemplates { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}


public class ViewsColumns_ViewModel : IMustHaveCurrentViewForList
{
    public ViewsColumnsListItem_ViewModel[] SelectedColumns { get; set; }
    public ViewsColumnsListItem_ViewModel[] NotSelectedColumns { get; set; }

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ViewsColumnsListItem_ViewModel
{
    public string Label { get; init; }
    public string DeveloperName { get; init; }
    public bool Selected { get; init; }
    public int FieldOrder { get; init; }
}

public class ViewsFilter_ViewModel : IMustHaveCurrentViewForList
{
    public ViewsFilterContentTypeField_ViewModel[] ContentTypeFields { get; set; }

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
    public string ContentTypeFieldsAsJson
    {
        get
        {
            return JsonSerializer.Serialize(ContentTypeFields);
        }
    }
}

public class ViewsFilterContentTypeField_ViewModel
{
    public string Label { get; init; }
    public string DeveloperName { get; init; }
    public BaseFieldType FieldType { get; init; }
    public IEnumerable<string> Choices { get; init; }
}

public class FilterSubtree_ViewModel
{
    public FilterCondition FilterCondition { get; set; }
    public ViewsFilterContentTypeField_ViewModel[] ContentTypeFields { get; set; }
    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ViewsSort_ViewModel : IMustHaveCurrentViewForList
{
    public ViewsSortListItem_ViewModel[] SelectedColumns { get; set; }

    //helpers
    public Dictionary<string, string> NotSelectedColumns { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ViewsSortListItem_ViewModel
{
    public string Label { get; init; }
    public string DeveloperName { get; init; }
    public string OrderByDirection { get; init; }
    public bool Selected { get; init; }
    public int FieldOrder { get; init; }
}
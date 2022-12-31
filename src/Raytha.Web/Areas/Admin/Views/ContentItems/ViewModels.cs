using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CSharpVitamins;
using Raytha.Web.Areas.Admin.Views.Shared;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

namespace Raytha.Web.Areas.Admin.Views.ContentItems;

public class ContentItemsPagination_ViewModel : Pagination_ViewModel, IMustHaveCurrentViewForList, IMustHaveFavoriteViewsForList
{
    public IEnumerable<ContentItemsListItem_ViewModel> Items { get; }

    public ContentItemsPagination_ViewModel(
        IEnumerable<ContentItemsListItem_ViewModel> items, int totalCount) : base(totalCount) => Items = items;

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
    public IEnumerable<FavoriteViewsForList_ViewModel> FavoriteViews { get; set; }
    public bool HasFavorites
    {
        get { return FavoriteViews != null && FavoriteViews.Any(); }
    }

    public bool CurrentViewIsFavorite
    {
        get { return HasFavorites && FavoriteViews.Any(p => p.Id == CurrentView.Id); }
    }
}

public class FieldValue_ViewModel
{
    public string FieldType { get; set; }
    public string Label { get; set; }
    public string DeveloperName { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public string Value { get; set; }
    public FieldValueChoiceItem_ViewModel[] AvailableChoices { get; set; }
    public string RelatedContentTypeId { get; set; }
    public string RelatedContentItemPrimaryField { get; set; }

    //helpers
    public string AsteriskCssIfRequired => IsRequired ? "raytha-required" : string.Empty;
}

public class FieldValueChoiceItem_ViewModel
{
    public string Label { get; set; }
    public string DeveloperName { get; set; }
    public bool Disabled { get; set; }
    public string Value { get; set; }
}

public class ContentItemsListItem_ViewModel
{
    public string Id { get; set; }
    public bool IsHomePage { get; set; }
    public Dictionary<string, string> FieldValues { get; set; }
}

public class ContentItemsCreate_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    [Display(Name = "Template")]
    public string TemplateId { get; set; }
    public bool SaveAsDraft { get; set; }
    public FieldValue_ViewModel[] FieldValues { get; set; }
    public long MaxFileSize { get; set; }
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }

    //helpers
    public Dictionary<ShortGuid, string> AvailableTemplates { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}


public class ContentItemsEdit_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }
    public bool SaveAsDraft { get; set; }
    public FieldValue_ViewModel[] FieldValues { get; set; }
    public long MaxFileSize { get; set; }
    public string AllowedMimeTypes { get; set; }
    public bool UseDirectUploadToCloud { get; set; }

    //helpers
    public string BackToListUrl { get; set; }
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ContentItemsActionsMenu_ViewModel
{
    public string Id { get; set; }
    public string ActivePage { get; set; }
    public string ContentTypeLabelSingular { get; set; }
    public string ContentTypeDeveloperName { get; set; }
}

public class ContentItemsRevisionsPagination_ViewModel : Pagination_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }

    public IEnumerable<ContentItemsRevisionsListItem_ViewModel> Items { get; }

    public ContentItemsRevisionsPagination_ViewModel(
        IEnumerable<ContentItemsRevisionsListItem_ViewModel> items, int totalCount) : base(totalCount) => Items = items;

    //helpers
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ContentItemsRevisionsListItem_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Created at")]
    public string CreationTime { get; set; }

    [Display(Name = "Created by")]
    public string CreatorUser { get; set; }

    public string ContentAsJson { get; set; }
}

public class ContentItemsSettings_ViewModel : FormSubmit_ViewModel, IMustHaveCurrentViewForList
{
    public string Id { get; set; }

    [Display(Name = "Template")]
    public string TemplateId { get; set; }

    [Display(Name = "Route path")]
    public string RoutePath { get; set; }

    public bool IsHomePage { get; set; }

    //helpers
    public string WebsiteUrl { get; set; }
    public Dictionary<string, string> AvailableTemplates { get; set; }
    public CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class ContentItemsBackToList_ViewModel
{
    public string ContentTypeDeveloperName { get; set; }
    public bool IsEditing { get; set; }
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public string BackToListUrl { get; set; }
}
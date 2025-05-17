using System.Collections.Generic;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

public interface IMustHaveCurrentViewForList
{
    CurrentViewForList_ViewModel CurrentView { get; set; }
}

public class CurrentViewForList_ViewModel
{
    public string Id { get; set; }
    public string Label { get; set; }
    public IEnumerable<string> Columns { get; set; }
    public IEnumerable<FilterCondition> Filter { get; set; }
    public string ContentTypeId { get; set; }
    public string ContentTypeLabelSingular { get; set; }
    public string ContentTypeLabelPlural { get; set; }
    public string ContentTypeDescription { get; set; }
    public string ContentTypeDeveloperName { get; set; }
    public bool IsPublished { get; set; }
    public string RoutePath { get; set; }
    public bool IsHomePage { get; set; }
}

public interface IMustHaveFavoriteViewsForList
{
    IEnumerable<FavoriteViewsForList_ViewModel> FavoriteViews { get; set; }
}

public class FavoriteViewsForList_ViewModel
{
    public string Id { get; set; }
    public string Label { get; set; }
}

using System.Collections.Generic;

namespace Raytha.Web.Areas.Admin.Views.Themes.MediaItems;

public class MediaItemsListItem_ViewModel
{
    public string Id { get; init; }
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public string FileStorageProvider { get; init; }
    public string ObjectKey { get; init; }
}

public class MediaItemList_ViewModel
{
    public IEnumerable<MediaItemsListItem_ViewModel> Items { get; init; }
    public string ThemeId { get; init; }
}


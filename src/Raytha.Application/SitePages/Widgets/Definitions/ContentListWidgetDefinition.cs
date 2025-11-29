using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the Content List widget type.
/// </summary>
public class ContentListWidgetDefinition : BaseWidgetDefinition<ContentListWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.ContentList.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.ContentList.DisplayName;
    public override string Description => "Displays a list of content items from a content type.";
    public override string IconClass => "bi-list-ul";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (!string.IsNullOrEmpty(settings.ContentType))
        {
            var style = settings.DisplayStyle ?? "cards";
            return $"{settings.ContentType} ({style}, {settings.PageSize} items)";
        }

        return "No content type selected";
    }
}


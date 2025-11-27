using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the Image + Text widget type.
/// </summary>
public class ImageTextWidgetDefinition : BaseWidgetDefinition<ImageTextWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.ImageText.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.ImageText.DisplayName;
    public override string Description => "Image alongside text content with configurable layout.";
    public override string IconClass => "bi-image";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (!string.IsNullOrEmpty(settings.Headline))
        {
            var position = settings.ImagePosition == "right" ? "Image right" : "Image left";
            return $"{Truncate(settings.Headline, 40)} ({position})";
        }

        return "No headline set";
    }
}


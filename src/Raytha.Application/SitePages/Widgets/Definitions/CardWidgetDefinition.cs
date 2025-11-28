using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the Card widget type.
/// </summary>
public class CardWidgetDefinition : BaseWidgetDefinition<CardWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.Card.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.Card.DisplayName;
    public override string Description => "A single card with image, title, description, and call-to-action.";
    public override string IconClass => "bi-card-text";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (!string.IsNullOrEmpty(settings.Title))
        {
            return Truncate(settings.Title, 40);
        }

        return "Empty card";
    }
}


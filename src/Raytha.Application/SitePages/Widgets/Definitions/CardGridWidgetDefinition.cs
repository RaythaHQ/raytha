using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the Card Grid widget type.
/// </summary>
public class CardGridWidgetDefinition : BaseWidgetDefinition<CardGridWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.CardGrid.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.CardGrid.DisplayName;
    public override string Description => "Grid of cards with images, titles, and descriptions.";
    public override string IconClass => "bi-grid-3x3-gap";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);
        var cardCount = settings.Cards?.Count ?? 0;

        if (!string.IsNullOrEmpty(settings.Headline))
        {
            return $"{Truncate(settings.Headline, 30)} ({cardCount} cards)";
        }

        return $"{cardCount} card{(cardCount == 1 ? "" : "s")}";
    }
}

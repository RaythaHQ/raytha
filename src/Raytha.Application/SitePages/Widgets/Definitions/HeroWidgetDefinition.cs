using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the Hero widget type.
/// </summary>
public class HeroWidgetDefinition : BaseWidgetDefinition<HeroWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.Hero.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.Hero.DisplayName;
    public override string Description => "Large banner with headline, subtext, and optional call-to-action button.";
    public override string IconClass => "bi-card-heading";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (!string.IsNullOrEmpty(settings.Headline))
        {
            return Truncate(settings.Headline, 60);
        }

        return "No headline set";
    }
}


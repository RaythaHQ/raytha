using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the CTA (Call to Action) widget type.
/// </summary>
public class CtaWidgetDefinition : BaseWidgetDefinition<CtaWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.CTA.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.CTA.DisplayName;
    public override string Description => "Prominent call-to-action block with heading and button.";
    public override string IconClass => "bi-megaphone";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (!string.IsNullOrEmpty(settings.Headline))
        {
            if (!string.IsNullOrEmpty(settings.ButtonText))
            {
                return $"{Truncate(settings.Headline, 35)} → {settings.ButtonText}";
            }
            return Truncate(settings.Headline, 60);
        }

        return "No headline set";
    }
}


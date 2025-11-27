using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the FAQ widget type.
/// </summary>
public class FaqWidgetDefinition : BaseWidgetDefinition<FaqWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.FAQ.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.FAQ.DisplayName;
    public override string Description => "Expandable accordion of frequently asked questions.";
    public override string IconClass => "bi-question-circle";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);
        var itemCount = settings.Items?.Count ?? 0;

        if (!string.IsNullOrEmpty(settings.Headline))
        {
            return $"{Truncate(settings.Headline, 30)} ({itemCount} items)";
        }

        return $"{itemCount} question{(itemCount == 1 ? "" : "s")}";
    }
}


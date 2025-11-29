using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the WYSIWYG widget type.
/// </summary>
public class WysiwygWidgetDefinition : BaseWidgetDefinition<WysiwygWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.Wysiwyg.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.Wysiwyg.DisplayName;
    public override string Description => "Rich text content block with WYSIWYG editor.";
    public override string IconClass => "bi-file-earmark-richtext";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (!string.IsNullOrEmpty(settings.Content))
        {
            var plainText = StripHtml(settings.Content);
            return Truncate(plainText, 60);
        }

        return "No content";
    }
}


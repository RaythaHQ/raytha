using Raytha.Application.SitePages.Widgets.Settings;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Definition for the Embed widget type.
/// </summary>
public class EmbedWidgetDefinition : BaseWidgetDefinition<EmbedWidgetSettings>
{
    public override string DeveloperName => BuiltInWidgetType.Embed.DeveloperName;
    public override string DisplayName => BuiltInWidgetType.Embed.DisplayName;
    public override string Description => "Embed external content via iframe or raw HTML.";
    public override string IconClass => "bi-code-slash";

    public override string GetAdminSummary(string settingsJson)
    {
        var settings = DeserializeSettings(settingsJson);

        if (settings.EmbedType == "iframe" && !string.IsNullOrEmpty(settings.IframeUrl))
        {
            try
            {
                var uri = new Uri(settings.IframeUrl);
                return $"iframe: {uri.Host}";
            }
            catch
            {
                return $"iframe: {Truncate(settings.IframeUrl, 40)}";
            }
        }

        if (settings.EmbedType == "html" && !string.IsNullOrEmpty(settings.HtmlContent))
        {
            return $"HTML embed ({settings.HtmlContent.Length} chars)";
        }

        return "No embed configured";
    }
}


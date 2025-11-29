using Raytha.Application.MediaItems;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WidgetTemplates;

namespace Raytha.Application.Themes;

public record ThemeJson
{
    public required IEnumerable<WebTemplateJson> WebTemplates { get; init; } =
        new List<WebTemplateJson>();
    public required IEnumerable<WidgetTemplateJson> WidgetTemplates { get; init; } =
        new List<WidgetTemplateJson>();
    public required IEnumerable<MediaItemJson> MediaItems { get; set; } = new List<MediaItemJson>();
}

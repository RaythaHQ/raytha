using Raytha.Application.MediaItems;
using Raytha.Application.Themes.WebTemplates;

namespace Raytha.Application.Themes;

public record ThemeJson
{
    public required IEnumerable<WebTemplateJson> WebTemplates { get; init; } = new List<WebTemplateJson>();
    public required IEnumerable<MediaItemJson> MediaItems { get; set; } = new List<MediaItemJson>();
}
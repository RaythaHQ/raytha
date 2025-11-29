namespace Raytha.Application.Common.Interfaces;

public interface IRenderEngine
{
    string RenderAsHtml(string template, object entity);

    /// <summary>
    /// Renders a template with Site Page context, including widgets.
    /// </summary>
    /// <param name="template">The Liquid template source.</param>
    /// <param name="entity">The render model entity.</param>
    /// <param name="themeId">The active theme ID for widget template lookup.</param>
    /// <param name="widgets">Dictionary of section name to widget list.</param>
    string RenderAsHtml(
        string template,
        object entity,
        Guid themeId,
        Dictionary<string, List<SitePageWidgetRenderData>>? widgets
    );
}

/// <summary>
/// Data class for widget rendering passed to the render engine.
/// </summary>
public class SitePageWidgetRenderData
{
    public string Id { get; set; } = string.Empty;
    public string WidgetType { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public int Row { get; set; }
    public int Column { get; set; }
    public int ColumnSpan { get; set; } = 12;
    public string CssClass { get; set; } = string.Empty;
    public string HtmlId { get; set; } = string.Empty;
    public string CustomAttributes { get; set; } = string.Empty;
}

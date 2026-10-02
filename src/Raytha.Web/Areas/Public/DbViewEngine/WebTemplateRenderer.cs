using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Areas.Public.DbViewEngine;

/// <summary>
/// A public render failure whose message names the template. Development surfaces that message;
/// production public pages stay empty.
/// </summary>
public sealed class TemplateRenderException : Exception
{
    public TemplateRenderException(string developerName, Exception inner)
        : base($"{developerName}: {inner.Message}", inner)
    {
        DeveloperName = developerName;
    }

    public string DeveloperName { get; }
}

public static class WebTemplateRenderer
{
    public static string Render(
        IRenderEngine renderer,
        string developerName,
        string source,
        object model
    )
    {
        try
        {
            return renderer.RenderAsHtml(source, model);
        }
        catch (Exception ex) when (ex is not TemplateRenderException)
        {
            throw new TemplateRenderException(developerName, ex);
        }
    }

    public static string Render(
        IRenderEngine renderer,
        string developerName,
        string source,
        object model,
        Guid themeId,
        Dictionary<string, List<SitePageWidgetRenderData>>? widgets
    )
    {
        try
        {
            return renderer.RenderAsHtml(source, model, themeId, widgets);
        }
        catch (Exception ex) when (ex is not TemplateRenderException)
        {
            throw new TemplateRenderException(developerName, ex);
        }
    }
}

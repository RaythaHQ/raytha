#nullable enable
using System.Text;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Shared.TagHelpers;

/// <summary>
/// Tag helper for rendering breadcrumbs navigation.
/// Usage: &lt;breadcrumbs /&gt;
/// Reads breadcrumbs from ViewData["Breadcrumbs"] set by SetBreadcrumbs() in PageModel.
/// </summary>
[HtmlTargetElement("breadcrumbs")]
public class BreadcrumbsTagHelper : TagHelper
{
    private readonly LinkGenerator _linkGenerator;

    /// <summary>
    /// Constructor that injects LinkGenerator for URL generation.
    /// </summary>
    public BreadcrumbsTagHelper(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    /// <summary>
    /// Gets or sets the ViewContext for accessing ViewData.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional CSS class to add to the breadcrumb container.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    /// <summary>
    /// Order for this tag helper. Set to run after built-in tag helpers.
    /// </summary>
    public override int Order => 100;

    /// <summary>
    /// Processes the tag by rendering breadcrumbs from ViewData.
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var breadcrumbs = ViewContext.ViewData["Breadcrumbs"] as IEnumerable<BreadcrumbNode>;

        // Debug: Check all ViewData keys
        Console.WriteLine(
            $"[Breadcrumbs Debug] ViewData Keys: {string.Join(", ", ViewContext.ViewData.Keys)}"
        );
        Console.WriteLine($"[Breadcrumbs Debug] Breadcrumbs is null: {breadcrumbs == null}");
        if (breadcrumbs != null)
        {
            Console.WriteLine($"[Breadcrumbs Debug] Breadcrumbs count: {breadcrumbs.Count()}");
        }

        if (breadcrumbs == null || !breadcrumbs.Any())
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "nav";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("aria-label", "breadcrumb");

        if (!string.IsNullOrEmpty(CssClass))
        {
            output.Attributes.SetAttribute("class", CssClass);
        }

        var html = new StringBuilder();
        html.AppendLine("<ol class=\"breadcrumb\">");

        Console.WriteLine(
            $"[Breadcrumbs Debug] Starting to render {breadcrumbs.Count()} breadcrumbs"
        );

        foreach (var breadcrumb in breadcrumbs)
        {
            Console.WriteLine(
                $"[Breadcrumbs Debug] Rendering breadcrumb: {breadcrumb.Label}, Active: {breadcrumb.IsActive}, RouteName: {breadcrumb.RouteName}"
            );

            var cssClasses = "breadcrumb-item";
            if (breadcrumb.IsActive)
            {
                cssClasses += " active";
            }

            html.Append($"<li class=\"{cssClasses}\"");

            if (breadcrumb.IsActive)
            {
                html.Append(" aria-current=\"page\"");
            }

            html.Append(">");

            if (!string.IsNullOrEmpty(breadcrumb.Icon))
            {
                html.Append(breadcrumb.Icon);
                html.Append(" ");
            }

            if (!breadcrumb.IsActive && !string.IsNullOrEmpty(breadcrumb.RouteName))
            {
                // Generate link
                var url = GenerateUrl(breadcrumb.RouteName, breadcrumb.RouteValues);
                Console.WriteLine(
                    $"[Breadcrumbs Debug] Generated URL for {breadcrumb.Label}: {url}"
                );
                html.Append($"<a href=\"{url}\">{breadcrumb.Label}</a>");
            }
            else
            {
                // Just text for active breadcrumb
                html.Append(breadcrumb.Label);
            }

            html.AppendLine("</li>");
        }

        html.AppendLine("</ol>");

        var htmlString = html.ToString();
        Console.WriteLine($"[Breadcrumbs Debug] Final HTML length: {htmlString.Length}");
        Console.WriteLine($"[Breadcrumbs Debug] Final HTML: {htmlString}");

        output.Content.SetHtmlContent(htmlString);
    }

    /// <summary>
    /// Generates a URL for the given route name and values.
    /// </summary>
    private string GenerateUrl(string routeName, Dictionary<string, string>? routeValues)
    {
        // Use LinkGenerator to generate page URLs
        var httpContext = ViewContext.HttpContext;
        var values = new RouteValueDictionary();

        if (routeValues != null)
        {
            foreach (var kvp in routeValues)
            {
                values.Add(kvp.Key, kvp.Value);
            }
        }

        return _linkGenerator.GetPathByPage(httpContext, routeName, values: values) ?? "#";
    }
}

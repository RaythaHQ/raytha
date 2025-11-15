#nullable enable
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Raytha.Web.Areas.Shared.TagHelpers;

/// <summary>
/// Tag helper for marking navigation links as active based on the current page route.
/// Automatically adds the specified CSS class (default: "active") when the link matches the current page.
/// Usage: &lt;a nav-active-section="Configuration" asp-page="..." class="nav-link"&gt;Configuration&lt;/a&gt;
/// Or: &lt;a nav-route-name="/Users/Index" asp-page="..." class="nav-link"&gt;Users&lt;/a&gt;
/// </summary>
[HtmlTargetElement("a", Attributes = NavActiveSectionAttributeName)]
[HtmlTargetElement("a", Attributes = NavRouteNameAttributeName)]
public class NavLinkTagHelper : TagHelper
{
    private const string NavActiveSectionAttributeName = "nav-active-section";
    private const string NavActiveClassAttributeName = "nav-active-class";
    private const string NavRouteNameAttributeName = "nav-route-name";
    private const string NavMatchModeAttributeName = "nav-match-mode";

    /// <summary>
    /// Gets or sets the ViewContext for accessing route data.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    /// <summary>
    /// Gets or sets the navigation section identifier to match against the current page.
    /// This is matched case-insensitively against the current page path.
    /// </summary>
    [HtmlAttributeName(NavActiveSectionAttributeName)]
    public string? NavSection { get; set; }

    /// <summary>
    /// Gets or sets the route name to match against the current page.
    /// If specified, uses exact route name matching instead of section matching.
    /// </summary>
    [HtmlAttributeName(NavRouteNameAttributeName)]
    public string? NavRouteName { get; set; }

    /// <summary>
    /// Gets or sets the CSS class to add when the link is active (default: "active").
    /// </summary>
    [HtmlAttributeName(NavActiveClassAttributeName)]
    public string ActiveClass { get; set; } = "active";

    /// <summary>
    /// Gets or sets the matching mode: "contains" (default), "exact", or "startswith".
    /// Only applies when using nav-active-section (not nav-route-name).
    /// </summary>
    [HtmlAttributeName(NavMatchModeAttributeName)]
    public string MatchMode { get; set; } = "contains";

    /// <summary>
    /// Order for this tag helper. Set to run after built-in tag helpers (e.g., asp-page).
    /// </summary>
    public override int Order => 100;

    /// <summary>
    /// Processes the tag by adding the active class if the current page matches the nav section or route name.
    /// </summary>
    /// <param name="context">The tag helper context.</param>
    /// <param name="output">The tag helper output.</param>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext == null)
        {
            return;
        }

        // If neither section nor route name specified, nothing to do
        if (string.IsNullOrWhiteSpace(NavSection) && string.IsNullOrWhiteSpace(NavRouteName))
        {
            return;
        }

        var currentPage = ViewContext.RouteData.Values["page"]?.ToString();
        var contentTypeDeveloperName = ViewContext
            .RouteData.Values["contentTypeDeveloperName"]
            ?.ToString();

        bool isActive = false;

        // Route name matching takes precedence
        if (!string.IsNullOrWhiteSpace(NavRouteName))
        {
            isActive = string.Equals(currentPage, NavRouteName, StringComparison.OrdinalIgnoreCase);
        }
        else if (!string.IsNullOrWhiteSpace(NavSection))
        {
            isActive = IsActive(currentPage, NavSection, contentTypeDeveloperName);
        }

        if (isActive)
        {
            var existingClass = output.Attributes.FirstOrDefault(a => a.Name == "class");
            if (existingClass != null)
            {
                var existingClassValue = existingClass.Value?.ToString() ?? string.Empty;
                output.Attributes.SetAttribute(
                    "class",
                    $"{existingClassValue} {ActiveClass}".Trim()
                );
            }
            else
            {
                output.Attributes.SetAttribute("class", ActiveClass);
            }

            // Add ARIA attribute for accessibility
            output.Attributes.SetAttribute("aria-current", "page");
        }

        // Remove the custom attributes so they don't appear in the rendered HTML
        output.Attributes.RemoveAll(NavActiveSectionAttributeName);
        output.Attributes.RemoveAll(NavActiveClassAttributeName);
        output.Attributes.RemoveAll(NavRouteNameAttributeName);
        output.Attributes.RemoveAll(NavMatchModeAttributeName);
    }

    /// <summary>
    /// Determines whether the current page matches the specified navigation section.
    /// </summary>
    /// <param name="currentPage">The current page route path.</param>
    /// <param name="navSection">The navigation section identifier.</param>
    /// <param name="contentTypeDeveloperName">The content type developer name from route data.</param>
    /// <returns>True if the link should be marked as active; otherwise, false.</returns>
    private bool IsActive(string? currentPage, string navSection, string? contentTypeDeveloperName)
    {
        if (string.IsNullOrWhiteSpace(currentPage))
        {
            return false;
        }

        // Normalize the current page path for comparison
        var normalizedPage = currentPage.ToLowerInvariant();
        var normalizedSection = navSection.ToLowerInvariant();

        // Special case handling for specific sections
        var isSpecialMatch = normalizedSection switch
        {
            "configuration" => normalizedPage.Contains(
                "/contenttypes/configuration",
                StringComparison.OrdinalIgnoreCase
            ),
            "fields" => normalizedPage.Contains(
                "/contenttypes/fields",
                StringComparison.OrdinalIgnoreCase
            ),
            "deleteditems" or "deleted" => normalizedPage.Contains(
                "/contenttypes/deleted",
                StringComparison.OrdinalIgnoreCase
            ),
            "views" => normalizedPage.Contains(
                "/contenttypes/views",
                StringComparison.OrdinalIgnoreCase
            ),
            "contentitems" => normalizedPage.Contains(
                "/contentitems",
                StringComparison.OrdinalIgnoreCase
            ) && !normalizedPage.Contains("/contenttypes", StringComparison.OrdinalIgnoreCase),
            _ => (bool?)null, // Not a special case
        };

        if (isSpecialMatch.HasValue)
        {
            return isSpecialMatch.Value;
        }

        // Apply match mode for general cases
        return MatchMode.ToLowerInvariant() switch
        {
            "exact" => normalizedPage.Equals(normalizedSection, StringComparison.OrdinalIgnoreCase),
            "startswith" => normalizedPage.StartsWith(
                normalizedSection,
                StringComparison.OrdinalIgnoreCase
            ),
            _ => normalizedPage.Contains(normalizedSection, StringComparison.OrdinalIgnoreCase),
        };
    }
}

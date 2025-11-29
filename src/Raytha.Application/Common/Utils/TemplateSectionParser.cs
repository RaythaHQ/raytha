using System.Text.RegularExpressions;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// Utility for parsing Liquid templates to extract section names.
/// </summary>
public static class TemplateSectionParser
{
    // Patterns to match render_section() and get_section() function call syntaxes:
    // {{ render_section("main") }}
    // {{ render_section('main') }}
    // {{ get_section("main") }}
    // {{ get_section('main') }}
    // {% for widget in get_section("sidebar") %}
    private static readonly Regex SectionPattern = new(
        @"(?:render_section|get_section)\s*\(\s*['""]([^'""]+)['""]\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    // Pattern to match Liquid comments: {% comment %}...{% endcomment %}
    private static readonly Regex LiquidCommentPattern = new(
        @"\{%\s*comment\s*%\}.*?\{%\s*endcomment\s*%\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
    );

    // Pattern to match HTML comments: <!-- ... -->
    private static readonly Regex HtmlCommentPattern = new(
        @"<!--.*?-->",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    /// <summary>
    /// Extracts all section names from a Liquid template string.
    /// Looks for both render_section() and get_section() function calls.
    /// Comments (HTML and Liquid) are stripped before parsing to avoid
    /// matching section names mentioned in documentation.
    /// </summary>
    /// <param name="templateContent">The template content to parse</param>
    /// <returns>A list of unique section names found in the template</returns>
    public static IReadOnlyList<string> ExtractSectionNames(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
            return Array.Empty<string>();

        // Strip comments before parsing to avoid matching documentation examples
        var contentWithoutComments = LiquidCommentPattern.Replace(templateContent, string.Empty);
        contentWithoutComments = HtmlCommentPattern.Replace(contentWithoutComments, string.Empty);

        var matches = SectionPattern.Matches(contentWithoutComments);
        var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var sectionName = match.Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(sectionName))
                {
                    sections.Add(sectionName);
                }
            }
        }

        return sections.ToList();
    }
}

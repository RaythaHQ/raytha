using System.Text.RegularExpressions;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// Utility for parsing Liquid templates to extract section names.
/// </summary>
public static class TemplateSectionParser
{
    // Patterns to match various section() function call syntaxes:
    // {{ section "main" }}
    // {{ section 'main' }}
    // {{ section("main") }}
    // {{ section('main') }}
    private static readonly Regex SectionPattern = new(
        @"section\s*[\(""]?\s*['""]([^'""]+)['""][\)""]?\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Extracts all section names from a Liquid template string.
    /// </summary>
    /// <param name="templateContent">The template content to parse</param>
    /// <returns>A list of unique section names found in the template</returns>
    public static IReadOnlyList<string> ExtractSectionNames(string templateContent)
    {
        if (string.IsNullOrWhiteSpace(templateContent))
            return Array.Empty<string>();

        var matches = SectionPattern.Matches(templateContent);
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


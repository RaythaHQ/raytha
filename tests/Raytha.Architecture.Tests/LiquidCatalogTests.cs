using System.Text.RegularExpressions;
using FluentAssertions;
using Raytha.Web.Services;

namespace Raytha.Architecture.Tests;

/// <summary>
/// The admin's Liquid editors insert the function snippets from <c>liquid-catalog.ts</c>; a
/// snippet the server's parser rejects makes the template fail to save.
/// </summary>
public class LiquidCatalogTests
{
    private static readonly Regex FunctionTemplate = new(
        """template:\s*(?:'(?<t>get_[^']*|raytha_[^']*|render_[^']*)'|"(?<t>get_[^"]*|raytha_[^"]*|render_[^"]*)")"""
    );
    private static readonly Regex TabStop = new(@"\$\{([^}]*)\}");

    [Test]
    public void Every_catalog_function_snippet_parses()
    {
        var catalog = File.ReadAllText(
            Path.Combine(RepoRoot(), "src/admin/apps/shell/src/pages/editors/liquid-catalog.ts")
        );
        var snippets = FunctionTemplate.Matches(catalog).Select(m => m.Groups["t"].Value).ToList();
        snippets.Should().Contain(s => s.StartsWith("get_content_items("));

        var parser = new LiquidTemplateParser();
        foreach (var snippet in snippets)
        {
            var expanded = TabStop.Replace(snippet, m => m.Groups[1].Value);
            parser
                .GetSyntaxError($"{{% assign probe = {expanded} %}}")
                .Should()
                .BeNull($"the editor inserts {snippet}");
        }
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Raytha.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raytha.sln not found");
    }
}

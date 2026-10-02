using FluentAssertions;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.UnitTests.Common.Utils;

public class RoutePathsTests
{
    [TestCase("raytha", "raytha")]
    [TestCase("Raytha/users", "Raytha")]
    [TestCase("account/billing", "account")]
    [TestCase("api/v2", "api")]
    [TestCase("healthz", "healthz")]
    [TestCase("_static-files/a.png", "_static-files")]
    [TestCase("favicon.ico", "favicon.ico")]
    [TestCase("raytha_default_2026/site.css", "raytha_default_2026")]
    public void A_path_under_a_root_the_host_answers_first_is_reserved(string path, string root)
    {
        RoutePaths.ReservedRoot(path).Should().Be(root);
    }

    [TestCase("about")]
    [TestCase("accounts")]
    [TestCase("blog/raytha")]
    [TestCase("my-raytha")]
    [TestCase("healthz-tips")]
    public void Other_paths_are_not_reserved(string path)
    {
        RoutePaths.ReservedRoot(path).Should().BeNull();
    }
}

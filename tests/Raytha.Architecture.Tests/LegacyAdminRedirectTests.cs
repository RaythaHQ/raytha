using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Raytha.Web.AdminSpa;

namespace Raytha.Architecture.Tests;

[TestFixture]
public class LegacyAdminRedirectTests
{
    [TestCase("/raytha/posts", "content/posts", "posts")]
    [TestCase("/raytha/posts/tDofqaGRJESssWjQRZOmVw", "content/posts/tDofqaGRJESssWjQRZOmVw", "posts")]
    [TestCase("/raytha/posts/create", "content/posts/new", "posts")]
    [TestCase("/raytha/posts/edit/fvKgASCku3WdCG1j0X1chw", "content/posts/items/fvKgASCku3WdCG1j0X1chw", "posts")]
    [TestCase("/raytha/posts/edit/fvKgASCku3WdCG1j0X1chw/revisions", "content/posts/items/fvKgASCku3WdCG1j0X1chw", "posts")]
    [TestCase("/raytha/posts/fields/edit/abc", "content-types/posts/fields/abc", "posts")]
    [TestCase("/raytha/posts/views", "content/posts/views", "posts")]
    [TestCase("/raytha/posts/views/filter/abc", "content/posts/views/abc#filter", "posts")]
    [TestCase("/raytha/posts/views/public-settings/abc", "content/posts/views/abc#public", "posts")]
    [TestCase("/raytha/site-pages/p1/widget/edit", "site-pages/p1/layout", null)]
    [TestCase("/raytha/posts/begin-import-from-csv", "content/posts/import", "posts")]
    [TestCase("/raytha/users/edit/abc", "users/abc", null)]
    [TestCase("/raytha/users/create", "users/new", null)]
    [TestCase("/raytha/audit-logs", "audit-log", null)]
    [TestCase("/raytha/settings/authentication-schemes/edit/abc", "settings/authentication/abc", null)]
    [TestCase("/raytha/themes/edit/t1/web-templates/edit/w1", "themes/t1/web-templates/w1", null)]
    [TestCase("/raytha/menus/edit/m1/menu-items/edit/i1", "menus/m1/items/i1", null)]
    [TestCase("/raytha/site-pages/layout/p1", "site-pages/p1/layout", null)]
    public void Maps_a_1_5_admin_page_to_its_spa_page(string path, string expected, string? contentType)
    {
        LegacyAdminRedirects.Resolve(new PathString(path)).Should().Be(new LegacyAdminRedirects.Target(expected, contentType));
    }

    [TestCase("/raytha")]
    [TestCase("/raytha/")]
    [TestCase("/raytha/users")]
    [TestCase("/raytha/users/abc")]
    [TestCase("/raytha/settings/admins")]
    [TestCase("/raytha/content/posts")]
    [TestCase("/raytha/content/posts/items/abc")]
    [TestCase("/raytha/media")]
    [TestCase("/raytha/webhooks/abc")]
    [TestCase("/raytha/assets/index-abc.js")]
    [TestCase("/raytha/favicon.ico")]
    [TestCase("/raytha/api/admin/users")]
    [TestCase("/raytha/login/magic-link")]
    [TestCase("/raytha/posts/delete/abc")]
    [TestCase("/posts/edit/abc")]
    public void Leaves_2_0_and_server_paths_alone(string path)
    {
        LegacyAdminRedirects.Resolve(new PathString(path)).Should().BeNull();
    }

    [Test]
    public void Every_spa_top_level_segment_is_reserved()
    {
        var router = File.ReadAllText(Path.Combine(RepoRoot(), "src/admin/apps/shell/src/router.tsx"));
        var segments = Regex
            .Matches(router, "\\bpath: \"/([a-z-]+)")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        segments.Should().NotBeEmpty();
        segments.Should().OnlyContain(s => LegacyAdminRedirects.ReservedSegments.Contains(s));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Raytha.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Raytha.sln not found above the test directory.");
    }
}

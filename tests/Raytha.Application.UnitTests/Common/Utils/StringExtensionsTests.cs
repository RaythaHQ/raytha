using FluentAssertions;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.UnitTests.Common.Utils;

public class StringExtensionsTests
{
    [Test]
    [TestCase("Hello World", "Hello-World")]
    [TestCase("  Hello   World  ", "Hello-World")]
    [TestCase("Hello/World", "Hello/World")]
    [TestCase("Hello---World", "Hello-World")]
    [TestCase("Café", "Cafe")]
    [TestCase("schön", "schon")]
    [TestCase("simple", "simple")]
    [TestCase("Mixed CASE", "Mixed-CASE")]
    public void ToUrlSlug_ShouldReturnCorrectSlug(string input, string expected)
    {
        var result = input.ToUrlSlug();
        result.Should().Be(expected);
    }

    [Test]
    public void ToUrlSlug_ShouldReturnEmpty_WhenInputIsNull()
    {
        string input = null;
        var result = input.ToUrlSlug();
        result.Should().BeEmpty();
    }

    [Test]
    [TestCase("Hello World", "hello_world")]
    [TestCase("Hello-World", "hello_world")]
    [TestCase("Hello!World", "hello_world")]
    [TestCase(" Developer Name ", "developer_name")]
    [TestCase("123 Name", "123_name")]
    public void ToDeveloperName_ShouldReturnCorrectDeveloperName(string input, string expected)
    {
        var result = input.ToDeveloperName();
        result.Should().Be(expected);
    }

    [Test]
    public void ToDeveloperName_ShouldReturnEmpty_WhenInputIsNull()
    {
        string input = null;
        var result = input.ToDeveloperName();
        result.Should().BeEmpty();
    }

    [Test]
    [TestCase("raytha", true)]
    [TestCase("raytha/admin", true)]
    [TestCase("account/login", true)]
    [TestCase("account/logout", true)]
    [TestCase("account/me", true)]
    [TestCase("account/create", true)]
    [TestCase("blog", false)]
    [TestCase("about-us", false)]
    [TestCase("contact", false)]
    public void IsProtectedRoutePath_ShouldReturnCorrectResult(string input, bool expected)
    {
        var result = input.IsProtectedRoutePath();
        result.Should().Be(expected);
    }
}


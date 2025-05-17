using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BuiltInWebTemplateTests
{
    [Test]
    [TestCase("raytha_html_base_layout")]
    [TestCase("raytha_html_base_login_layout")]
    [TestCase("raytha_html_error_403")]
    [TestCase("raytha_html_error_404")]
    [TestCase("raytha_html_error_500")]
    [TestCase("raytha_html_home")]
    [TestCase("raytha_html_content_item_list")]
    [TestCase("raytha_html_content_item_detail")]
    [TestCase("raytha_html_login_emailandpassword")]
    [TestCase("raytha_html_login_magiclink")]
    [TestCase("raytha_html_login_magiclinksent")]
    [TestCase("raytha_html_user_registration")]
    [TestCase("raytha_html_user_registration_success")]
    [TestCase("raytha_html_changeprofile")]
    [TestCase("raytha_html_changepassword")]
    [TestCase("raytha_html_forgotpassword")]
    [TestCase("raytha_html_forgotpasswordcomplete")]
    [TestCase("raytha_html_forgotpassword_reset_link_sent")]
    [TestCase("raytha_html_forgotpasswordsuccess")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BuiltInWebTemplate.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = BuiltInWebTemplate._Layout;

        type.Should().Be("raytha_html_base_layout");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (BuiltInWebTemplate)"raytha_html_base_layout";

        type.Should().Be(BuiltInWebTemplate._Layout);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BuiltInWebTemplate.From("BadValue"))
            .Should()
            .Throw<UnsupportedTemplateTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BuiltInWebTemplate.Templates.Count().Should().Be(19);
    }
}

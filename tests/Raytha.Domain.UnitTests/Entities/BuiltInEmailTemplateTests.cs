using FluentAssertions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class BuiltInEmailTemplateTests
{
    [Test]
    [TestCase("raytha_email_admin_welcome")]
    [TestCase("raytha_email_admin_passwordchanged")]
    [TestCase("raytha_email_admin_passwordreset")]
    [TestCase("raytha_email_login_beginloginwithmagiclink")]
    [TestCase("raytha_email_login_beginforgotpassword")]
    [TestCase("raytha_email_login_completedforgotpassword")]
    [TestCase("raytha_email_user_welcome")]
    [TestCase("raytha_email_user_passwordchanged")]
    [TestCase("raytha_email_user_passwordreset")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = BuiltInEmailTemplate.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = BuiltInEmailTemplate.AdminWelcomeEmail;

        type.Should().Be("raytha_email_admin_welcome");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (BuiltInEmailTemplate)"raytha_email_admin_welcome";

        type.Should().Be(BuiltInEmailTemplate.AdminWelcomeEmail);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => BuiltInEmailTemplate.From("BadValue"))
            .Should()
            .Throw<UnsupportedTemplateTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        BuiltInEmailTemplate.Templates.Count().Should().Be(9);
    }
}

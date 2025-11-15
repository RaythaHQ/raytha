using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class AuthenticationSchemeTypeTests
{
    [Test]
    [TestCase("magic_link")]
    [TestCase("email_and_password")]
    [TestCase("jwt")]
    [TestCase("saml")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = AuthenticationSchemeType.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase("magic_link", "Magic link")]
    [TestCase("email_and_password", "Email and password")]
    [TestCase("jwt", "Json web token")]
    [TestCase("saml", "SAML")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = AuthenticationSchemeType.From(developerName);
        type.ToString().Should().Be(label);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = AuthenticationSchemeType.MagicLink;

        type.Should().Be("magic_link");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (AuthenticationSchemeType)"magic_link";

        type.Should().Be(AuthenticationSchemeType.MagicLink);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions
            .Invoking(() => AuthenticationSchemeType.From("BadValue"))
            .Should()
            .Throw<UnsupportedAuthenticationSchemeTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        AuthenticationSchemeType.SupportedTypes.Count().Should().Be(4);
    }
}

using FluentAssertions;
using Raytha.Domain.Exceptions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Domain.UnitTests.ValueObjects;

public class VerificationCodeTypeTests
{
    [Test]
    [TestCase("forgot_password")]
    [TestCase("reset_email")]
    [TestCase("verify_email")]
    [Parallelizable(ParallelScope.All)]
    public void ShouldReturnCorrectDeveloperName(string developerName)
    {
        var type = VerificationCodeType.From(developerName);
        type.DeveloperName.Should().Be(developerName);
    }

    [Test]
    [TestCase("forgot_password", "Forgot password")]
    [TestCase("reset_email", "Reset email")]
    [TestCase("verify_email", "Verify email")]
    [Parallelizable(ParallelScope.All)]
    public void ToStringShouldMatchLabel(string developerName, string label)
    {
        var type = VerificationCodeType.From(developerName);
        type.ToString().Should().Be(label);
    }

    [Test]
    public void ShouldPerformImplicitConversionToString()
    {
        string type = VerificationCodeType.ForgotPassword;

        type.Should().Be("forgot_password");
    }

    [Test]
    public void ShouldPerformExplicitConversionGivenSupportedType()
    {
        var type = (VerificationCodeType)"forgot_password";

        type.Should().Be(VerificationCodeType.ForgotPassword);
    }

    [Test]
    public void ShouldThrowUnsupportedColourExceptionGivenNotSupportedColourCode()
    {
        FluentActions.Invoking(() => VerificationCodeType.From("BadValue"))
            .Should().Throw<UnsupportedVerificationCodeTypeException>();
    }

    [Test]
    public void ShouldMatchNumberOfSupportedTypes()
    {
        VerificationCodeType.SupportedTypes.Count().Should().Be(3);
    }
}

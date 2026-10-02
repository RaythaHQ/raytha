using FluentAssertions;
using Moq;
using NUnit.Framework;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.OrganizationSettings.Commands;

namespace Raytha.Application.UnitTests.OrganizationSettings;

[TestFixture]
public class InitialSetupValidatorTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Setup_requires_a_website_url(string? websiteUrl)
    {
        var result = Validate(websiteUrl!);

        result.Errors.Should()
            .ContainSingle(e => e.PropertyName == nameof(InitialSetup.Command.WebsiteUrl))
            .Which.ErrorMessage.Should()
            .Be("Enter the website URL, for example https://www.example.com.");
    }

    [Test]
    public void Setup_rejects_a_website_url_that_is_not_absolute()
    {
        var result = Validate("cms.example.com");

        result.Errors.Should()
            .ContainSingle(e => e.PropertyName == nameof(InitialSetup.Command.WebsiteUrl))
            .Which.ErrorMessage.Should()
            .Be("cms.example.com must be a valid URI format.");
    }

    [Test]
    public void Setup_accepts_a_website_url_with_a_path_base()
    {
        Validate("https://cms.example.com/site").IsValid.Should().BeTrue();
    }

    private static FluentValidation.Results.ValidationResult Validate(string websiteUrl) =>
        new InitialSetup.Validator(Mock.Of<IEmailerConfiguration>()).Validate(
            new InitialSetup.Command
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                SuperAdminEmailAddress = "ada@example.com",
                SuperAdminPassword = "correct-horse",
                OrganizationName = "Example",
                WebsiteUrl = websiteUrl,
                TimeZone = "Etc/UTC",
                SmtpDefaultFromAddress = "no-reply@example.com",
                SmtpDefaultFromName = "Example",
            }
        );
}

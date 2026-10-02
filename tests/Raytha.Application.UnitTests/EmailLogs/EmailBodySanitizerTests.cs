using FluentAssertions;
using Raytha.Application.EmailLogs;

namespace Raytha.Application.UnitTests.EmailLogs;

public class EmailBodySanitizerTests
{
    [Test]
    public void Sanitize_ForgotPasswordPath_RedactsToken()
    {
        var body =
            "<a href=\"https://site/raytha/login/forgot-password/complete/CfDJ8AbC123\">reset</a>";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("CfDJ8AbC123");
        sanitized.Should().Contain("/forgot-password/complete/[redacted]");
        sanitized.Should().Contain("https://site/raytha/login");
    }

    [Test]
    public void Sanitize_MagicLinkPath_RedactsToken()
    {
        var body =
            "Login: https://site/account/login/magic-link/complete/abc.def-ghi_jkl?returnUrl=/raytha";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("abc.def-ghi_jkl");
        sanitized.Should().Contain("/magic-link/complete/[redacted]");
        sanitized.Should().Contain("returnUrl=/raytha");
    }

    [Test]
    public void Sanitize_ResetPasswordHtmlBody_RedactsQueryTokenButKeepsTheRest()
    {
        var body =
            "<p><a href=\"https://app.example.com/auth/reset-password"
            + "?email=user%40example.com&amp;token=CfDJ8AbC123\">Reset your password</a></p>";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("CfDJ8AbC123");
        sanitized.Should().Contain("&amp;token=[redacted]");
        sanitized.Should().Contain("email=user%40example.com");
        sanitized.Should().Contain("Reset your password");
    }

    [Test]
    public void Sanitize_HtmlBodyWithRawAmpersand_StillRedactsToken()
    {
        var body = "<a href=\"https://app.example.com/auth/reset?email=a%40b.c&token=LIVETOKEN\">Reset</a>";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("LIVETOKEN");
        sanitized.Should().Contain("&token=[redacted]");
    }

    [Test]
    [TestCase("code")]
    [TestCase("otp")]
    [TestCase("key")]
    [TestCase("secret")]
    [TestCase("signature")]
    [TestCase("password")]
    [TestCase("TOKEN")]
    public void Sanitize_RedactsSensitiveQueryParameterNames(string name)
    {
        var body = $"https://x.example/cb?{name}=sensitive-value&next=/home";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("sensitive-value");
        sanitized.Should().Contain("next=/home");
    }

    [Test]
    [TestCase("&amp;")]
    [TestCase("&#38;")]
    [TestCase("&#x26;")]
    public void Sanitize_HtmlEncodedAmpersand_RedactsToken(string amp)
    {
        var body = $"<a href=\"https://x.example/cb?email=a%40b.c{amp}token=sensitive-value\">Go</a>";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("sensitive-value");
        sanitized.Should().Contain($"{amp}token=[redacted]");
    }

    [Test]
    public void Sanitize_AdminResetPasswordLine_RedactsThePassword()
    {
        var body = "<p>Email address: admin@site.com <br/>\n  Password: SuperSecret9</p>";

        var sanitized = EmailBodySanitizer.Sanitize(body);

        sanitized.Should().NotContain("SuperSecret9");
        sanitized.Should().Contain("Password: [redacted]");
        sanitized.Should().Contain("admin@site.com");
    }

    [Test]
    public void Sanitize_BodyWithoutSecrets_IsUnchanged()
    {
        var body = "<p>Welcome! Visit https://app.example.com/docs?page=2 to get started.</p>";

        EmailBodySanitizer.Sanitize(body).Should().Be(body);
    }

    [Test]
    public void Sanitize_NullOrEmpty_ReturnsEmpty()
    {
        EmailBodySanitizer.Sanitize(null).Should().BeEmpty();
        EmailBodySanitizer.Sanitize(string.Empty).Should().BeEmpty();
    }
}

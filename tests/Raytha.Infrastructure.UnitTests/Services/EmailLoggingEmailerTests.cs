using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Common;
using Raytha.Infrastructure.Services;

namespace Raytha.Infrastructure.UnitTests.Services;

public class EmailLoggingEmailerTests
{
    [Test]
    public void Sanitize_RedactsTokenBearingQueryParameters()
    {
        var body =
            "<a href=\"https://site/raytha/reset?token=abc123&amp;x=1\">reset</a> and https://site/login?code=999";

        var sanitized = EmailLoggingEmailer.Sanitize(body);

        sanitized.Should().NotContain("abc123").And.NotContain("999");
        sanitized.Should().Contain("token=[redacted]").And.Contain("code=[redacted]");
        sanitized.Should().Contain("x=1", "non-sensitive parameters are preserved");
    }

    [Test]
    public void Sanitize_RedactsMagicLinkPathTokens()
    {
        var body = "https://site/raytha/login/magic-link/complete/live-token-value";

        EmailLoggingEmailer.Sanitize(body).Should().NotContain("live-token-value");
    }

    [Test]
    public void Sanitize_RedactsForgotPasswordPathAndLabeledPassword()
    {
        var body =
            "<a href=\"https://site/account/login/forgot-password/complete/CfDJ8AbC123\">reset</a>"
            + "<p>Password: SuperSecret9</p>";

        var sanitized = EmailLoggingEmailer.Sanitize(body);

        sanitized.Should().NotContain("CfDJ8AbC123").And.NotContain("SuperSecret9");
        sanitized.Should().Contain("/forgot-password/complete/[redacted]");
        sanitized.Should().Contain("Password: [redacted]");
    }

    [Test]
    public void Sanitize_TruncatesOversizedBodies()
    {
        var body = new string('a', EmailLoggingEmailer.MaxBodyLength + 500);

        EmailLoggingEmailer.Sanitize(body).Should().HaveLength(EmailLoggingEmailer.MaxBodyLength);
    }

    [Test]
    public void Sanitize_HandlesNullOrEmpty()
    {
        EmailLoggingEmailer.Sanitize(null).Should().BeEmpty();
        EmailLoggingEmailer.Sanitize(string.Empty).Should().BeEmpty();
    }

    [Test]
    public void RedactSensitiveContent_StripsDeclaredValuesFromAnyTemplateShape()
    {
        var body = "<p>Your sign-in code is <strong>123456</strong>. It expires soon.</p>";

        var redacted = EmailLoggingEmailer.RedactSensitiveContent(body, new[] { "123456" });

        redacted.Should().NotContain("123456");
        redacted.Should().Contain("<strong>[redacted]</strong>");
    }

    [Test]
    public void RedactSensitiveContent_IgnoresNullAndEmptyValues()
    {
        var body = "nothing secret here";

        EmailLoggingEmailer
            .RedactSensitiveContent(body, new[] { string.Empty, null! })
            .Should()
            .Be(body);
        EmailLoggingEmailer.RedactSensitiveContent(body, null).Should().Be(body);
        EmailLoggingEmailer.RedactSensitiveContent(null, new[] { "123456" }).Should().BeEmpty();
    }

    [Test]
    public void SendEmail_PropagatesInnerFailure_EvenWhenLoggingScopeIsUnavailable()
    {
        var inner = new Mock<IEmailer>();
        inner.Setup(e => e.SendEmail(It.IsAny<EmailMessage>())).Throws(new InvalidOperationException("smtp"));
        // A scope factory whose scope has no DbContext: the log write fails and must be swallowed.
        var scopeFactory = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var emailer = new EmailLoggingEmailer(inner.Object, scopeFactory, NullLogger<EmailLoggingEmailer>.Instance);

        var act = () => emailer.SendEmail(EmailMessage.From("s", "c", "to@x.com", "from@x.com", "From"));

        act.Should().Throw<InvalidOperationException>().WithMessage("smtp");
    }

    [Test]
    public void SendEmail_SucceedsWhenInnerSucceeds_EvenWhenLoggingScopeIsUnavailable()
    {
        var inner = new Mock<IEmailer>();
        var scopeFactory = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var emailer = new EmailLoggingEmailer(inner.Object, scopeFactory, NullLogger<EmailLoggingEmailer>.Instance);

        var act = () => emailer.SendEmail(EmailMessage.From("s", "c", "to@x.com", "from@x.com", "From"));

        act.Should().NotThrow();
        inner.Verify(e => e.SendEmail(It.IsAny<EmailMessage>()), Times.Once);
    }
}

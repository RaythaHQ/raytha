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

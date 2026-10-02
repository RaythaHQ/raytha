using FluentAssertions;
using Raytha.Application.Webhooks;

namespace Raytha.Application.UnitTests.Webhooks;

public class WebhookSignerTests
{
    private const string Timestamp = "2026-10-01T20:00:00.0000000Z";

    [Test]
    public void Sign_ProducesHexSha256Prefix_AndIsDeterministic()
    {
        var first = WebhookSigner.Sign(Timestamp, "{\"a\":1}", "secret");
        var second = WebhookSigner.Sign(Timestamp, "{\"a\":1}", "secret");

        first.Should().StartWith("sha256=");
        first.Should().HaveLength("sha256=".Length + 64);
        first.Should().Be(second);
    }

    [Test]
    public void Sign_ChangesWhenTimestampPayloadOrSecretChanges()
    {
        var baseline = WebhookSigner.Sign(Timestamp, "payload", "secret");

        WebhookSigner.Sign("2026-10-01T20:00:01.0000000Z", "payload", "secret").Should().NotBe(baseline);
        WebhookSigner.Sign(Timestamp, "payload!", "secret").Should().NotBe(baseline);
        WebhookSigner.Sign(Timestamp, "payload", "other").Should().NotBe(baseline);
    }

    [Test]
    public void Verify_AcceptsMatchingSignature_AndRejectsTamperedOnes()
    {
        var signature = WebhookSigner.Sign(Timestamp, "payload", "secret");

        WebhookSigner.Verify(Timestamp, "payload", "secret", signature).Should().BeTrue();
        WebhookSigner.Verify("2026-10-02T20:00:00.0000000Z", "payload", "secret", signature).Should().BeFalse();
        WebhookSigner.Verify(Timestamp, "payload2", "secret", signature).Should().BeFalse();
        WebhookSigner.Verify(Timestamp, "payload", "wrong", signature).Should().BeFalse();
        WebhookSigner.Verify(Timestamp, "payload", "secret", string.Empty).Should().BeFalse();
    }

    [Test]
    public void GenerateSecret_Produces64HexChars_AndIsRandom()
    {
        var a = WebhookSigner.GenerateSecret();
        var b = WebhookSigner.GenerateSecret();

        a.Should().HaveLength(64).And.MatchRegex("^[0-9a-f]+$");
        a.Should().NotBe(b);
    }
}

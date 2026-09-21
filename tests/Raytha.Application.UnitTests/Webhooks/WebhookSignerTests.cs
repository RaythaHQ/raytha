using FluentAssertions;
using Raytha.Application.Webhooks;

namespace Raytha.Application.UnitTests.Webhooks;

public class WebhookSignerTests
{
    [Test]
    public void Sign_ProducesHexSha256Prefix_AndIsDeterministic()
    {
        var first = WebhookSigner.Sign("{\"a\":1}", "secret");
        var second = WebhookSigner.Sign("{\"a\":1}", "secret");

        first.Should().StartWith("sha256=");
        first.Should().HaveLength("sha256=".Length + 64);
        first.Should().Be(second);
    }

    [Test]
    public void Sign_ChangesWhenPayloadOrSecretChanges()
    {
        var baseline = WebhookSigner.Sign("payload", "secret");

        WebhookSigner.Sign("payload!", "secret").Should().NotBe(baseline);
        WebhookSigner.Sign("payload", "other").Should().NotBe(baseline);
    }

    [Test]
    public void Verify_AcceptsMatchingSignature_AndRejectsTamperedOnes()
    {
        var signature = WebhookSigner.Sign("payload", "secret");

        WebhookSigner.Verify("payload", "secret", signature).Should().BeTrue();
        WebhookSigner.Verify("payload2", "secret", signature).Should().BeFalse();
        WebhookSigner.Verify("payload", "wrong", signature).Should().BeFalse();
        WebhookSigner.Verify("payload", "secret", string.Empty).Should().BeFalse();
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

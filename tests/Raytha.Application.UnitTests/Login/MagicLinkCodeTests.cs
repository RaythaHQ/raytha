using FluentAssertions;
using Raytha.Application.Login;

namespace Raytha.Application.UnitTests.Login;

public class MagicLinkCodeTests
{
    [Test]
    public void Generate_ProducesSixDigits()
    {
        for (var i = 0; i < 100; i++)
        {
            var code = MagicLinkCode.Generate();
            code.Should().HaveLength(6);
            code.Should().MatchRegex("^[0-9]{6}$");
        }
    }

    [Test]
    public void Normalize_StripsEverythingButDigits()
    {
        MagicLinkCode.Normalize(" 123 456 ").Should().Be("123456");
        MagicLinkCode.Normalize("12-34-56").Should().Be("123456");
        MagicLinkCode.Normalize(null).Should().BeEmpty();
        MagicLinkCode.Normalize("   ").Should().BeEmpty();
        MagicLinkCode.Normalize("abc").Should().BeEmpty();
    }

    [Test]
    public void OtpId_IsDeterministicPerUserAndCode()
    {
        var userId = Guid.NewGuid();

        MagicLinkCode.OtpId(userId, "123456").Should().Equal(MagicLinkCode.OtpId(userId, "123456"));
        MagicLinkCode
            .OtpId(userId, "123456")
            .Should()
            .NotEqual(MagicLinkCode.OtpId(userId, "654321"));
        MagicLinkCode
            .OtpId(userId, "123456")
            .Should()
            .NotEqual(MagicLinkCode.OtpId(Guid.NewGuid(), "123456"));
    }

    [Test]
    public void OtpId_CannotBeForgedByShiftingDigitsBetweenUserAndCode()
    {
        // The user id is formatted without dashes and joined with ':', so the
        // hash input is unambiguous; a code prefixed with hex characters must
        // not collide with another user's input.
        var userId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var otherUser = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

        MagicLinkCode
            .OtpId(userId, "123456")
            .Should()
            .NotEqual(MagicLinkCode.OtpId(otherUser, "23456"));
    }
}

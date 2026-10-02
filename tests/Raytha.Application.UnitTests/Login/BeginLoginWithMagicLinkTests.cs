using CSharpVitamins;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Login.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.Login;

/// <summary>
/// A magic-link request answers the same way whether or not the address belongs to an account
/// that may sign in, so the form cannot be used to discover accounts.
/// </summary>
[TestFixture]
public class BeginLoginWithMagicLinkTests
{
    private const string Email = "person@example.com";

    private AuthenticationScheme _scheme = null!;
    private List<User> _users = null!;
    private List<OneTimePassword> _added = null!;
    private Mock<IRaythaDbContext> _db = null!;

    [SetUp]
    public void SetUp()
    {
        _scheme = new AuthenticationScheme
        {
            Id = Guid.NewGuid(),
            DeveloperName = AuthenticationSchemeType.MagicLink.DeveloperName,
            AuthenticationSchemeType = AuthenticationSchemeType.MagicLink,
            IsEnabledForUsers = true,
            IsEnabledForAdmins = true,
            MagicLinkExpiresInSeconds = 900,
        };
        _users = [];
        _added = [];

        _db = new Mock<IRaythaDbContext>();
        _db.Setup(x => x.AuthenticationSchemes)
            .Returns(() => new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Users).Returns(() => _users.AsQueryable().BuildMockDbSet().Object);
        var otps = new List<OneTimePassword>().AsQueryable().BuildMockDbSet();
        otps.Setup(x => x.Add(It.IsAny<OneTimePassword>())).Callback<OneTimePassword>(_added.Add);
        _db.Setup(x => x.OneTimePasswords).Returns(otps.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Test]
    public async Task An_unknown_address_is_accepted_and_sends_nothing()
    {
        await ShouldLookSentButSendNothing();
    }

    [Test]
    public async Task A_deactivated_account_is_accepted_and_sends_nothing()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = false });

        await ShouldLookSentButSendNothing();
    }

    [Test]
    public async Task An_admin_is_accepted_and_sends_nothing_when_the_scheme_is_off_for_admins()
    {
        _scheme.IsEnabledForAdmins = false;
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true, IsAdmin = true });

        await ShouldLookSentButSendNothing();
    }

    [Test]
    public async Task A_public_user_is_accepted_and_sends_nothing_when_the_scheme_is_off_for_users()
    {
        _scheme.IsEnabledForUsers = false;
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        await ShouldLookSentButSendNothing();
    }

    [Test]
    public async Task An_eligible_account_gets_a_one_time_code()
    {
        var user = new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true };
        _users.Add(user);

        var response = await Send();

        response.Success.Should().BeTrue();
        _added.Should().ContainSingle().Which.UserId.Should().Be(user.Id);
        user.DomainEvents.Should().ContainSingle();
    }

    [Test]
    public void A_scheme_disabled_for_everyone_is_still_refused()
    {
        _scheme.IsEnabledForUsers = false;
        _scheme.IsEnabledForAdmins = false;

        var result = new BeginLoginWithMagicLink.Validator(_db.Object).Validate(Command());

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Authentication scheme is disabled.");
    }

    private async Task ShouldLookSentButSendNothing()
    {
        var response = await Send();

        response.Success.Should().BeTrue();
        _added.Should().BeEmpty();
        _users.SelectMany(u => u.DomainEvents).Should().BeEmpty();
    }

    private async Task<CommandResponseDto<ShortGuid>> Send()
    {
        var validation = new BeginLoginWithMagicLink.Validator(_db.Object).Validate(Command());
        validation.Errors.Should().BeEmpty();
        return await new BeginLoginWithMagicLink.Handler(_db.Object).Handle(Command(), CancellationToken.None);
    }

    private static BeginLoginWithMagicLink.Command Command() => new() { EmailAddress = Email };
}

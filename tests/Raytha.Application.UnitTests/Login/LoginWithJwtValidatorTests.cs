using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Login.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Login;

[TestFixture]
public class LoginWithJwtValidatorTests
{
    private const string Secret = "a-test-secret-that-is-long-enough-for-hs256";
    private const string Email = "person@example.com";

    private AuthenticationScheme _scheme = null!;
    private List<User> _users = null!;

    [SetUp]
    public void SetUp()
    {
        _scheme = new AuthenticationScheme
        {
            Id = Guid.NewGuid(),
            Label = "Customer SSO",
            DeveloperName = "customer_sso",
            IsEnabledForUsers = true,
            IsEnabledForAdmins = false,
            JwtSecretKey = Secret,
        };
        _users = [];
    }

    [Test]
    public void Admin_with_unlinked_sub_cannot_sign_in_through_a_users_only_scheme()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsAdmin = true, IsActive = true });

        var result = Validate(Token(sub: "new-idp-subject"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Authentication scheme disabled for administrators.");
    }

    [Test]
    public void Deactivated_user_with_unlinked_sub_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = false });

        var result = Validate(Token(sub: "new-idp-subject"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "User has been deactivated.");
    }

    [Test]
    public void Active_user_with_unlinked_sub_is_accepted()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        Validate(Token(sub: "new-idp-subject")).IsValid.Should().BeTrue();
    }

    [Test]
    public void Linked_sub_is_checked_against_the_linked_account()
    {
        _users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                EmailAddress = "someone-else@example.com",
                IsActive = false,
                SsoId = "linked-subject",
                AuthenticationSchemeId = _scheme.Id,
            }
        );

        var result = Validate(Token(sub: "linked-subject"));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "User has been deactivated.");
    }

    [Test]
    public async Task Linked_account_keeps_its_email_when_the_token_carries_another_accounts_address_in_other_case()
    {
        var linked = new User
        {
            Id = Guid.NewGuid(),
            EmailAddress = "old@example.com",
            IsActive = true,
            SsoId = "linked-subject",
            AuthenticationSchemeId = _scheme.Id,
        };
        _users.Add(linked);
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email.ToUpper(), IsActive = true });

        await Handle(Token(sub: "linked-subject"));

        linked.EmailAddress.Should().Be("old@example.com");
    }

    [Test]
    public async Task Linked_account_takes_the_tokens_email_when_no_other_account_has_it()
    {
        var linked = new User
        {
            Id = Guid.NewGuid(),
            EmailAddress = "old@example.com",
            IsActive = true,
            SsoId = "linked-subject",
            AuthenticationSchemeId = _scheme.Id,
        };
        _users.Add(linked);

        await Handle(Token(sub: "linked-subject"));

        linked.EmailAddress.Should().Be(Email);
    }

    private async Task Handle(string token)
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.AuthenticationSchemes)
            .Returns(new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(_users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.JwtLogins).Returns(new List<JwtLogin>().AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await new LoginWithJwt.Handler(db.Object).Handle(
            new LoginWithJwt.Command { Token = token, DeveloperName = _scheme.DeveloperName! },
            CancellationToken.None
        );
    }

    private FluentValidation.Results.ValidationResult Validate(string token)
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.AuthenticationSchemes)
            .Returns(new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(_users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.JwtLogins).Returns(new List<JwtLogin>().AsQueryable().BuildMockDbSet().Object);

        return new LoginWithJwt.Validator(db.Object).Validate(
            new LoginWithJwt.Command { Token = token, DeveloperName = _scheme.DeveloperName! }
        );
    }

    private static string Token(string sub)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Secret));
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, sub), new Claim(JwtRegisteredClaimNames.Email, Email)]
            ),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }
}

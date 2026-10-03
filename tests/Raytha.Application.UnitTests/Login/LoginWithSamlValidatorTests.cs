using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NUnit.Framework;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Login.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Login;

[TestFixture]
public class LoginWithSamlValidatorTests
{
    private const string Email = "person@example.com";
    private const string SpEntityId = "https://raytha.test/sp";

    private RSA _rsa = null!;
    private X509Certificate2 _certificate = null!;
    private AuthenticationScheme _scheme = null!;
    private List<User> _users = null!;

    [OneTimeSetUp]
    public void CreateSigningCertificate()
    {
        _rsa = RSA.Create(2048);
        _certificate = new CertificateRequest("CN=idp.test", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    [OneTimeTearDown]
    public void DisposeSigningCertificate()
    {
        _certificate.Dispose();
        _rsa.Dispose();
    }

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
            SamlCertificate = _certificate.ExportCertificatePem(),
            SamlIdpEntityId = SpEntityId,
        };
        _users = [];
    }

    [Test]
    public void Admin_with_unlinked_name_id_cannot_sign_in_through_a_users_only_scheme()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsAdmin = true, IsActive = true });

        var result = Validate(SignedResponse(nameId: "new-idp-subject"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Authentication scheme disabled for administrators.");
    }

    [Test]
    public void Deactivated_user_with_unlinked_name_id_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = false });

        var result = Validate(SignedResponse(nameId: "new-idp-subject"));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "User has been deactivated.");
    }

    [Test]
    public void Unknown_email_is_refused_when_the_scheme_is_admin_only()
    {
        _scheme.IsEnabledForUsers = false;
        _scheme.IsEnabledForAdmins = true;

        var result = Validate(SignedResponse(nameId: "new-idp-subject"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Authentication scheme disabled for public users.");
    }

    [Test]
    public async Task Unknown_email_on_an_admin_only_scheme_creates_no_account()
    {
        _scheme.IsEnabledForUsers = false;
        _scheme.IsEnabledForAdmins = true;
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.AuthenticationSchemes)
            .Returns(new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(_users.AsQueryable().BuildMockDbSet().Object);

        var response = await new LoginWithSaml.Handler(db.Object).Handle(
            new LoginWithSaml.Command
            {
                SAMLResponse = SignedResponse(nameId: "new-idp-subject"),
                DeveloperName = _scheme.DeveloperName!,
            },
            CancellationToken.None
        );

        response.Success.Should().BeFalse();
        response.GetErrors().Should().ContainSingle(e => e.ErrorMessage == "Authentication scheme disabled for public users.");
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Active_user_with_unlinked_name_id_is_accepted()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        Validate(SignedResponse(nameId: "new-idp-subject")).IsValid.Should().BeTrue();
    }

    [Test]
    public void Assertion_issued_for_another_service_provider_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        var result = Validate(SignedResponse(nameId: "subject", audience: "https://other-app.test/sp"));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Failed authentication.");
    }

    [Test]
    public void Assertion_without_an_audience_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        var result = Validate(SignedResponse(nameId: "subject", audience: null));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Failed authentication.");
    }

    [Test]
    public void Assertion_without_an_expiry_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        var result = Validate(SignedResponse(nameId: "subject", withExpiry: false));

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Failed authentication.");
    }

    [Test]
    public void Unsigned_assertion_next_to_a_different_signed_element_is_refused()
    {
        _users.Add(new User { Id = Guid.NewGuid(), EmailAddress = Email, IsActive = true });

        var result = Validate(WrappedResponse());

        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Failed authentication.");
    }

    [Test]
    public async Task Linked_account_keeps_its_email_when_the_assertion_carries_another_accounts_address_in_other_case()
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

        await Handle(SignedResponse(nameId: "linked-subject"));

        linked.EmailAddress.Should().Be("old@example.com");
    }

    [Test]
    public async Task Linked_account_takes_the_assertions_email_when_no_other_account_has_it()
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

        await Handle(SignedResponse(nameId: "linked-subject"));

        linked.EmailAddress.Should().Be(Email);
    }

    private async Task Handle(string samlResponse)
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.AuthenticationSchemes)
            .Returns(new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(_users.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await new LoginWithSaml.Handler(db.Object).Handle(
            new LoginWithSaml.Command { SAMLResponse = samlResponse, DeveloperName = _scheme.DeveloperName! },
            CancellationToken.None
        );
    }

    private FluentValidation.Results.ValidationResult Validate(string samlResponse)
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.AuthenticationSchemes)
            .Returns(new List<AuthenticationScheme> { _scheme }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Users).Returns(_users.AsQueryable().BuildMockDbSet().Object);

        return new LoginWithSaml.Validator(db.Object).Validate(
            new LoginWithSaml.Command { SAMLResponse = samlResponse, DeveloperName = _scheme.DeveloperName! }
        );
    }

    private string SignedResponse(string nameId, string? audience = SpEntityId, bool withExpiry = true)
    {
        var now = DateTime.UtcNow;
        var expiry = withExpiry ? $" NotOnOrAfter=\"{now.AddMinutes(5):yyyy-MM-ddTHH:mm:ssZ}\"" : string.Empty;
        var audienceRestriction = audience is null
            ? string.Empty
            : $"<saml:AudienceRestriction><saml:Audience>{audience}</saml:Audience></saml:AudienceRestriction>";
        var xml =
            "<samlp:Response xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\" xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\" "
            + $"ID=\"_r1\" Version=\"2.0\" IssueInstant=\"{now:yyyy-MM-ddTHH:mm:ssZ}\">"
            + "<saml:Issuer>https://idp.test</saml:Issuer>"
            + "<samlp:Status><samlp:StatusCode Value=\"urn:oasis:names:tc:SAML:2.0:status:Success\"/></samlp:Status>"
            + $"<saml:Assertion ID=\"_a1\" Version=\"2.0\" IssueInstant=\"{now:yyyy-MM-ddTHH:mm:ssZ}\">"
            + "<saml:Issuer>https://idp.test</saml:Issuer>"
            + $"<saml:Subject><saml:NameID>{nameId}</saml:NameID></saml:Subject>"
            + $"<saml:Conditions NotBefore=\"{now.AddMinutes(-1):yyyy-MM-ddTHH:mm:ssZ}\"{expiry}>{audienceRestriction}</saml:Conditions>"
            + $"<saml:AttributeStatement><saml:Attribute Name=\"email\"><saml:AttributeValue>{Email}</saml:AttributeValue></saml:Attribute></saml:AttributeStatement>"
            + "</saml:Assertion></samlp:Response>";

        var doc = Load(xml);
        var assertion = (XmlElement)doc.SelectSingleNode("//saml:Assertion", Namespaces(doc))!;
        Sign(doc, assertion, "_a1", insertAfter: assertion.SelectSingleNode("saml:Issuer", Namespaces(doc))!);
        return Encode(doc);
    }

    private string WrappedResponse()
    {
        var now = DateTime.UtcNow;
        var xml =
            "<samlp:Response xmlns:samlp=\"urn:oasis:names:tc:SAML:2.0:protocol\" xmlns:saml=\"urn:oasis:names:tc:SAML:2.0:assertion\" "
            + $"ID=\"_r1\" Version=\"2.0\" IssueInstant=\"{now:yyyy-MM-ddTHH:mm:ssZ}\">"
            + "<saml:Issuer>https://idp.test</saml:Issuer>"
            + "<samlp:Extensions ID=\"_e1\"><saml:Issuer>https://idp.test</saml:Issuer></samlp:Extensions>"
            + "<samlp:Status><samlp:StatusCode Value=\"urn:oasis:names:tc:SAML:2.0:status:Success\"/></samlp:Status>"
            + $"<saml:Assertion ID=\"_a1\" Version=\"2.0\" IssueInstant=\"{now:yyyy-MM-ddTHH:mm:ssZ}\">"
            + "<saml:Issuer>https://idp.test</saml:Issuer>"
            + "<saml:Subject><saml:NameID>forged</saml:NameID></saml:Subject>"
            + $"<saml:Conditions NotBefore=\"{now.AddMinutes(-1):yyyy-MM-ddTHH:mm:ssZ}\" NotOnOrAfter=\"{now.AddMinutes(5):yyyy-MM-ddTHH:mm:ssZ}\">"
            + $"<saml:AudienceRestriction><saml:Audience>{SpEntityId}</saml:Audience></saml:AudienceRestriction></saml:Conditions>"
            + $"<saml:AttributeStatement><saml:Attribute Name=\"email\"><saml:AttributeValue>{Email}</saml:AttributeValue></saml:Attribute></saml:AttributeStatement>"
            + "</saml:Assertion></samlp:Response>";

        var doc = Load(xml);
        var extensions = (XmlElement)doc.SelectSingleNode("//samlp:Extensions", Namespaces(doc))!;
        Sign(doc, extensions, "_e1", insertAfter: extensions.FirstChild!);
        return Encode(doc);
    }

    private void Sign(XmlDocument doc, XmlElement element, string id, XmlNode insertAfter)
    {
        var signed = new SignedXml(doc) { SigningKey = _rsa };
        var reference = new Reference($"#{id}");
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signed.AddReference(reference);
        signed.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
        signed.KeyInfo = new KeyInfo();
        signed.KeyInfo.AddClause(new KeyInfoX509Data(_certificate));
        signed.ComputeSignature();
        element.InsertAfter(doc.ImportNode(signed.GetXml(), true), insertAfter);
    }

    private static XmlDocument Load(string xml)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        return doc;
    }

    private static XmlNamespaceManager Namespaces(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
        ns.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
        return ns;
    }

    private static string Encode(XmlDocument doc) => Convert.ToBase64String(Encoding.ASCII.GetBytes(doc.OuterXml));
}
